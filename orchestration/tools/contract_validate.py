#!/usr/bin/env python3
"""
SimpX 契约校验：检查任务契约和其他契约的字段、状态和引用。

用法：
  python orchestration/tools/contract_validate.py <文件或目录> [<文件或目录> ...]

目录会递归找 .yaml / .yml / .json。每个文件打印“通过”或“失败”及原因，有失败时退出码是 1。
字段规则读 contracts/schemas/，状态规则读 orchestration/policies/task-states.yaml 和 parallelism.yaml。
"""

from __future__ import annotations

import json
import sys
from dataclasses import dataclass
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

import schema_check  # noqa: E402
import simple_yaml  # noqa: E402

SIMPX_ROOT = TOOLS_DIR.parents[1]
TASK_SCHEMA_PATH = SIMPX_ROOT / "contracts" / "schemas" / "task-contract.schema.json"
HEADER_SCHEMA_PATH = SIMPX_ROOT / "contracts" / "schemas" / "contract-header.schema.json"
STATES_POLICY_PATH = SIMPX_ROOT / "orchestration" / "policies" / "task-states.yaml"
PARALLEL_POLICY_PATH = SIMPX_ROOT / "orchestration" / "policies" / "parallelism.yaml"
CONTRACT_SUFFIXES = {".yaml", ".yml", ".json"}


class RulesError(ValueError):
    """Schema 或策略文件自己有问题，校验没法开始。"""


@dataclass
class Rules:
    """一次校验要用到的 Schema 和策略，启动时读一次，后面所有文件共用。"""

    task_schema: dict
    header_schema: dict
    states: list[str]
    ready_or_later: set[str]
    dependency_satisfied: set[str]
    requires_verification_record: set[str]
    evidence_required_on_enter: set[str]
    finished: set[str]
    transitions: dict[str, set[str]]
    classes: dict[str, dict]
    require_allowed_paths: bool


def load_rules(
    states_path: Path = STATES_POLICY_PATH,
    parallel_path: Path = PARALLEL_POLICY_PATH,
    task_schema_path: Path = TASK_SCHEMA_PATH,
    header_schema_path: Path = HEADER_SCHEMA_PATH,
) -> Rules:
    """读 Schema 和策略文件，并核对状态名和 Schema 里的枚举一致。

    Args:
        states_path: 任务状态策略文件。
        parallel_path: 并行策略文件。
        task_schema_path: 任务契约 Schema。
        header_schema_path: 其他契约的通用头 Schema。

    Returns:
        校验规则。

    Raises:
        RulesError: 策略文件缺字段、状态名对不上 Schema、跳转里出现没登记的状态。
    """
    try:
        task_schema = schema_check.load_schema(task_schema_path)
        header_schema = schema_check.load_schema(header_schema_path)
        states_policy = simple_yaml.load_file(states_path)
        parallel_policy = simple_yaml.load_file(parallel_path)
    except (schema_check.SchemaError, simple_yaml.YamlSubsetError, OSError) as error:
        raise RulesError(str(error)) from None
    if not isinstance(states_policy, dict) or not isinstance(parallel_policy, dict):
        raise RulesError("策略文件顶层要是映射")

    def state_list(key: str) -> list[str]:
        """从状态策略里取一个状态列表，缺了就报错。"""
        value = states_policy.get(key)
        if not isinstance(value, list):
            raise RulesError(f"{states_path}：缺少列表 {key}")
        return value

    states = state_list("states")
    schema_states = task_schema["properties"]["state"]["enum"]
    if sorted(states) != sorted(schema_states):
        raise RulesError(f"{states_path} 的 states 和 {task_schema_path} 的 state 枚举不一致")
    transitions_raw = states_policy.get("transitions")
    if not isinstance(transitions_raw, dict) or set(transitions_raw) != set(states):
        raise RulesError(f"{states_path}：transitions 要给每个状态都写一行")
    transitions = {}
    for source, targets in transitions_raw.items():
        if not isinstance(targets, list) or not set(targets) <= set(states):
            raise RulesError(f"{states_path}：transitions.{source} 里有没登记的状态")
        transitions[source] = set(targets)
    groups = {}
    for key in ("ready_or_later", "dependency_satisfied", "requires_verification_record", "evidence_required_on_enter", "finished"):
        values = state_list(key)
        if not set(values) <= set(states):
            raise RulesError(f"{states_path}：{key} 里有没登记的状态")
        groups[key] = set(values)

    classes = parallel_policy.get("classes")
    if not isinstance(classes, dict) or set(classes) != {"P0", "P1", "P2", "P3"}:
        raise RulesError(f"{parallel_path}：classes 要正好是 P0/P1/P2/P3")
    safety = parallel_policy.get("safety") or {}
    return Rules(
        task_schema=task_schema,
        header_schema=header_schema,
        states=states,
        transitions=transitions,
        classes=classes,
        require_allowed_paths=bool(safety.get("require_allowed_paths")),
        **groups,
    )


def load_contract(path: Path) -> object:
    """按扩展名读一个契约文件：.json 用 json，其余用 YAML 子集解析器。

    Args:
        path: 契约文件路径。

    Returns:
        解析出的内容。

    Raises:
        simple_yaml.YamlSubsetError: YAML 写法不支持或格式不对。
        ValueError: JSON 格式不对。
    """
    if path.suffix.lower() == ".json":
        try:
            return json.loads(path.read_text(encoding="utf-8-sig"))
        except json.JSONDecodeError as error:
            raise ValueError(f"{path}：不是合法 JSON（第 {error.lineno} 行：{error.msg}）") from None
    return simple_yaml.load_file(path)


def check_file(path: Path, rules: Rules) -> tuple[object, list[str]]:
    """读一个契约文件并做全部单文件检查。

    Args:
        path: 契约文件路径。
        rules: 校验规则。

    Returns:
        (解析出的内容，读不出来时是 None；错误列表)。
    """
    try:
        data = load_contract(path)
    except (simple_yaml.YamlSubsetError, ValueError, OSError) as error:
        return None, [f"读不出来：{error}"]
    return data, check_contract(path, data, rules)


def check_contract(path: Path, data: object, rules: Rules) -> list[str]:
    """按 contract_type 选 Schema 校验字段，再做状态和引用检查。

    Args:
        path: 契约文件路径，解析 contract_refs 的相对路径要用。
        data: 解析出的内容。
        rules: 校验规则。

    Returns:
        错误列表；空列表表示通过。
    """
    if not isinstance(data, dict):
        return ["顶层要是映射"]
    contract_type = data.get("contract_type")
    if contract_type is None:
        return ["缺少 contract_type，判断不了是哪种契约"]
    if contract_type == "task":
        errors = schema_check.validate(data, rules.task_schema)
        return errors or check_task(path, data, rules)
    errors = schema_check.validate(data, rules.header_schema)
    return errors or check_header(data)


def check_header(data: dict) -> list[str]:
    """检查接口、数据、执行、验收契约的状态字段前后一致。

    Args:
        data: 已通过通用头 Schema 的契约内容。

    Returns:
        错误列表。
    """
    errors = []
    if data["status"] == "approved" and not data.get("approved_by"):
        errors.append("status 是 approved，但没写 approved_by")
    if data["status"] == "superseded" and not data.get("superseded_by"):
        errors.append("status 是 superseded，但没写 superseded_by，找不到新版本")
    return errors


def check_task(path: Path, data: dict, rules: Rules) -> list[str]:
    """检查任务契约的审批、引用契约、允许修改路径、验证记录和状态历史。

    这些检查只看这一个文件；依赖的任务是否存在、是否满足由 task_graph_validate.py 查。

    Args:
        path: 任务文件路径。
        data: 已通过任务 Schema 的内容。
        rules: 校验规则。

    Returns:
        错误列表。
    """
    errors: list[str] = []
    state = data["state"]
    status = data["status"]
    parallel_class = data["parallel"]["class"]

    if status == "approved" and not data.get("approved_by"):
        errors.append("status 是 approved，但没写 approved_by")

    if rules.classes[parallel_class].get("require_contract_refs") and not data["contract_refs"]:
        errors.append(f"并行类别 {parallel_class} 要靠已批准的契约解耦，但 contract_refs 是空的")

    if state in rules.ready_or_later:
        if status != "approved":
            errors.append(f"任务契约状态是 {status}，没批准不能进 {state}")
        errors.extend(_check_contract_refs(path, data["contract_refs"], state))
        if data["task_type"] == "implementation" and rules.require_allowed_paths and not data["workspace"]["allowed_paths"]:
            errors.append(f"实现类任务进 {state} 前要写 workspace.allowed_paths")

    if state in rules.requires_verification_record:
        verification = data["verification"]
        if not verification["commands"]:
            errors.append(f"状态是 {state}，但 verification.commands 是空的；手工验证写成“手工：……”")
        if verification["not_run_reason"]:
            errors.append(f"状态是 {state}，但 not_run_reason 还写着没跑的原因")

    errors.extend(_check_history(data, rules))
    return errors


def _check_contract_refs(path: Path, refs: list[dict], state: str) -> list[str]:
    """逐个打开任务引用的契约文件，核对 ID、版本，并要求状态是 approved。

    Args:
        path: 任务文件路径；引用路径相对它所在的目录。
        refs: contract_refs 列表。
        state: 任务当前状态，错误消息用。

    Returns:
        错误列表。
    """
    errors = []
    for ref in refs:
        target = (path.parent / ref["path"]).resolve()
        label = f"引用的契约 {ref['id']}@{ref['version']}"
        if not target.is_file():
            errors.append(f"{label}：文件 {ref['path']} 不存在")
            continue
        try:
            contract = load_contract(target)
        except (simple_yaml.YamlSubsetError, ValueError, OSError) as error:
            errors.append(f"{label}：读不出来（{error}）")
            continue
        if not isinstance(contract, dict):
            errors.append(f"{label}：文件顶层不是映射")
            continue
        if contract.get("contract_id") != ref["id"]:
            errors.append(f"{label}：文件里的 contract_id 是 {contract.get('contract_id')!r}，对不上")
        if contract.get("version") != ref["version"]:
            errors.append(f"{label}：文件里的版本是 {contract.get('version')!r}，对不上")
        if contract.get("status") != "approved":
            errors.append(f"{label}：状态是 {contract.get('status')!r}，没批准，任务不能进 {state}")
    return errors


def _check_history(data: dict, rules: Rules) -> list[str]:
    """检查状态历史：每一步跳转合法、前后衔接、最后一条等于当前状态、关键状态带证据。

    Args:
        data: 任务契约内容。
        rules: 校验规则。

    Returns:
        错误列表。
    """
    errors = []
    history = data.get("state_history") or []
    state = data["state"]
    if not history:
        if state in rules.evidence_required_on_enter:
            errors.append(f"状态是 {state}，但没有 state_history，看不出是谁、凭什么改到这个状态")
        return errors
    previous_to = None
    for position, entry in enumerate(history):
        source, target = entry["from"], entry["to"]
        label = f"state_history[{position}]"
        if source not in rules.transitions or target not in rules.transitions:
            errors.append(f"{label}：{source} → {target} 里有没登记的状态")
            continue
        if previous_to is not None and source != previous_to:
            errors.append(f"{label}：from 是 {source}，但上一条的 to 是 {previous_to}，接不上")
        if target not in rules.transitions[source]:
            allowed = "、".join(sorted(rules.transitions[source])) or "无"
            errors.append(f"{label}：不能从 {source} 直接跳到 {target}，{source} 只能到 {allowed}")
        if target in rules.evidence_required_on_enter and not entry.get("evidence_refs"):
            errors.append(f"{label}：进入 {target} 要带 evidence_refs，不能只凭一句已完成")
        previous_to = target
    if previous_to != state:
        errors.append(f"state 是 {state}，但 state_history 最后一条的 to 是 {previous_to}")
    return errors


def collect_files(targets: list[str]) -> list[Path]:
    """把命令行给的文件和目录展开成契约文件列表，目录递归查找。

    Args:
        targets: 文件或目录路径。

    Returns:
        去重排序后的文件列表。

    Raises:
        FileNotFoundError: 给的路径不存在。
    """
    files: set[Path] = set()
    for target in targets:
        path = Path(target)
        if path.is_dir():
            files.update(item for item in path.rglob("*") if item.is_file() and item.suffix.lower() in CONTRACT_SUFFIXES)
        elif path.is_file():
            files.add(path)
        else:
            raise FileNotFoundError(f"路径不存在：{target}")
    return sorted(files)


def display(path: Path) -> str:
    """把路径显示成相对当前目录的正斜杠形式，报告里不出现盘符；不在当前目录下时显示原样。

    Args:
        path: 文件路径。

    Returns:
        显示用的路径。
    """
    try:
        return path.resolve().relative_to(Path.cwd().resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def main(argv: list[str]) -> int:
    """命令行入口：校验给定的契约文件或目录，打印每个文件的结果。

    Args:
        argv: 命令行参数（不含脚本名）。

    Returns:
        退出码：全部通过是 0，有失败或用法不对是 1。
    """
    if not argv:
        print(__doc__.strip())
        return 1
    try:
        rules = load_rules()
        files = collect_files(argv)
    except (RulesError, FileNotFoundError) as error:
        print(f"无法开始校验：{error}")
        return 1
    if not files:
        print("没有找到 .yaml / .yml / .json 契约文件")
        return 1
    failed = 0
    for path in files:
        _, errors = check_file(path, rules)
        if errors:
            failed += 1
            print(f"失败  {display(path)}")
            for error in errors:
                print(f"  - {error}")
        else:
            print(f"通过  {display(path)}")
    print(f"共 {len(files)} 个文件，通过 {len(files) - failed}，失败 {failed}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

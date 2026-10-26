#!/usr/bin/env python3
"""
SimpX 任务图校验：检查一组任务契约的依赖、循环、并行类别和可能同时推进的冲突。

用法：
  python orchestration/tools/task_graph_validate.py <任务目录>

目录下递归找契约文件；contract_type 是 task 的当作任务，其余契约只用来解析 contract_refs。
先对每个文件跑 contract_validate.py 的单文件检查，再做整图检查。
全部通过时打印按依赖排好的推进顺序；有错误时退出码是 1。
"""

from __future__ import annotations

import sys
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

import contract_validate  # noqa: E402

WILDCARDS = "*?["


def load_tasks(directory: Path, rules: contract_validate.Rules) -> tuple[dict[str, dict], list[str], int]:
    """读目录下所有契约文件，挑出任务契约，同时收集单文件检查的错误。

    Args:
        directory: 任务目录。
        rules: 校验规则。

    Returns:
        (task_id 到 {"path", "data"} 的字典, 错误列表, 检查过的文件数)。
    """
    tasks: dict[str, dict] = {}
    errors: list[str] = []
    files = contract_validate.collect_files([str(directory)])
    for path in files:
        data, file_errors = contract_validate.check_file(path, rules)
        shown = contract_validate.display(path)
        errors.extend(f"{shown}：{error}" for error in file_errors)
        if not isinstance(data, dict) or data.get("contract_type") != "task":
            continue
        task_id = data.get("task_id")
        if not isinstance(task_id, str):
            continue
        if task_id in tasks:
            errors.append(f"{shown}：task_id {task_id} 和 {contract_validate.display(tasks[task_id]['path'])} 重复")
            continue
        tasks[task_id] = {"path": path, "data": data}
    return tasks, errors, len(files)


def find_cycles(graph: dict[str, list[str]]) -> list[list[str]]:
    """找出依赖图里的循环，每个循环只报一次。

    Args:
        graph: 任务到它依赖的任务列表（只含存在的任务）。

    Returns:
        循环列表，每个循环是首尾相同的任务序列，如 [A, B, A]。
    """
    cycles: list[list[str]] = []
    seen_sets: set[frozenset] = set()
    color = {node: 0 for node in graph}
    stack: list[str] = []

    def visit(node: str) -> None:
        """深度优先走一遍，遇到正在走的节点就是循环。"""
        color[node] = 1
        stack.append(node)
        for dep in graph[node]:
            if color[dep] == 0:
                visit(dep)
            elif color[dep] == 1:
                cycle = stack[stack.index(dep):] + [dep]
                key = frozenset(cycle)
                if key not in seen_sets:
                    seen_sets.add(key)
                    cycles.append(cycle)
        stack.pop()
        color[node] = 2

    for node in sorted(graph):
        if color[node] == 0:
            visit(node)
    return cycles


def reachable(graph: dict[str, list[str]]) -> dict[str, set[str]]:
    """算出每个任务直接或间接依赖的全部任务；用来判断两个任务有没有先后关系。

    Args:
        graph: 无循环的依赖图。

    Returns:
        任务到它所有上游任务的集合。
    """
    result: dict[str, set[str]] = {}

    def upstream(node: str) -> set[str]:
        """递归收集上游，算过的直接复用。"""
        if node not in result:
            found: set[str] = set()
            for dep in graph[node]:
                found.add(dep)
                found |= upstream(dep)
            result[node] = found
        return result[node]

    for node in graph:
        upstream(node)
    return result


def layers(graph: dict[str, list[str]]) -> list[list[str]]:
    """按依赖分层：第一层没有依赖，后面每层只依赖前面的层。

    Args:
        graph: 无循环的依赖图。

    Returns:
        每层的任务列表，层内按 task_id 排序。
    """
    remaining = {node: set(deps) for node, deps in graph.items()}
    result: list[list[str]] = []
    done: set[str] = set()
    while remaining:
        layer = sorted(node for node, deps in remaining.items() if deps <= done)
        result.append(layer)
        done.update(layer)
        for node in layer:
            del remaining[node]
    return result


def path_prefix(pattern: str) -> str:
    """取一个路径写法里通配符前面的完整目录部分，用来粗判两个路径会不会重叠。

    Args:
        pattern: 允许修改路径，可以带 * ? [ 通配符。

    Returns:
        去掉通配部分后的路径前缀，没有斜杠结尾；整个仓库都可能改时返回空串。
    """
    normalized = pattern.replace("\\", "/").strip()
    while normalized.startswith("./"):
        normalized = normalized[2:]
    cut = min([normalized.find(char) for char in WILDCARDS if char in normalized] or [len(normalized)])
    prefix = normalized[:cut]
    if cut < len(normalized) and not prefix.endswith("/"):
        prefix = prefix[: prefix.rfind("/") + 1] if "/" in prefix else ""
    return prefix.rstrip("/")


def paths_overlap(first: str, second: str) -> bool:
    """判断两个允许修改路径可能不可能改到同一个文件；拿不准时按会重叠算。

    Args:
        first: 第一个路径写法。
        second: 第二个路径写法。

    Returns:
        可能重叠时返回 True。
    """
    a, b = path_prefix(first), path_prefix(second)
    if not a or not b:
        return True
    return a == b or a.startswith(b + "/") or b.startswith(a + "/")


def check_graph(tasks: dict[str, dict], rules: contract_validate.Rules) -> tuple[list[str], dict[str, list[str]] | None]:
    """做整图检查：依赖存在、无自依赖、无循环、依赖满足、并行类别和同时推进的冲突。

    Args:
        tasks: load_tasks 读出的任务。
        rules: 校验规则。

    Returns:
        (错误列表, 依赖图；有循环时依赖图是 None，没法排顺序)。
    """
    errors: list[str] = []
    graph: dict[str, list[str]] = {}
    for task_id, task in sorted(tasks.items()):
        deps = task["data"].get("depends_on") or []
        for dep in deps:
            if dep == task_id:
                errors.append(f"{task_id}：依赖了自己")
            elif dep not in tasks:
                errors.append(f"{task_id}：依赖的 {dep} 不存在（目录里没有这个任务）")
        graph[task_id] = [dep for dep in deps if dep in tasks and dep != task_id]

    cycles = find_cycles(graph)
    for cycle in cycles:
        errors.append("有循环依赖：" + " → ".join(cycle))
    if cycles:
        return errors, None

    for task_id, task in sorted(tasks.items()):
        data = task["data"]
        state = data.get("state")
        if state in rules.finished:
            continue
        parallel_class = (data.get("parallel") or {}).get("class")
        unsatisfied = [dep for dep in graph[task_id] if tasks[dep]["data"].get("state") not in rules.dependency_satisfied]
        cancelled = [dep for dep in graph[task_id] if tasks[dep]["data"].get("state") == "Cancelled"]
        for dep in cancelled:
            errors.append(f"{task_id}：依赖的 {dep} 已取消，要么去掉这条依赖，要么一起取消")
        if state in rules.ready_or_later and unsatisfied:
            listed = "、".join(f"{dep}({tasks[dep]['data'].get('state')})" for dep in unsatisfied)
            errors.append(f"{task_id}：状态是 {state}，但依赖还没满足：{listed}")
        policy = rules.classes.get(parallel_class) or {}
        if unsatisfied and policy.get("allow_unsatisfied_dependencies") is False:
            errors.append(f"{task_id}：判成 {parallel_class}，但还有没满足的硬依赖 {'、'.join(unsatisfied)}；要么改成 P2/P3，要么先用契约解耦")

    errors.extend(_check_concurrent(tasks, graph, rules))
    return errors, graph


def _check_concurrent(tasks: dict[str, dict], graph: dict[str, list[str]], rules: contract_validate.Rules) -> list[str]:
    """找出可能同时推进的任务对，检查允许修改路径和共享资源有没有冲突。

    彼此没有先后依赖、都没结束、并行类别允许并行的两个任务，算作可能同时推进。

    Args:
        tasks: 全部任务。
        graph: 无循环的依赖图。
        rules: 校验规则。

    Returns:
        错误列表。
    """
    errors: list[str] = []
    upstream = reachable(graph)
    candidates = sorted(
        task_id for task_id, task in tasks.items()
        if task["data"].get("state") not in rules.finished
        and (rules.classes.get((task["data"].get("parallel") or {}).get("class")) or {}).get("allow_parallel")
    )
    for index, first in enumerate(candidates):
        for second in candidates[index + 1:]:
            if first in upstream[second] or second in upstream[first]:
                continue
            first_ws = tasks[first]["data"].get("workspace") or {}
            second_ws = tasks[second]["data"].get("workspace") or {}
            overlaps = [
                (a, b)
                for a in first_ws.get("allowed_paths") or []
                for b in second_ws.get("allowed_paths") or []
                if paths_overlap(a, b)
            ]
            if overlaps:
                shown = "；".join(f"{a} 和 {b}" for a, b in overlaps[:3])
                errors.append(
                    f"{first} 和 {second} 彼此没有依赖、可能同时推进，但允许修改路径可能重叠：{shown}；"
                    "要么指定一个任务负责这些文件，要么加依赖改成先后"
                )
            shared = sorted(set(first_ws.get("shared_resources") or []) & set(second_ws.get("shared_resources") or []))
            if shared:
                errors.append(f"{first} 和 {second} 可能同时推进，但共用资源 {'、'.join(shared)}，要改成先后")
    return errors


def main(argv: list[str]) -> int:
    """命令行入口：校验一个任务目录，打印错误或推进顺序。

    Args:
        argv: 命令行参数（不含脚本名）。

    Returns:
        退出码：通过是 0，有错误或用法不对是 1。
    """
    if len(argv) != 1:
        print(__doc__.strip())
        return 1
    directory = Path(argv[0])
    if not directory.is_dir():
        print(f"无法开始校验：{argv[0]} 不是目录")
        return 1
    try:
        rules = contract_validate.load_rules()
    except contract_validate.RulesError as error:
        print(f"无法开始校验：{error}")
        return 1
    tasks, errors, file_count = load_tasks(directory, rules)
    if not tasks and not errors:
        print("目录里没有任务契约（contract_type: task）")
        return 1
    graph_errors, graph = check_graph(tasks, rules)
    errors.extend(graph_errors)

    print(f"检查了 {file_count} 个契约文件，其中任务 {len(tasks)} 个")
    if errors:
        print(f"失败，共 {len(errors)} 个问题：")
        for error in errors:
            print(f"  - {error}")
        return 1
    print("通过。推进顺序（同一层的任务彼此没有依赖）：")
    for number, layer in enumerate(layers(graph), 1):
        described = "、".join(
            f"{task_id}（{tasks[task_id]['data']['parallel']['class']}，{tasks[task_id]['data']['owner_role']}，{tasks[task_id]['data']['state']}）"
            for task_id in layer
        )
        print(f"  第 {number} 层：{described}")
    print("注意：现在没有真实执行器和隔离工作区，同一层的任务也只能在会话里按顺序推进，不能说已并行。")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

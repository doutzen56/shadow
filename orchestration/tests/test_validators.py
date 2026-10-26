"""
契约校验和任务图校验的行为测试：在临时目录写真实契约文件，跑命令行入口，看退出码和输出。

运行：python -m unittest discover -s orchestration/tests -v
临时目录由 tempfile 创建，每个用例结束自动删除，不在 SimpX 里留文件。
"""

from __future__ import annotations

import contextlib
import copy
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parents[1] / "tools"
sys.path.insert(0, str(TOOLS_DIR))

import contract_validate  # noqa: E402
import schema_check  # noqa: E402
import simple_yaml  # noqa: E402
import task_graph_validate  # noqa: E402

SIMPX_ROOT = Path(__file__).resolve().parents[2]

BASE_TASK = {
    "schema_version": "v1",
    "contract_type": "task",
    "task_id": "TASK-A",
    "title": "示例任务",
    "status": "draft",
    "state": "Draft",
    "task_type": "implementation",
    "owner_role": "@@BE",
    "project_id": None,
    "goal": "完成一个可验证的行为",
    "depends_on": [],
    "contract_refs": [],
    "parallel": {"class": "P3", "rationale": "尚未完成依赖分析", "group_id": None},
    "workspace": {"mode": "session", "allowed_paths": [], "forbidden_paths": [], "shared_resources": []},
    "acceptance": [{"id": "AC-001", "description": "可观察的结果", "verification_method": "单元测试", "evidence_required": True}],
    "verification": {"required_checks": [], "commands": [], "not_run_reason": None},
    "created_by": "@@X",
    "approved_by": None,
}

API_CONTRACT = {
    "schema_version": "v1",
    "contract_type": "api",
    "contract_id": "API-RECHARGE",
    "version": "1.0.0",
    "status": "approved",
    "owner_role": "@@BA",
    "consumers": ["@@FE"],
    "approved_by": "@@TL",
}


def make_task(task_id: str = "TASK-A", **changes: object) -> dict:
    """按基础模板生成一个任务契约，再覆盖指定字段；嵌套字段整块替换。

    Args:
        task_id: 任务编号。
        **changes: 要覆盖的顶层字段。

    Returns:
        任务契约字典。
    """
    task = copy.deepcopy(BASE_TASK)
    task["task_id"] = task_id
    task.update(copy.deepcopy(changes))
    return task


def approved_ready(task_id: str, **changes: object) -> dict:
    """生成一个已批准、状态是 Ready、带允许修改路径和状态历史的实现任务。

    Args:
        task_id: 任务编号。
        **changes: 要覆盖的顶层字段。

    Returns:
        任务契约字典。
    """
    base = {
        "status": "approved",
        "approved_by": "@@TL",
        "state": "Ready",
        "workspace": {"mode": "session", "allowed_paths": [f"src/{task_id.lower()}/**"], "forbidden_paths": [], "shared_resources": []},
        "state_history": [
            {"at": "2026-10-26T00:00:00Z", "actor": "@@X", "from": "Draft", "to": "Planned", "reason": "拆完任务"},
            {"at": "2026-10-26T00:10:00Z", "actor": "@@X", "from": "Planned", "to": "Ready", "reason": "依赖满足"},
        ],
    }
    base.update(changes)
    return make_task(task_id, **base)


def write_json(directory: Path, name: str, data: dict) -> Path:
    """把契约按 UTF-8 写成 JSON 文件。

    Args:
        directory: 目标目录。
        name: 文件名。
        data: 契约内容。

    Returns:
        写出的文件路径。
    """
    path = directory / name
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    return path


def run_tool(module: object, args: list[str]) -> tuple[int, str]:
    """调用工具的命令行入口，收集打印内容。

    Args:
        module: contract_validate 或 task_graph_validate 模块。
        args: 命令行参数。

    Returns:
        (退出码, 打印出的全部文本)。
    """
    buffer = io.StringIO()
    with contextlib.redirect_stdout(buffer):
        code = module.main(args)
    return code, buffer.getvalue()


class ContractValidateTests(unittest.TestCase):
    """单个契约文件的校验行为。"""

    def setUp(self) -> None:
        """每个用例一个干净的临时目录。"""
        self._tmp = tempfile.TemporaryDirectory()
        self.dir = Path(self._tmp.name)

    def tearDown(self) -> None:
        """删掉临时目录和里面的契约文件。"""
        self._tmp.cleanup()

    def test_template_in_repo_passes(self) -> None:
        """仓库里的任务模板本身要能通过校验，否则复制模板的人一开始就是错的。"""
        code, output = run_tool(contract_validate, [str(SIMPX_ROOT / "contracts" / "templates" / "task.yaml")])
        self.assertEqual(code, 0, output)

    def test_missing_acceptance_fails(self) -> None:
        """没写验收条件的任务契约不能通过。"""
        task = make_task()
        del task["acceptance"]
        path = write_json(self.dir, "task.json", task)
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("缺少必填字段 acceptance", output)

    def test_unapproved_task_cannot_be_ready(self) -> None:
        """任务契约还是 draft 时，状态不能是 Ready。"""
        path = write_json(self.dir, "task.json", approved_ready("TASK-A", status="draft", approved_by=None))
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("没批准不能进 Ready", output)

    def test_referenced_contract_must_be_approved(self) -> None:
        """引用的接口契约还在 review 时任务不能 Ready；契约批准后同一个任务就能通过。"""
        contract = dict(API_CONTRACT, status="review", approved_by=None)
        write_json(self.dir, "api.json", contract)
        refs = [{"id": "API-RECHARGE", "version": "1.0.0", "path": "api.json"}]
        task_path = write_json(self.dir, "task.json", approved_ready("TASK-A", contract_refs=refs))

        code, output = run_tool(contract_validate, [str(task_path)])
        self.assertEqual(code, 1)
        self.assertIn("没批准，任务不能进 Ready", output)

        write_json(self.dir, "api.json", API_CONTRACT)
        code, output = run_tool(contract_validate, [str(task_path)])
        self.assertEqual(code, 0, output)

    def test_referenced_contract_version_mismatch_fails(self) -> None:
        """任务引用的版本和契约文件里的版本不一致时要报错，防止按旧版本实现。"""
        write_json(self.dir, "api.json", dict(API_CONTRACT, version="2.0.0"))
        refs = [{"id": "API-RECHARGE", "version": "1.0.0", "path": "api.json"}]
        path = write_json(self.dir, "task.json", approved_ready("TASK-A", contract_refs=refs))
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("版本是 '2.0.0'，对不上", output)

    def test_missing_referenced_file_fails(self) -> None:
        """引用的契约文件不存在时要报错，不能当成已批准。"""
        refs = [{"id": "API-RECHARGE", "version": "1.0.0", "path": "nowhere.json"}]
        path = write_json(self.dir, "task.json", approved_ready("TASK-A", contract_refs=refs))
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("不存在", output)

    def test_p1_without_contract_refs_fails(self) -> None:
        """判成 P1（靠契约解耦）却没有引用任何契约，说明判断依据站不住。"""
        task = make_task(parallel={"class": "P1", "rationale": "按契约并行", "group_id": None})
        path = write_json(self.dir, "task.json", task)
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("contract_refs 是空的", output)

    def test_ready_implementation_without_allowed_paths_fails(self) -> None:
        """实现任务进 Ready 前要写允许修改路径。"""
        workspace = {"mode": "session", "allowed_paths": [], "forbidden_paths": [], "shared_resources": []}
        path = write_json(self.dir, "task.json", approved_ready("TASK-A", workspace=workspace))
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("allowed_paths", output)

    def test_skip_from_implemented_to_accepted_fails(self) -> None:
        """状态历史里从 Implemented 直接跳到 Accepted 是非法的，要先集成和验证。"""
        history = approved_ready("TASK-A")["state_history"] + [
            {"at": "t3", "actor": "@@BE", "from": "Ready", "to": "Running", "reason": "开工"},
            {"at": "t4", "actor": "@@BE", "from": "Running", "to": "Implemented", "reason": "写完", "evidence_refs": ["自验证输出"]},
            {"at": "t5", "actor": "@@TL", "from": "Implemented", "to": "Accepted", "reason": "看起来没问题", "evidence_refs": ["口头"]},
        ]
        verification = {"required_checks": [], "commands": ["python -m unittest"], "not_run_reason": None}
        path = write_json(self.dir, "task.json", approved_ready("TASK-A", state="Accepted", state_history=history, verification=verification))
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("不能从 Implemented 直接跳到 Accepted", output)

    def test_implemented_without_evidence_fails(self) -> None:
        """进入 Implemented 时没带证据引用，等于只凭一句已完成，要报错。"""
        history = approved_ready("TASK-A")["state_history"] + [
            {"at": "t3", "actor": "@@BE", "from": "Ready", "to": "Running", "reason": "开工"},
            {"at": "t4", "actor": "@@BE", "from": "Running", "to": "Implemented", "reason": "写完了"},
        ]
        path = write_json(self.dir, "task.json", approved_ready("TASK-A", state="Implemented", state_history=history))
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("要带 evidence_refs", output)

    def test_verified_without_commands_fails(self) -> None:
        """状态是 Verified 却没有任何验证命令，不能通过。"""
        history = approved_ready("TASK-A")["state_history"] + [
            {"at": "t3", "actor": "@@BE", "from": "Ready", "to": "Running", "reason": "开工"},
            {"at": "t4", "actor": "@@BE", "from": "Running", "to": "Implemented", "reason": "写完", "evidence_refs": ["e1"]},
            {"at": "t5", "actor": "@@X", "from": "Implemented", "to": "Integrated", "reason": "合并", "evidence_refs": ["e2"]},
            {"at": "t6", "actor": "@@QA", "from": "Integrated", "to": "Verified", "reason": "验完", "evidence_refs": ["e3"]},
        ]
        path = write_json(self.dir, "task.json", approved_ready("TASK-A", state="Verified", state_history=history))
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("verification.commands 是空的", output)

    def test_unsupported_yaml_is_reported_not_misread(self) -> None:
        """用了不支持的多行字符串写法时，报“读不出来”，而不是悄悄读成别的值。"""
        path = self.dir / "task.yaml"
        path.write_text("schema_version: v1\ngoal: |\n  多行内容\n", encoding="utf-8")
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 1)
        self.assertIn("读不出来", output)

    def test_yaml_task_with_comments_and_quotes_parses(self) -> None:
        """手写 YAML 里的注释、引号里的 #、行内列表和中文都能正确读出。"""
        text = (
            "# 注释行\n"
            "schema_version: v1\n"
            "contract_type: task\n"
            "task_id: TASK-YAML  # 行尾注释\n"
            "title: \"标题里有 # 号\"\n"
            "status: draft\n"
            "state: Draft\n"
            "task_type: implementation\n"
            "owner_role: \"@@FE\"\n"
            "goal: 前端页面显示充值状态\n"
            "depends_on: []\n"
            "contract_refs: []\n"
            "risk_tags: [funds, \"order-state\"]\n"
            "parallel:\n"
            "  class: P3\n"
            "  rationale: 尚未分析\n"
            "workspace:\n"
            "  mode: session\n"
            "  allowed_paths:\n"
            "  - WebSite/Web.Admin/src/**\n"
            "acceptance:\n"
            "  - id: AC-001\n"
            "    description: 页面显示状态\n"
            "    verification_method: 手工\n"
            "    evidence_required: true\n"
            "verification:\n"
            "  required_checks: []\n"
            "  commands: []\n"
            "  not_run_reason: null\n"
            "created_by: \"@@X\"\n"
            "approved_by: null\n"
        )
        data = simple_yaml.loads(text)
        self.assertEqual(data["title"], "标题里有 # 号")
        self.assertEqual(data["task_id"], "TASK-YAML")
        self.assertEqual(data["risk_tags"], ["funds", "order-state"])
        self.assertEqual(data["workspace"]["allowed_paths"], ["WebSite/Web.Admin/src/**"])
        self.assertIs(data["acceptance"][0]["evidence_required"], True)
        path = self.dir / "task.yaml"
        path.write_text(text, encoding="utf-8")
        code, output = run_tool(contract_validate, [str(path)])
        self.assertEqual(code, 0, output)

    def test_schema_with_unsupported_keyword_is_rejected(self) -> None:
        """Schema 里用了校验器不认识的关键字时直接报错，避免那条约束被悄悄跳过。"""
        path = self.dir / "bad.schema.json"
        path.write_text(json.dumps({"type": "object", "properties": {"a": {"type": "string", "maxLength": 3}}}), encoding="utf-8")
        with self.assertRaises(schema_check.SchemaError) as caught:
            schema_check.load_schema(path)
        self.assertIn("maxLength", str(caught.exception))


class TaskGraphValidateTests(unittest.TestCase):
    """一组任务契约的整图校验行为。"""

    def setUp(self) -> None:
        """每个用例一个干净的临时目录。"""
        self._tmp = tempfile.TemporaryDirectory()
        self.dir = Path(self._tmp.name)

    def tearDown(self) -> None:
        """删掉临时目录和里面的契约文件。"""
        self._tmp.cleanup()

    def write(self, name: str, data: dict) -> None:
        """在临时目录写一个契约文件。

        Args:
            name: 文件名。
            data: 契约内容。
        """
        write_json(self.dir, name, data)

    def run_graph(self) -> tuple[int, str]:
        """对临时目录跑任务图校验。

        Returns:
            (退出码, 输出文本)。
        """
        return run_tool(task_graph_validate, [str(self.dir)])

    def test_contract_decoupled_frontend_and_backend_pass_and_order_is_layered(self) -> None:
        """前后端按已批准契约并行（P1），QA 任务依赖两者；校验通过，QA 排在第二层。"""
        self.write("api.json", API_CONTRACT)
        refs = [{"id": "API-RECHARGE", "version": "1.0.0", "path": "api.json"}]
        p1 = {"class": "P1", "rationale": "两边只依赖已批准的接口契约", "group_id": "G1"}
        self.write("be.json", approved_ready("TASK-BE", contract_refs=refs, parallel=p1))
        self.write("fe.json", approved_ready("TASK-FE", owner_role="@@FE", contract_refs=refs, parallel=p1))
        self.write("qa.json", make_task("TASK-QA", owner_role="@@QA", task_type="verification", depends_on=["TASK-BE", "TASK-FE"]))
        code, output = self.run_graph()
        self.assertEqual(code, 0, output)
        self.assertIn("第 1 层：TASK-BE", output)
        self.assertIn("第 2 层：TASK-QA", output)

    def test_missing_dependency_fails(self) -> None:
        """依赖了目录里不存在的任务要报错。"""
        self.write("a.json", make_task("TASK-A", depends_on=["TASK-GHOST"]))
        code, output = self.run_graph()
        self.assertEqual(code, 1)
        self.assertIn("TASK-GHOST 不存在", output)

    def test_cycle_fails(self) -> None:
        """A 依赖 B、B 依赖 A 是循环依赖，要报出循环路径。"""
        self.write("a.json", make_task("TASK-A", depends_on=["TASK-B"]))
        self.write("b.json", make_task("TASK-B", depends_on=["TASK-A"]))
        code, output = self.run_graph()
        self.assertEqual(code, 1)
        self.assertIn("有循环依赖", output)

    def test_ready_with_unfinished_dependency_fails(self) -> None:
        """依赖的任务还在 Running，下游任务不能已经是 Ready。"""
        running_history = approved_ready("TASK-A")["state_history"] + [
            {"at": "t3", "actor": "@@BE", "from": "Ready", "to": "Running", "reason": "开工"},
        ]
        self.write("a.json", approved_ready("TASK-A", state="Running", state_history=running_history))
        self.write("b.json", approved_ready("TASK-B", depends_on=["TASK-A"]))
        code, output = self.run_graph()
        self.assertEqual(code, 1)
        self.assertIn("依赖还没满足：TASK-A(Running)", output)

    def test_p0_with_unsatisfied_dependency_fails(self) -> None:
        """判成 P0（完全独立）却有没满足的依赖，判断自相矛盾。"""
        self.write("a.json", make_task("TASK-A"))
        self.write("b.json", make_task("TASK-B", depends_on=["TASK-A"], parallel={"class": "P0", "rationale": "独立", "group_id": None}))
        code, output = self.run_graph()
        self.assertEqual(code, 1)
        self.assertIn("判成 P0，但还有没满足的硬依赖", output)

    def test_concurrent_tasks_with_overlapping_paths_fail_until_ordered(self) -> None:
        """两个可能同时推进的任务改同一目录要报错；加上先后依赖后就不算同时推进。"""
        p0 = {"class": "P0", "rationale": "看起来独立", "group_id": None}
        shared_ws = {"mode": "session", "allowed_paths": ["src/shared/**"], "forbidden_paths": [], "shared_resources": []}
        file_ws = {"mode": "session", "allowed_paths": ["src/shared/util.ts"], "forbidden_paths": [], "shared_resources": []}
        self.write("a.json", make_task("TASK-A", parallel=p0, workspace=shared_ws))
        self.write("b.json", make_task("TASK-B", parallel=p0, workspace=file_ws))
        code, output = self.run_graph()
        self.assertEqual(code, 1)
        self.assertIn("允许修改路径可能重叠", output)

        p2 = {"class": "P2", "rationale": "等 A 改完公共文件", "group_id": None}
        self.write("b.json", make_task("TASK-B", parallel=p2, workspace=file_ws, depends_on=["TASK-A"]))
        code, output = self.run_graph()
        self.assertEqual(code, 0, output)

    def test_sibling_directories_do_not_count_as_overlap(self) -> None:
        """src/a 和 src/ab 是两个目录，不能因为前缀字符相同就判成重叠。"""
        p0 = {"class": "P0", "rationale": "目录不同", "group_id": None}
        self.write("a.json", make_task("TASK-A", parallel=p0, workspace={"mode": "session", "allowed_paths": ["src/a/**"]}))
        self.write("b.json", make_task("TASK-B", parallel=p0, workspace={"mode": "session", "allowed_paths": ["src/ab/**"]}))
        code, output = self.run_graph()
        self.assertEqual(code, 0, output)

    def test_shared_resource_conflict_fails(self) -> None:
        """两个可能同时推进的任务用同一个测试库，要改成先后。"""
        p0 = {"class": "P0", "rationale": "目录不同", "group_id": None}
        self.write("a.json", make_task("TASK-A", parallel=p0, workspace={"mode": "session", "allowed_paths": ["src/a/**"], "shared_resources": ["db:test"]}))
        self.write("b.json", make_task("TASK-B", parallel=p0, workspace={"mode": "session", "allowed_paths": ["src/b/**"], "shared_resources": ["db:test"]}))
        code, output = self.run_graph()
        self.assertEqual(code, 1)
        self.assertIn("共用资源 db:test", output)

    def test_duplicate_task_id_fails(self) -> None:
        """两个文件用了同一个 task_id，依赖关系就说不清了，要报错。"""
        self.write("a.json", make_task("TASK-A"))
        self.write("a-copy.json", make_task("TASK-A"))
        code, output = self.run_graph()
        self.assertEqual(code, 1)
        self.assertIn("重复", output)


if __name__ == "__main__":
    unittest.main()

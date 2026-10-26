"""
rag_index.py 前端源码索引的端到端测试：在临时目录造一个小业务仓，真跑 bootstrap 和 project，
再查生成的 SQLite、摘要 Markdown 和导入报告。

运行：python -m unittest discover -s tools/tests -v
所有输出路径都指到临时目录，用例结束自动删除，不碰 SimpX 真实的 rag/ 和 knowledge/。
"""

from __future__ import annotations

import json
import sqlite3
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import rag_index  # noqa: E402

PROJECT_ID = "demo"
BODY_MARKER = "do-not-copy-7f3a"

DEMO_PROJECT = {
    "display_name": "Demo",
    "solution_file": "Demo.sln",
    "primary_language": "C# + TS",
    "groups": [
        ("frontend", "演示前端", ["web"], "frontend-admin", ["user-data"], "演示用前端目录。"),
        ("backend", "演示后端", ["backend"], "business-agent", ["funds"], "演示用后端目录。"),
    ],
}

SOURCES = {
    "web/src/api/recharge.ts": (
        "import { http } from './http'\n"
        "export interface RechargeOrder { id: string; amount: number }\n"
        "export async function createRechargeOrder(amount: number) {\n"
        f"  const note = '{BODY_MARKER}'\n"
        "  return http.post('/recharge', { amount, note })\n"
        "}\n"
        "export const cancelRecharge = (id: string) => http.post('/cancel', { id })\n"
    ),
    "web/src/views/OrderList.vue": (
        "<template><div>{{ total }}</div></template>\n"
        "<script>\n"
        "export default {\n"
        "  name: 'OrderList',\n"
        "  methods: {\n"
        "    load: function () { return 1 }\n"
        "  }\n"
        "}\n"
        "</script>\n"
    ),
    "web/src/lib/admin.js": (
        "layui.define(['jquery'], function (exports) {\n"
        "  var obj = {\n"
        "    render: function () {},\n"
        "    if: 1\n"
        "  }\n"
        "  function bindEvents() {}\n"
        "  exports('admin', obj)\n"
        "})\n"
    ),
    "backend/RechargeService.cs": "public class RechargeService { public void Pay() { } }\n",
}

SKIPPED_SOURCES = {
    "web/static/js/chunk-vendors.02da486b.js": "export function bundled() {}\n",
    "web/src/jquery.min.js": "function minified(){}\n",
    "web/node_modules/left-pad/index.js": "export function leftPad() {}\n",
    "web/appsettings.Development.json": "{\"ConnectionStrings\": \"secret\"}\n",
}


class FrontendIndexTests(unittest.TestCase):
    """前端文件能被索引、打包产物被跳过、源码正文不进 SimpX、源码改了卡片会过期。"""

    def setUp(self) -> None:
        """在临时目录准备业务仓和 SimpX 输出目录，并把 rag_index 的输出路径全部指过去。"""
        self._tmp = tempfile.TemporaryDirectory()
        base = Path(self._tmp.name)
        self.repo = base / "repo"
        self.simpx = base / "simpx"
        (self.repo / "Demo.sln").parent.mkdir(parents=True)
        (self.repo / "Demo.sln").write_text("", encoding="utf-8")
        for relative, text in {**SOURCES, **SKIPPED_SOURCES}.items():
            path = self.repo / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8")
        big = self.repo / "web/src/lib/extend/echarts.js"
        big.parent.mkdir(parents=True, exist_ok=True)
        big.write_text("export function chart() {}\n" + "//" + "x" * (rag_index.MAX_FRONTEND_FILE_BYTES + 10), encoding="utf-8")

        rag = self.simpx / "rag"
        self._patches = [
            mock.patch.multiple(
                rag_index,
                SIMPX_ROOT=self.simpx,
                RAG_ROOT=rag,
                INDEX_ROOT=rag / "index",
                CORE_DB=rag / "index" / "core.db",
                PROJECT_DB_DIR=rag / "index" / "projects",
                TRACE_DIR=rag / "traces",
                LEGACY_DB=rag / "index.db",
                QDRANT_ROOT=rag / "qdrant",
                REPORT_DIR=rag / "init-reports",
                SUMMARY_ROOT=self.simpx / "knowledge" / "code-summaries",
            ),
            mock.patch.dict(rag_index.PROJECTS, {PROJECT_ID: DEMO_PROJECT}),
        ]
        for patch in self._patches:
            patch.start()
        rag_index.run_bootstrap(quiet=True)

    def tearDown(self) -> None:
        """撤掉路径替换，删除临时业务仓和临时索引。"""
        for patch in reversed(self._patches):
            patch.stop()
        self._tmp.cleanup()

    def run_import(self) -> dict:
        """跑一次项目导入，返回导入报告。

        Returns:
            导入报告 JSON。
        """
        rag_index.run_project(PROJECT_ID, str(self.repo), quiet=True)
        report_path = rag_index.REPORT_DIR / f"{rag_index.STAMP}-project-{PROJECT_ID}.json"
        return json.loads(report_path.read_text(encoding="utf-8"))

    def query(self, sql: str) -> list[tuple]:
        """在临时项目库里执行一条查询。

        Args:
            sql: 查询语句。

        Returns:
            查询结果行。
        """
        conn = sqlite3.connect(rag_index.project_db_path(PROJECT_ID))
        try:
            return conn.execute(sql).fetchall()
        finally:
            conn.close()

    def test_frontend_files_indexed_and_bundles_skipped(self) -> None:
        """ts/vue/js 源码和 C# 都进索引；带哈希的打包文件、.min.js、超大文件、node_modules、配置文件都不进。"""
        report = self.run_import()
        paths = {row[0] for row in self.query("select path from source where source_type = 'code_file'")}
        self.assertEqual(paths, set(SOURCES))

        skip_counts = {}
        for group in report["groups"]:
            for reason, count in group["skip_counts"].items():
                skip_counts[reason] = skip_counts.get(reason, 0) + count
        for reason in ("build_output", "minified_file", "oversize_frontend_file", "pruned_directory", "sensitive_file"):
            self.assertGreaterEqual(skip_counts.get(reason, 0), 1, f"跳过原因 {reason} 没出现：{skip_counts}")

    def test_hashes_and_symbols_recorded(self) -> None:
        """记录的哈希等于文件内容哈希；各语言的关键符号都被提取，关键字不会被当成符号。"""
        self.run_import()
        recharge = self.repo / "web/src/api/recharge.ts"
        expected_hash = rag_index.digest(rag_index.read_text(recharge))
        stored = self.query("select content_hash from source where path = 'web/src/api/recharge.ts'")
        self.assertEqual(stored, [(expected_hash,)])

        symbols = {(row[0], row[1]) for row in self.query("select symbol, symbol_kind from symbol")}
        for expected in [
            ("createRechargeOrder", "ts-symbol"),
            ("RechargeOrder", "ts-symbol"),
            ("cancelRecharge", "ts-symbol"),
            ("OrderList", "vue-symbol"),
            ("load", "vue-symbol"),
            ("admin", "js-symbol"),
            ("render", "js-symbol"),
            ("bindEvents", "js-symbol"),
            ("RechargeService", "csharp-symbol"),
        ]:
            self.assertIn(expected, symbols)
        self.assertNotIn("if", {name for name, _ in symbols})

    def test_source_body_not_copied_into_simpx(self) -> None:
        """源码正文里的字符串不能出现在 SQLite、摘要 Markdown、摘要 manifest 和导入报告里。"""
        self.run_import()
        rows = self.query("select heading, summary, text_ref from chunk")
        self.assertTrue(rows)
        self.assertFalse(any(BODY_MARKER in " ".join(str(cell) for cell in row) for row in rows))
        for path in self.simpx.rglob("*"):
            if path.is_file() and path.suffix in {".md", ".yaml", ".json"}:
                self.assertNotIn(BODY_MARKER, path.read_text(encoding="utf-8"), f"{path} 里出现了源码正文")

    def test_card_turns_stale_after_source_changes(self) -> None:
        """深挖卡片记的哈希和当前源码一致时是 active；源码改了之后重新导入，卡片标成 stale。"""
        recharge = self.repo / "web/src/api/recharge.ts"
        cards = rag_index.SUMMARY_ROOT / PROJECT_ID / "cards"
        cards.mkdir(parents=True)
        card = cards / "recharge.md"
        card.write_text(
            "---\n"
            "summary_id: demo-recharge\n"
            f"project_id: {PROJECT_ID}\n"
            "file_path: web/src/api/recharge.ts\n"
            f"file_hash: {rag_index.digest(rag_index.read_text(recharge))}\n"
            "---\n"
            "# 充值下单\n",
            encoding="utf-8",
        )
        report = self.run_import()
        self.assertEqual([item["status"] for item in report["cards"]], ["active"])

        recharge.write_text(SOURCES["web/src/api/recharge.ts"] + "export function refund() {}\n", encoding="utf-8")
        report = self.run_import()
        self.assertEqual([item["status"] for item in report["stale_cards"]], ["stale"])


if __name__ == "__main__":
    unittest.main()

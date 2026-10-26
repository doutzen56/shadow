"""
过期任务目录清理的行为测试：在临时目录造新旧任务目录，改文件修改时间，跑命令行入口看留下了什么。

运行：python -m unittest discover -s orchestration/tests -v
临时目录由 tempfile 创建，用例结束自动删除。
"""

from __future__ import annotations

import contextlib
import io
import os
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))

import prune_runs  # noqa: E402

NOW = 1_800_000_000.0
DAY = 86400


def make_run(runs: Path, name: str, ages_in_days: list[int]) -> Path:
    """建一个任务目录，里面每个文件按给定天数设置修改时间。

    Args:
        runs: runs 目录。
        name: 任务目录名。
        ages_in_days: 每个文件距 NOW 的天数。

    Returns:
        任务目录路径。
    """
    directory = runs / name
    directory.mkdir(parents=True)
    for index, age in enumerate(ages_in_days):
        path = directory / f"task-{index}.yaml"
        path.write_text("schema_version: v1\n", encoding="utf-8")
        stamp = NOW - age * DAY
        os.utime(path, (stamp, stamp))
    return directory


def run(runs: Path, args: list[str]) -> tuple[int, str]:
    """跑清理命令，收集打印内容。

    Args:
        runs: runs 目录。
        args: 命令行参数。

    Returns:
        (退出码, 打印内容)。
    """
    buffer = io.StringIO()
    with contextlib.redirect_stdout(buffer):
        code = prune_runs.main(args, runs_dir=runs, now=NOW)
    return code, buffer.getvalue()


class PruneRunsTests(unittest.TestCase):
    """只删过期的任务目录，默认不删。"""

    def setUp(self) -> None:
        """每个用例一个干净的临时 runs 目录。"""
        self._tmp = tempfile.TemporaryDirectory()
        self.runs = Path(self._tmp.name) / "runs"
        self.runs.mkdir()

    def tearDown(self) -> None:
        """删掉临时目录。"""
        self._tmp.cleanup()

    def test_default_only_lists_and_deletes_nothing(self) -> None:
        """不加 --apply 时只列出过期目录，什么都不删。"""
        old = make_run(self.runs, "20260901-old", [40])
        code, output = run(self.runs, [])
        self.assertEqual(code, 0)
        self.assertIn("20260901-old", output)
        self.assertTrue(old.exists())

    def test_apply_deletes_only_expired(self) -> None:
        """加 --apply 后删掉过期目录，没过期的留着。"""
        old = make_run(self.runs, "20260901-old", [40, 35])
        fresh = make_run(self.runs, "20261020-fresh", [3])
        code, _ = run(self.runs, ["--apply"])
        self.assertEqual(code, 0)
        self.assertFalse(old.exists())
        self.assertTrue(fresh.exists())

    def test_recently_edited_file_keeps_old_run(self) -> None:
        """目录建得早，但里面有文件最近改过，说明任务还在推进，不能删。"""
        active = make_run(self.runs, "20260801-long-task", [80, 2])
        run(self.runs, ["--apply"])
        self.assertTrue(active.exists())

    def test_days_option_changes_threshold(self) -> None:
        """--days 7 时，10 天没改的目录也算过期。"""
        week_old = make_run(self.runs, "20261016-week", [10])
        run(self.runs, ["--days", "7", "--apply"])
        self.assertFalse(week_old.exists())

    def test_loose_files_in_runs_are_untouched(self) -> None:
        """runs 下的散文件不是任务目录，再旧也不删。"""
        loose = self.runs / "notes.md"
        loose.write_text("说明\n", encoding="utf-8")
        os.utime(loose, (NOW - 90 * DAY, NOW - 90 * DAY))
        run(self.runs, ["--apply"])
        self.assertTrue(loose.exists())

    def test_bad_arguments_fail_without_deleting(self) -> None:
        """参数写错时退出码是 1，不执行删除。"""
        old = make_run(self.runs, "20260901-old", [40])
        code, _ = run(self.runs, ["--days", "abc", "--apply"])
        self.assertEqual(code, 1)
        self.assertTrue(old.exists())

    def test_missing_runs_dir_is_fine(self) -> None:
        """runs 目录还没建时不报错。"""
        code, output = run(self.runs / "not-created", ["--apply"])
        self.assertEqual(code, 0)
        self.assertIn("没有超过", output)


if __name__ == "__main__":
    unittest.main()

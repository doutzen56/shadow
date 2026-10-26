#!/usr/bin/env python3
"""
清理 orchestration/runs/ 下过期的任务目录。

用法：
  python orchestration/tools/prune_runs.py [--days 30] [--apply]

目录里最新的文件超过 --days 天没改过，算过期。默认只列出过期目录，加 --apply 才删除。
只处理 runs/ 下一层的子目录，runs/ 里的散文件和 runs/ 以外的路径都不碰。
"""

from __future__ import annotations

import shutil
import sys
import time
from pathlib import Path

RUNS_DIR = Path(__file__).resolve().parents[1] / "runs"
DEFAULT_DAYS = 30


def last_modified(directory: Path) -> float:
    """取目录里所有文件中最新的修改时间；还在改的长任务因此不会被当成过期。

    Args:
        directory: 一个任务目录。

    Returns:
        最新修改时间（秒）；目录里没有文件时用目录自己的修改时间。
    """
    times = [path.stat().st_mtime for path in directory.rglob("*") if path.is_file()]
    return max(times) if times else directory.stat().st_mtime


def find_expired(runs_dir: Path, days: int, now: float) -> list[tuple[Path, float]]:
    """找出过期的任务目录。

    Args:
        runs_dir: runs 目录。
        days: 多少天没改过算过期。
        now: 当前时间（秒），测试时可以传固定值。

    Returns:
        (过期目录, 距今天数) 列表，按目录名排序。
    """
    if not runs_dir.is_dir():
        return []
    cutoff = now - days * 86400
    expired = []
    for directory in sorted(item for item in runs_dir.iterdir() if item.is_dir()):
        modified = last_modified(directory)
        if modified < cutoff:
            expired.append((directory, (now - modified) / 86400))
    return expired


def main(argv: list[str], runs_dir: Path = RUNS_DIR, now: float | None = None) -> int:
    """命令行入口：列出或删除过期的任务目录。

    Args:
        argv: 命令行参数（不含脚本名）。
        runs_dir: runs 目录，测试时可以换成临时目录。
        now: 当前时间（秒），测试时可以传固定值。

    Returns:
        退出码：正常是 0，参数不对是 1。
    """
    days = DEFAULT_DAYS
    apply = False
    args = list(argv)
    while args:
        arg = args.pop(0)
        if arg == "--apply":
            apply = True
        elif arg == "--days" and args and args[0].isdigit() and int(args[0]) > 0:
            days = int(args.pop(0))
        else:
            print(__doc__.strip())
            return 1
    expired = find_expired(runs_dir, days, time.time() if now is None else now)
    if not expired:
        print(f"没有超过 {days} 天的任务目录")
        return 0
    for directory, age in expired:
        if apply:
            shutil.rmtree(directory)
            print(f"已删除  {directory.name}（{age:.0f} 天没改过）")
        else:
            print(f"过期    {directory.name}（{age:.0f} 天没改过）")
    if not apply:
        print(f"共 {len(expired)} 个过期目录；确认后加 --apply 删除")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

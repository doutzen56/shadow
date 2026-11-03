# -*- coding: utf-8 -*-
"""
从 SiteAdminMenuSeedData.cs 同步 Path -> Component 到本地主库 SysMenu。

用途：
- 改了菜单种子里的 Component，但暂时不想整站重启时跑一次。
- Host.Admin 主库 EnableInitSeed=true 时，正常启动也会按 Id 回写种子；本脚本是补丁通道。

默认库：SimpX.Net/SimpX.Net.Host.Admin/SimpX.Net.db
用法：python tools/sync_site_admin_menu_components.py
可选：python tools/sync_site_admin_menu_components.py --db "绝对路径.db"
"""
from __future__ import annotations

import argparse
import re
import sqlite3
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SEED = ROOT / "SimpX.Net" / "SimpX.Net.Application.Shared" / "SeedData" / "SiteAdminMenuSeedData.cs"
DEFAULT_DB = ROOT / "SimpX.Net" / "SimpX.Net.Host.Admin" / "SimpX.Net.db"


def parse_pairs(seed_text: str) -> list[tuple[str, str]]:
    pairs: list[tuple[str, str]] = []
    for m in re.finditer(r'Path="([^"]+)"[^\}]*?Component="([^"]+)"', seed_text):
        path, comp = m.group(1), m.group(2)
        if path and comp and comp != "/menu/placeholder":
            pairs.append((path, comp))
    return pairs


def main() -> int:
    parser = argparse.ArgumentParser(description="Sync site admin menu Components from seed to SysMenu")
    parser.add_argument("--db", type=Path, default=DEFAULT_DB, help="Sqlite 主库路径")
    args = parser.parse_args()

    if not SEED.is_file():
        print("seed missing:", SEED, file=sys.stderr)
        return 2
    if not args.db.is_file():
        print("db missing:", args.db, file=sys.stderr)
        return 2

    pairs = parse_pairs(SEED.read_text(encoding="utf-8"))
    conn = sqlite3.connect(str(args.db))
    cur = conn.cursor()
    updated = 0
    for path, comp in pairs:
        n = cur.execute(
            "UPDATE SysMenu SET Component=? WHERE Path=? AND IFNULL(Component,'')<>?",
            (comp, path, comp),
        ).rowcount
        updated += n
    conn.commit()
    left = cur.execute(
        "SELECT COUNT(1) FROM SysMenu WHERE Permission LIKE 'site:%' AND Component LIKE '%placeholder%'"
    ).fetchone()[0]
    conn.close()
    print(f"pairs={len(pairs)} updated={updated} site_placeholders_left={left} db={args.db}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

# -*- coding: utf-8 -*-
"""Inventory BetWin.Handler.Request private Result paths vs SimpX WebApi OldApi routes."""
from __future__ import annotations

import re
from pathlib import Path

handler_root = Path(r"F:\betwin5.0\Library\BetWin.Handler.Request")
webapi_root = Path(r"F:\SimpX.Net\SimpX.Net\SimpX.Net.Application.WebApi")

# old: namespace BW.Handler.Request.X.Y → /request/x/y/method
# file path relative under Handler.Request mirrors URL segments

method_re = re.compile(r"private\s+Result\s+(\w+)\s*\(", re.I)
route_re = re.compile(
    r'Http(?:Post|Get)\("(/request/[^"]+)"\)',
    re.I,
)

hung: set[str] = set()
for cs in webapi_root.rglob("*.cs"):
    text = cs.read_text(encoding="utf-8", errors="ignore")
    for m in route_re.finditer(text):
        hung.add(m.group(1).lower().rstrip("/"))

expected: set[str] = set()
for cs in handler_root.rglob("*.cs"):
    rel = cs.relative_to(handler_root).as_posix()
    if "/obj/" in rel or "/bin/" in rel:
        continue
    parts = rel[:-3].split("/")  # drop .cs
    # skip designer-ish
    text = cs.read_text(encoding="utf-8", errors="ignore")
    for m in method_re.finditer(text):
        method = m.group(1).lower()
        segs = [p.lower() for p in parts if p]
        path = "/request/" + "/".join(segs + [method])
        expected.add(path)

miss = sorted(expected - hung)
print(f"expected={len(expected)} hung={len(hung)} miss={len(miss)}")
print("--- miss sample ---")
for p in miss[:80]:
    print(p)
if len(miss) > 80:
    print(f"... +{len(miss)-80} more")

---
name: learn-and-update
description: >-
  Persist a confirmed preference, correction, ADR, or learning into KB_ROOT
  (E:\shadow) or the current repo .cursor. Use when user says 人生级记住,
  本仓记住, 记一条 ADR, learn-and-update, or confirms end-of-task沉淀 candidates.
---

# learn-and-update

## Hard rules

1. **Never write under `E:\shadow` (KB_ROOT) unless the user confirmed** (explicit口令 or answered yes to收尾候选).
2. Do **not** dump long chat into journal; write short actionable notes.
3. Do **not** inflate User Rules or alwaysApply rules with long text.
4. Route sensitived商户/生产细节 only to **current repo** `.cursor/LEARNINGS.md`, never to shadow.

## End-of-task prompt (tier 1)

After important work (feature, hotfix, architecture discussion, clear correction), list **1–3 candidates** (人生级 / 本仓 / ADR) and ask whether to persist. If user says 不用记, stop. If user picks items, write only those.

## Routing

| Signal | Destination |
|--------|-------------|
| 人生级记住 / confirmed 人生级 | `$KB_ROOT` principles/craft/journal |
| 记一条 ADR | `$KB_ROOT/architecture/adr/` + update INDEX |
| 提炼成架构模式 | `$KB_ROOT/architecture/patterns/` |
| 本仓记住 / confirmed 本仓 | current repo `.cursor/rules` or `LEARNINGS.md` or skill |
| Explicit口令 without prior ask | treat as confirmed |

Default `KB_ROOT` = `E:\shadow` unless User Rules override.

## After write

Tell user which file changed; remind them to commit/push **shadow** themselves if desired. Never push business repos.

# HX.MatchingApi — Agent notes

## Layout

- `HX.Matching.sln` — solution entry
- `HX.Matching.Admin.WebApi` — admin API
- `HX.Matching.MerchantAdmin.WebApi` — merchant admin API
- `HX.Matching.MerchantApi` — merchant open API
- `HX.Matching.PartnerApi` — partner interchange API
- `HX.Matching.Quartz.Job` — jobs / MQ consumers / Telegram
- `HX.Matching.ServiceCore` — business core
- `HX.Matching.Model` / `Infrastructure` / `Repository` — shared layers

## Docs

- `docs/商户API对接文档.md` (+ 代收/代付)
- `docs/合作方互通API对接文档.md`
- `docs/线程池饥饿事故复盘与修复说明.md`
- `docs/architecture/README.md`

## Workflow

- Local edits only; **no git push / no PR** from Agent on this repo
- Human reviews before you commit/push
- Personal shadow KB: `KB_ROOT` (default `E:\shadow`) — cross-repo memory
- This repo's Agent files (`.cursor/`, `AGENTS.md`, `docs/architecture/`) are an **overlay** from `E:\shadow\projects\hx-matching`; truth source is shadow, synced via `sync-hx-matching-overlay.ps1`, locally gitignored — do not commit them to business git

## 收尾沉淀

Important task end: list 1–3 candidates (人生级 / 本仓 / ADR) and ask. **Do not write `E:\shadow` until confirmed.** Explicit `人生级记住` / `记一条 ADR` counts as confirmation.

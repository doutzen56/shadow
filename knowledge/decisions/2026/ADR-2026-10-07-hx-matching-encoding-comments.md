# ADR-2026-10-07：HX.Matching 编码与注释约定（记住）

- 日期：2026-10-07
- 来源：用户「记住：编码规范，注释规范，核心业务也要写注释」
- 项目：`projects/examples/hx-matching`
- 状态：生效

## 决定

1. 源码与文档统一 **UTF-8（无 BOM）**；读写脚本必须显式指定 UTF-8（见 SimpX `.cursor/rules/encoding.mdc`）。
2. 新写或改动的方法必须有完整 XML 文档注释；核心业务（切库、租户映射、MQ 头、Job 分桶、缓存分租户、资金与验签）要写清「为什么」（见 `.cursor/rules/coding.mdc`）。
3. **业务仓禁止**写入 `AGENTS.md` / `.cursor/rules` / `.cursor/skills` / `.cursor/agents`（见 `workspace-hosting.mdc`）。约定只落 SimpX；2026-10-07 已删除业务仓误放的 `coding-comments.mdc`。
4. **注释禁止嵌入改造文档细节**（章节号 `15 §x`、拍板 `11 #n`、验收号 `S01-API-*` / `S01-ASYNC-*`）。用白话写原因；编号只在文档仓与 `[Acceptance]` 上。2026-10-07 已对 AdminV2 / 多租户底座 / V2.Tests 叙述性注释做过一轮清扫。

## 后果

- Agent 在 `HX.MatchingApi` 不得再新建 `.cursor/rules`。
- 注释乱码（`?/summary>`、`U+FFFD`）视为未完成，改完后须按 UTF-8 自检可读中文。
- 新写注释若再出现 `§` / `S01-API-` 等文档引用，视为未按规范完成。
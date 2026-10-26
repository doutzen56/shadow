# 知识增长循环

## 目标

让任务产物按 `@@TL` 的判断进入 raw、candidate 或正式知识，用户周审修正，避免知识库被幻觉和临时结论污染。

## 状态

- raw：任务原始摘要和证据来源。
- candidate：候选知识，待 `@@TL` 判断入库。
- confirmed：`@@TL` 按证据确认，或用户说「记住：」「记一条 ADR」后直接入库。
- deprecated：过期或被替代。
- rejected：明确不沉淀。

## 标准步骤

1. 任务结束时，`@@X` 汇总沉淀线索（复用结果、失败教训、新模式、反例）。有任务目录（`orchestration/runs/`）时，线索来自收口摘要：目标、结果、证据位置、学到了什么，三到五行；可复用的拆法写成模式卡并附任务图骨架。
2. `@@TL` 按 `knowledge/candidates/README.md` 的“使用时机”判断：不沉淀、写 raw、写 candidate，还是直接入正式知识。这次出现过值得沉淀的线索、但决定不沉淀时，一句话写原因；没有线索就不提沉淀。
3. 判断要写时直接写 SimpX，不再询问用户；写 candidate 先走 `workflows/knowledge-candidate-intake/README.md`。
4. 直接入正式知识时，必须满足 `config/rag-growth.yaml` 的 `promotion_requirements`，并标 `confirmed_by: "@@TL"`、`user_reviewed: false`。
5. 每周由 `agent-governance` 汇总候选和 `@@TL` 已入库条目。
6. `@@BA` / `@@FE` / `@@BE` / `@@QA` / `@@DEVOPS` 按职责补齐抽象、证据和验证提示。
7. 用户周审：保留、修正、合并、驳回或归档；复核后改 `user_reviewed: true`。
8. 入库、修正或归档后触发 `knowledge-index-maintenance`。

## 输出

- 沉淀判断结论和原因。
- raw 记录、candidate 卡或正式知识条目路径。
- 周审清单。
- 索引维护触发结果。

## 收口检查

- 没有证据来源的内容不能进 candidate，更不能入正式知识。
- 不能写 `.cursor/rules/`；规则变更走 `agent-governance`。
- 业务代码不能整段进入知识主体。
- 实际没写文件时，不能写“已沉淀”。

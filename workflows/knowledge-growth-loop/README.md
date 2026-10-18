# 知识增长循环

## 目标

让任务产物自动进入 raw 或 candidate，并通过人工确认进入正式知识，避免知识库被幻觉和临时结论污染。

## 状态

- raw：任务原始摘要和证据来源。
- candidate：候选知识，待 review。
- confirmed：用户或 `@@TL` 确认可复用。
- deprecated：过期或被替代。
- rejected：明确不沉淀。

## 标准步骤

1. 任务结束时，`@@X` 判断是否有高价值经验。
2. 有价值但未确认时，先进入 `workflows/knowledge-candidate-intake/README.md`，生成候选卡。
3. 写入 `knowledge/raw/` 或 `knowledge/candidates/`。
4. 每周由 `agent-governance` 汇总候选。
5. `@@BA` / `@@BE` / `@@DEVOPS` 按职责补齐抽象、证据和验证提示。
6. `@@TL` 给出 promote、merge、reject、archive 建议。
7. 只有用户明确确认后，才写入正式 `knowledge/`。
8. 入正式库后触发 `knowledge-index-maintenance`。

## 输出

- raw 记录或 candidate 卡。
- 周审清单。
- promote/merge/reject/archive 决策。
- 索引维护触发结果。

## 收口检查

- 没有证据来源的候选不能 promote。
- 未经用户明确确认，不能写正式知识。
- 业务代码不能整段进入知识主体。
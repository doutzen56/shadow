# 知识候选接入

## 目标

把任务结束后的高价值经验转成可审计候选知识，等待周审和用户确认。

## 输入

- 任务目标和结论。
- 证据来源。
- 知识复用检查结果。
- 是否适合跨项目复用。

## 标准步骤

1. `@@TL` 已在 `knowledge-growth-loop` 判断要写 candidate；`@@X` 整理任务结论和证据。
2. 按 `templates/knowledge-pattern-card` 或 ADR/incident 模板提炼候选。
3. 标注 project_scope、role_scope、task_scope、risk_tags 和 evidence_refs。
4. 写入 `knowledge/candidates/`。
5. 登记到周审清单。

## 收口检查

- 没有证据来源不得进入候选。
- 仅项目局部经验不得伪装成跨项目模式。
- 转正式知识由 `@@TL` 按证据确认，用户周审修正。
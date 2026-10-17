# 工作流共用检查

本目录放所有工作流都要复用的检查项。

## 使用规则

每个工作流开始前，先完成：

1. `task-intake.md`：确认任务入口。
2. `permission-check.md`：确认权限等级。
3. `evidence-check.md`：确认需要什么证据。
4. `validation-check.md`：确认按任务级别怎么验证。
5. `role-handoff.md`：确认角色之间如何会话内交接。
6. `completion-check.md`：确认怎样才算完成。

## 优先级

这些检查项服务于 `.cursor/rules/` 的硬规则，不能覆盖硬规则。

如果当前工作流和本目录冲突，以 `config/active-rules.yaml` 登记的生效规则为准。

## 输出要求

Agent 开工前要说明：

- 任务类型。
- 权限等级。
- 影响范围。
- 风险等级。
- 下一步动作。
- 是否需要用户拍板。

# 策略原则

## 当前定位

`core/policies/` 是长期原则参考，不是当前 agent 的直接执行规则。

当前真正生效的规则以 `config/active-rules.yaml` 登记的 `.cursor/rules/*.mdc` 为准。

如果本目录内容和 `config/active-rules.yaml` 冲突，按 `config/active-rules.yaml` 执行。

本目录适合沉淀稳定后的原则，不能替代任务入口、权限等级、证据标准和完成定义。

## 文档范围

- `autonomy.md`：agent 自主边界。
- `change-scope.md`：修改范围。
- `permission.md`：工具和操作权限。
- `production-access.md`：生产访问。

## 使用规则

- 日常执行先看 `.cursor/rules/`。
- 本目录只解释策略原则。
- 稳定后的策略可以再同步到 `.cursor/rules/` 或 `config/active-rules.yaml`。

# 工程原则

本目录存放跨项目复用的工程判断原则。

## 当前定位

这些原则用于指导架构、实现、验证和运维判断，不是当前 agent 的直接执行规则。

当前生效规则以 `config/active-rules.yaml` 登记的 `.cursor/rules/*.mdc` 为准。

## 文档范围

- `abstraction.md`：抽象边界。
- `maintainability.md`：可维护性。
- `observability.md`：可观测性。
- `performance.md`：性能。
- `reliability.md`：可靠性。
- `simplicity.md`：简单性。

## 使用规则

- 做架构取舍时可以引用本目录。
- 写实现方案时优先使用项目档案和工作流，不要只引用原则。
- 原则和生效规则冲突时，以 `active-rules.yaml` 为准。

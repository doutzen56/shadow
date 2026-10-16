# 核心宪章

本目录存放 SimpX 的长期原则来源。

## 当前定位

这些文档用于解释 SimpX 为什么这样要求，不直接替代 `.cursor/rules/` 中的执行规则。

当前 agent 必须执行的规则，以 `config/active-rules.yaml` 登记的 `.cursor/rules/*.mdc` 为准。

## 文档范围

- `ai.md`：人工智能行为边界。
- `architecture.md`：架构判断原则。
- `coding.md`：编码和注释原则。
- `engineering.md`：工程治理原则。
- `git.md`：版本管理原则。
- `security.md`：安全原则。
- `testing.md`：测试原则。

## 使用规则

- 写长期规范时，可以从这里抽象原则。
- 日常执行时，不要用本目录覆盖 `active-rules.yaml`。
- 本目录内容变化后，要检查 `.cursor/rules/` 是否需要同步。

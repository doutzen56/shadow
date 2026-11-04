# simpx-testing

## 状态

已启用。

## 用途

用于执行断言式验证并产出期望值/实际值证据报告；有页面项时默认先调 Playwright，仅当其不可用时再调内置 Browser。

## 触发方式

- 代码改动后进入测试环境验证阶段。
- 需要判断是否满足交付口径。
- 断言或场景含界面操作时（无需用户再说「用 Playwright」）。

## 关联角色与流程

- 角色：`@@QA`（主用）、`@@BE` / `@@FE`（自验证和断言）、`@@DEVOPS`（环境证据格式）、`@@TL`
- 流程：`workflows/release`、`workflows/feature-development`；细则见 `workflows/_shared/validation-check.md`

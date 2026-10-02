# SimpX 项目启动技能

## 适用场景

- 新接入一个业务仓时使用。
- 需要建立项目档案映射和环境配置骨架时使用。
- 任务开始前需要快速完成上下文就绪时使用。

## 前置输入

- 工程名（仓库根目录的解决方案文件名，例如 `HX.Matching.sln`），不看工作区目录名和盘符。
- 对应项目档案路径（相对 SimpX 根：`projects/examples/<project-id>`）。
- 目标环境集合（按项目 `envs.yaml` 的 `default_envs`，不要求 `dev/test/uat` 齐全；例如 hx-matching 只有 `dev/uat`，betwin 只有 `test`）。

## 执行步骤

1. 确认是否存在项目档案（`README.md`、`profile.yaml`、`boundaries.md`）。
2. 校验 `workspace-hosting.mdc` 中按工程名的映射是否正确。
3. 校验项目环境配置文件是否存在：`envs.yaml`、`envs.secrets.yaml`（本地）。
4. 若文件缺失，按模板提示补齐最小骨架。
5. 输出项目初始化检查结果和缺口清单。

## 输出格式

- 项目标识
- 档案检查结果
- 环境配置检查结果
- 缺口与下一步动作

## 失败与升级

- 找不到项目映射时，升级给 `@@X` 和 `@@TL`。
- 关键档案缺失时，标记阻塞，不进入后续实现流程。

## 禁止事项

- 不在业务仓写入 `.cursor/agents`、`.cursor/rules`、`.cursor/skills`。
- 不把密钥写入仓库文件。

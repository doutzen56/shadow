# 角色一致性校验

## 目标

确保角色清单、召唤标签、角色文件和核心规则一致，避免出现“有标签但没有角色定义”的情况。

## 校验范围

每次新增、删除、改名角色时，必须同时检查：

- `config/simpx.yaml`
- `README.md`
- `.cursor/rules/simpx-core.mdc`
- `agents/<role>/agent.yaml`
- `agents/<role>/system.md`
- `core/roles/<role>/README.md`（如果存在）

## 必须满足

- `config/simpx.yaml` 里的正式成员，在 `agents/` 下必须有目录。
- 每个正式角色必须有 `agent.yaml` 和 `system.md`。
- 召唤标签必须在 `README.md`、`simpx.yaml`、`simpx-core.mdc` 中一致。
- `simpx-core.mdc` 的角色加载路径必须真实存在。
- 辅助角色也要说明启用方式和调用边界。

## 发现不一致时

按以下方式处理：

1. 先停止新增功能改动。
2. 列出不一致项。
3. 判断哪个文件是源头。
4. 同步其余文件。
5. 重新读取校验结果。

## 输出格式

- 角色名。
- 召唤标签。
- 是否在 `simpx.yaml`。
- 是否在 `README.md`。
- 是否有 `agent.yaml`。
- 是否有 `system.md`。
- 是否被 `simpx-core.mdc` 正确引用。
- 结论：通过 / 阻塞。

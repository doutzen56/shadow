# 全局配置

本目录存放 SimpX 的全局配置。

## 当前生效文件

- `simpx.yaml`：成员、召唤标签、默认协作顺序和建设阶段。
- `active-rules.yaml`：当前真正生效的规则清单和优先级。
- `knowledge-structure.yaml`：长期知识库的分层、检索和沉淀规则。
- `environments.yaml`、`defaults.yaml`：通用环境和默认配置参考。

## 使用规则

- Agent 开始工作前必须读取 `active-rules.yaml`。
- 角色和召唤标签以 `simpx.yaml` 为准。
- 规则冲突时，以用户当前消息和 `active-rules.yaml` 为准。
- 本目录不得存放密钥、令牌、生产密码或连接串。

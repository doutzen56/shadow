# 演练：HX.Matching 新增支付渠道的任务图

把 `workflows/feature-development/examples/hx-matching-add-channel.md` 拆成可校验的任务图，用来演练 2.0 的契约和任务图校验。这是演练样例，不是真实需求；允许修改路径只写到 HX.Matching 的顶层项目目录。

## 任务

| 任务 | owner | 并行类别 | 依赖 | 说明 |
| --- | --- | --- | --- | --- |
| `TASK-CH-MODEL` | `@@BE` | P1 | 无 | 渠道配置实体和内部状态映射，按 `DATA-CHANNEL-STATUS` 实现 |
| `TASK-CH-ADAPTER` | `@@BE` | P1 | 无 | 下单、回调验签、幂等、状态推进；按两份已批准契约实现，不等 MODEL 的代码 |
| `TASK-CH-QUERY-JOB` | `@@BE` | P3 | ADAPTER | 查单补偿任务，要调用 ADAPTER 的查单方法 |
| `TASK-CH-ENV` | `@@DEVOPS` | P0 | 无 | 测试环境渠道配置、回调地址和 MQ |
| `TASK-CH-VERIFY` | `@@QA` | P3 | 以上全部 | 金额精度、重复回调、乱序、伪造签名、启停等场景验证 |

## 契约

- `api-channel-callback.yaml`：渠道回调接口契约（`API-CHANNEL-CALLBACK@1.0.0`，已批准）。
- `data-channel-status.yaml`：渠道状态到平台订单状态的映射（`DATA-CHANNEL-STATUS@1.0.0`，已批准）。

## 校验

```text
python orchestration/tools/task_graph_validate.py orchestration/examples/hx-matching-add-channel
```

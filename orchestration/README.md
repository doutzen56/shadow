# 任务编排

编排只负责结构化任务、依赖、状态、权限和调度检查，不替领域角色做专业设计。

## 现在有什么

| 内容 | 路径 | 状态 |
| --- | --- | --- |
| 并行判断规则 | `policies/parallelism.yaml` | 已启用 |
| 任务状态和合法跳转 | `policies/task-states.yaml` | 已启用 |
| 契约校验 | `tools/contract_validate.py` | 已启用 |
| 任务图校验 | `tools/task_graph_validate.py` | 已启用 |
| 演练样例 | `examples/` | 已启用 |
| 运行中的任务目录 | `runs/`（不进 git） | 已启用 |
| 过期任务目录清理 | `tools/prune_runs.py` | 已启用 |
| 状态变更工具 `task_state.py` | — | 规划 |
| 改动文件范围检查 `changed_files_check.py` | — | 规划 |
| 验证报告生成 `verification_report.py` | — | 规划 |
| 隔离工作区、真实并行执行 | — | 规划 |

规划项没落地前，不能说“已并行执行”“状态机已拦截”；没有真实执行器时只出任务图，在会话里按顺序推进。

## 基本规则

- 任务图必须是有向无环图，不能循环依赖。
- 只有依赖都满足、任务契约和引用的契约都 `approved` 的任务才能进 `Ready`。
- 每次状态变化记一条 `state_history`：谁改的、什么时候、从什么到什么、原因、证据。
- `Implemented` 不代表集成成功；`Integrated` 不代表测试通过；`Verified` 不代表业务验收完成；只有 `Accepted` 才算最终完成。
- 并行任务要有允许修改路径和明确交付物；将来真正并行时还要有隔离工作区。
- 集成失败回到责任任务，不把所有失败都交给 `@@TL`。
- 先只做本地能重复运行的校验器和状态文件，不引入消息队列、分布式调度或数据库。

## 任务目录

- 位置：`orchestration/runs/<日期-简称>/`，如 `runs/20261026-betwin-recharge/`。一次需求一个目录，放这次的任务契约和只为这次约定的契约。
- 不进 git（见 `.gitignore`），不写回业务仓，第一次用时再建。
- 收口：全部任务到 `Accepted` 或 `Cancelled` 后，`@@X` 写三到五行收口摘要（目标、结果、证据位置、学到了什么），交 `@@TL` 按 `workflows/knowledge-growth-loop` 判断沉淀；长期有效的接口或数据约定提升进项目档案或 ADR；然后删除整个任务目录。
- 兜底：忘了删的，目录里最新文件超过 30 天没改过就算过期。`python orchestration/tools/prune_runs.py` 只列出过期目录，加 `--apply` 才删除。

## 状态

```text
Draft → Planned → (ContractReady) → Ready → Running → Implemented → Integrated → Verified → Accepted
另有 Blocked、Failed、Cancelled；合法跳转见 policies/task-states.yaml
```

## 并行类别

| 类别 | 含义 | 能否同时推进 |
| --- | --- | --- |
| P0 | 完全独立：没有未满足依赖，文件和资源不冲突 | 能 |
| P1 | 契约解耦：共享契约已批准，双方按契约各自实现 | 能，要有 `contract_refs` |
| P2 | 部分并行：先拆出可独立的部分，依赖部分继续等 | 拆开后能 |
| P3 | 强依赖串行：依赖消不掉或资源隔离不了 | 不能 |

“不同文件”不等于可以并行，“同一个仓库”也不等于不能并行。初始一律判 P3，做完依赖分析再改。

## 校验工具检查什么

`contract_validate.py <文件或目录>`：

- 字段齐全、类型和枚举对（按 `contracts/schemas/`）。
- 契约批准了要写 `approved_by`。
- 任务进 `Ready` 及以后状态时：任务契约要 `approved`；引用的契约文件要存在、版本对得上、状态是 `approved`；实现类任务要写允许修改路径。
- P1 任务要有 `contract_refs`。
- 到 `Verified` / `Accepted` 时，要有验证命令或写清没跑的原因。
- `state_history` 前后衔接、每一步跳转合法、最后一条等于当前状态。

`task_graph_validate.py <任务目录>`：

- 先对每个任务跑一遍上面的契约校验。
- `task_id` 不重复，依赖的任务都存在，不依赖自己，没有循环依赖。
- 进 `Ready` 及以后状态的任务，依赖都到了 `Implemented` 或之后。
- 判了 P0 / P1 的任务没有未满足的依赖。
- 可能同时推进的任务（彼此没有先后依赖、都没结束、类别是 P0/P1/P2）：允许修改路径不能重叠，共享资源不能同名。
- 输出按依赖排好的推进顺序，每一层里的任务彼此没有依赖。

## 用法

```text
python orchestration/tools/contract_validate.py contracts/templates/task.yaml
python orchestration/tools/task_graph_validate.py orchestration/examples/hx-matching-add-channel
python orchestration/tools/prune_runs.py [--days 30] [--apply]
python -m unittest discover -s orchestration/tests -v
```

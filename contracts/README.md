# 契约目录

契约是角色之间、以及角色和校验工具之间可以检查的约定，不是自然语言说明的别名。写成文件的契约可以用 `orchestration/tools/contract_validate.py` 校验。

## 契约类型

| 类型 | `contract_type` | 用途 | Schema |
| --- | --- | --- | --- |
| 任务契约 | `task` | 一个任务的目标、owner、依赖、允许修改路径、验收条件和状态 | `schemas/task-contract.schema.json` |
| 接口契约 | `api` | 接口的治理信息：版本、状态、消费者；接口本身优先用 OpenAPI 等标准描述 | `schemas/contract-header.schema.json`（只校验通用头） |
| 数据契约 | `data` | 表结构、消息体、缓存键等数据约定 | 同上 |
| 执行契约 | `execution` | 执行环境、工作区、共享资源约定 | 同上 |
| 验收契约 | `acceptance` | 验收标准和证据要求 | 同上 |

接口、数据、执行、验收契约目前只校验通用头字段（`contract_id`、`version`、`status`、`owner_role` 等），正文格式还没有专门 Schema。

## 通用要求

- 每份契约要有唯一 ID、版本、状态和 owner。
- 任务契约还要有依赖、允许修改路径、验收条件和证据要求。
- 契约状态：`draft → review → approved → superseded`；被拒绝的标 `rejected`，不覆盖历史版本。
- 只有 `approved` 的契约才能解除对应的执行依赖：任务引用的契约没批准，任务不能进 `Ready`。
- 批准时要写 `approved_by`。
- 修改已批准的契约要提变更：列出消费者、兼容方式、迁移方案和要重新验证的范围；旧版本标 `superseded`，新版本另起文件或升版本号，不静默覆盖。
- 接口的具体格式优先用 OpenAPI 等标准；SimpX 的 Schema 只管治理信息和校验约束，不重新发明接口描述标准。

## 什么时候要落文件

- 只有一个任务、没有跨领域契约：在会话交接块里写 `task_id`、允许修改路径和验收条件就行，不强制落文件。
- 拆成两个以上任务、有跨领域或公共契约、要跨会话追踪：要落文件，并跑校验工具。

## 放在哪

- 模板：`contracts/templates/`。
- 精选示例：`orchestration/examples/`，由 `@@TL` 挑选，进 git。
- 运行中的任务和只为这次需求约定的契约：`orchestration/runs/<日期-简称>/`（如 `20261026-betwin-recharge/`），一次需求一个目录，不进 git，不写回业务仓。收口后删除，超过 30 天的由 `orchestration/tools/prune_runs.py` 清理。
- 任务结束后还会长期使用的接口或数据约定，不跟任务目录一起删：收口时提升进项目档案（如 `boundaries.md`）或写成 ADR。

## 文件格式

YAML 只支持常用写法：块状映射和列表、`- key: value` 形式的列表项、`[]` / `[a, b]` 行内列表、`{}`、带引号或不带引号的字符串、整数、`true` / `false`、`null`。

不支持多行字符串（`|`、`>`）、锚点（`&`、`*`）、标签（`!`）和 Tab 缩进，遇到会直接报错。以 `@` 开头的值（如 `"@@BE"`）要加引号。版本号要加引号（`"1.0.0"`），不加引号的 `1.0` 会被当成数字。

也可以用 `.json` 写，字段相同。

## 校验命令

```text
python orchestration/tools/contract_validate.py <文件或目录>
python orchestration/tools/task_graph_validate.py <任务目录>
```

两条命令都会把每个文件的结果打出来，有错误时退出码是 1。

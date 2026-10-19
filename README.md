# SimpX

SimpX 是一个个人智能技术团队框架，用来把技术负责人的判断、智能角色分工、工程流程、项目风格和长期知识沉淀组织起来。

它不是一次性的提示词集合，而是一套可复用、可演进、可沉淀的个人技术组织系统。

## 一阶段目标

一阶段先让团队能够稳定工作：

- 明确技术负责人的职责与决策边界。
- 固定核心智能成员：调度者、技术负责人、后端架构、后端实现、环境与交付。
- 建立业务交付型与架构治理型两类项目打法。
- 使用统一工作流完成需求、设计、实现、验证和交付。
- 把经验、决策和教训沉淀到长期知识目录。

## 目录结构

| 目录 | 放什么 |
| --- | --- |
| `.cursor/rules/` | 硬规则，每次对话都要遵守；生效清单登记在 `config/active-rules.yaml` |
| `agents/` | 角色定义，每个角色一个 `system.md` |
| `config/` | 全局配置：成员、生效规则、知识结构、RAG 各环节配置 |
| `core/` | 原则、宪章和长期设计，只做参考，不能覆盖生效规则 |
| `workflows/` | 工作流，`_shared/` 是所有工作流共用的检查项 |
| `projects/` | 项目档案（`examples/`）和项目风格说明（`profiles/`） |
| `knowledge/` | 长期知识：模式、决策、事故、术语、索引、代码摘要、候选和原始记录 |
| `templates/` | 输出模板，只有工作流或角色明确引用时才用 |
| `tools/` | 自研工具，目前是 RAG 索引脚本 `rag_index.py` |
| `rag/` | RAG 的初始化报告、Qdrant 占位和本地索引 |

改动记录见 `CHANGELOG.md`，参与方式见 `CONTRIBUTING.md`。

## SimpX 1.0 成员

| 正式编号 | 召唤标签 | 职责 |
| --- | --- | --- |
| `X-01` | `@@X` | 团队调度者 |
| `TL-01` | `@@TL` | 技术负责人 |
| `BA-01` | `@@BA` | 后端架构工程师 |
| `BE-01` | `@@BE` | 后端实现工程师 |
| `DEVOPS-01` | `@@DEVOPS` | 环境与交付工程师 |

正式编号用于文档、职责边界和长期沉淀；召唤标签用于日常对话和任务分派。成员和标签以 `config/simpx.yaml` 为准，不在清单里的标签一律不认。

## 辅助成员

| 标识 | 用途 | 启用方式 |
| --- | --- | --- |
| `reviewer` | 高风险改动审查与缺陷识别 | 按 `@@TL` 或 `@@X` 指派 |
| `researcher` | 方案调研、实验与决策依据补充 | 按 `@@TL` 或 `@@X` 指派 |

## 如何召唤成员

```text
@@X：按项目风格调度角色，产出执行顺序。
@@TL：做目标确认、优先级判断和最终取舍。
@@BA：分析架构边界和技术风险。
@@BE：拆后端实现步骤和影响范围。
@@DEVOPS：配置环境并执行测试环境交付验证。
```

需要多人协作时，可以组合召唤：

```text
@@TL + @@BA + @@BE：一起评估后端需求方案。
@@BE + @@DEVOPS：实现后完成环境验证与交付检查。
@@X：按项目风格完成任务分析、方案、验证和沉淀。
```

`@@X` 是唯一入口。把任务交给 `@@X` 后，它自己判断任务类型、权限和链路，按阶段切换角色职责，不需要你再手动召唤其他成员；只有碰到要你拍板的事情才会停下来问。

## Agent 工作入口

Agent 开始工作前，按以下顺序读取和判断：

1. 确定 SimpX 根目录：以本次对话里用户第一次让读的目录为准，用户没指定就先问。
2. `.cursor/rules/`：当前必须执行的硬规则。
3. `config/active-rules.yaml`：当前生效规则清单和优先级。
4. `config/simpx.yaml`：成员、召唤标签和默认协作顺序。
5. 当前业务仓映射到的项目档案：`README.md`、`profile.yaml`、`boundaries.md`，映射方式见下面的“项目接入与映射”。
6. `workflows/_shared/`：任务入口、权限、证据、验证、交接和完成检查。
7. 当前任务对应的 `workflows/<name>/README.md`。
8. 本次要用到的每个角色的 `agents/<role>/system.md`：用户召唤的、`@@X` 链路里要进入职责阶段的、被指派的 `reviewer` / `researcher`。
9. 任务涉及知识检索、沉淀、RAG、项目接入或代码摘要时，再读 `config/knowledge-structure.yaml` 和对应的 `config/rag-*.yaml`。

规则冲突时按这个顺序裁决：用户当前消息 > `config/active-rules.yaml` 登记的生效规则 > 当前源码 > 项目档案 > 工作流 > 角色定义 > 模板和长期知识。

## 任务类型与工作流

先定任务类型，再按 `config/rag-x-routing.yaml` 的 `task_routing` 选工作流。判断不了的时候问用户，不自己猜。

| 任务类型 | 工作流 |
| --- | --- |
| 只读分析 | 纯分析或问答不套工作流；要出方案结论时走 `requirement-to-design` |
| 方案设计 | `requirement-to-design` |
| 缺陷定位 | `bug-fix` |
| 代码实现 | 方案已定、要拆编码任务走 `design-to-code`；端到端功能走 `feature-development` |
| 代码审查 | `code-review` |
| 测试验证 | 实现还没完成走 `feature-development`；实现完成后复测或交付环境走 `release` |
| 发布交付 | `release` |
| 事故处理 | `incident-response` |
| 规则或文档沉淀 | `agent-governance` |
| 项目接入 | `project-init` |
| 知识库/RAG运维 | `knowledge-rag-bootstrap`、`knowledge-project-import`、`knowledge-index-maintenance`、`knowledge-auto-repair`、`knowledge-rag-evaluation`，按场景选 |

任务收口后，由 `@@TL` 按 `knowledge-growth-loop` 判断要不要沉淀；要写候选知识时再走 `knowledge-candidate-intake`。

## 协作链路怎么裁剪

默认顺序是 `@@X → @@TL → @@BA → @@BE → @@DEVOPS → @@TL`，但不是每个任务都要走全链路：

| 档位 | 适用场景 | 链路 |
| --- | --- | --- |
| 0 分快线 | 改一行文案、普通说明文档、注释错别字，不改行为、接口、配置和数据 | 直接改 |
| 1-2 分简化链路 | 小改动；只读分析 | `@@X → @@BE → 最小验证 → @@X`；只读分析走 `@@X → @@BA → @@X` |
| 3-4 分标准链路 | 一般需求 | `@@X → @@TL → @@BA → @@BE → @@DEVOPS → @@TL` |
| 5 分及以上或直接高风险 | 复杂或高风险需求 | `@@X → @@TL → @@BA → @@BE → reviewer → @@DEVOPS → @@TL` |

涉及资金、订单状态、权限、登录、用户数据、生产配置、密钥、删除数据、数据库结构、对外接口契约时，不打分，直接按高风险走。

不管走哪条链路，证据不全不能算通过，没有验证结果不能说完成。权限分 L0 到 L4 五级，见 `.cursor/rules/permission-levels.mdc`。

## 项目接入与映射

打开业务仓时，按仓库根目录的解决方案文件名找项目档案，不看目录名和盘符：

| 解决方案文件 | 项目档案 |
| --- | --- |
| `HX.Matching.sln` | `projects/examples/hx-matching` |
| `BetWin5.0.sln` | `projects/examples/betwin` |

- 当前窗口就是 SimpX 根目录或空窗口时，不建档案。
- 不认识的业务仓只遵守 SimpX 核心规则，不自动建档案；要接入时走 `project-init`。
- 新增或迁移项目时，要同步项目档案、`knowledge/indexes/project-index.md` 和 `knowledge/indexes/rag-manifest.yaml`。
- 项目风格（企业稳定型、遗留系统改造型、性能优先型等）的对照表见 `projects/profiles/README.md`。

业务仓的禁止项：

- 不写 `AGENTS.md`、`.cursor/agents`、`.cursor/rules`、`.cursor/skills`。
- 不为了托管或说明去改业务仓自带的文件。
- Agent 不 git push、不开 PR，提交前由人审。
- 业务代码摘要写到 SimpX 的 `knowledge/code-summaries/<project-id>/`，不写回业务仓。

## 知识沉淀

- 用户说“记住：”或“记一条 ADR”时，直接写入 SimpX 正式知识。
- 其余经验由 `@@TL` 按 `workflows/knowledge-growth-loop` 判断，写入 `raw`、`candidates` 或正式知识，用户周审修正。
- 知识只写 SimpX，不写业务仓，也不写外部知识库。
- 目录说明和查询规则见 `knowledge/README.md`。

## RAG 索引

索引脚本是 `tools/rag_index.py`，有三个命令：

```text
python tools/rag_index.py bootstrap
python tools/rag_index.py project <project_id> <业务仓目录>
python tools/rag_index.py search <关键词> [--project <project_id>] [--repo <业务仓目录>] [--role <角色>] [--task <任务说明>]
```

- `bootstrap`：重建 SimpX 文档层，包括规则、角色、工作流、模板、人工索引和正式知识。
- `project`：扫描一个业务仓，刷新该项目的代码索引和代码摘要。
- `search`：按关键词检索文档层和项目层，同时把这次检索记一笔到当月的检索轨迹里，交接时把 trace_id 当证据。

索引文件可以随时重建，不进 git：

| 文件 | 内容 |
| --- | --- |
| `rag/index/core.db` | 规则、角色、工作流、知识、项目档案这些文档层内容 |
| `rag/index/projects/<id>.db` | 单个业务项目的代码层 |
| `rag/traces/<年-月>.db` | 检索轨迹和修复记录，保留 30 天 |

换电脑或索引坏了，删掉 `rag/index/`，跑一次 `bootstrap`，再对每个接入的项目跑一次 `project` 就能恢复。

RAG 检索结果只能当参考和定位线索，不能覆盖用户当前消息、生效规则、当前源码、项目档案和工作流；涉及行为判断时要回读当前源码。能不能做语义向量召回，以 `knowledge/indexes/rag-manifest.yaml` 里的 `vector_status` 为准。

## 建设原则

- 先可用，再完整。
- 先沉淀方法，再建设工具。
- 先稳定角色，再扩展能力。
- 先服务真实项目，再追求平台化。

## 成熟度

- 已启用：当前一阶段需要认真维护。
- 草稿：一阶段会逐步完善。
- 规划：未来阶段建设。
- 实验：用于探索，不进入稳定流程。

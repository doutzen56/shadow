# 协作规范

SimpX 的协作目标是让个人智能技术团队长期保持一致、可复用、可沉淀。

## 基本要求

- 新增内容必须说明适用场景。
- 修改核心原则必须说明原因和影响范围。
- 工作流变更必须能服务真实项目。
- 经验沉淀必须来自实际问题或明确判断。
- 不为尚未使用的能力过度设计。

## 文档要求

所有正文内容使用中文。文件名和目录名可以保留通用英文，以便兼容工具链。

## 一致性检查

改动下面这些东西时，要按清单同步，并跑对应命令留证据：

| 改了什么 | 要同步的地方 | 要跑的检查 |
| --- | --- | --- |
| 增删或改名角色 | `config/simpx.yaml`、`README.md`、`.cursor/rules/simpx-core.mdc`、`agents/<role>/`（`agent.yaml`、`system.md`、`workflows/README.md`）、`config/rag-x-routing.yaml`、`config/rag-role-matrix.yaml`、`tools/rag_index.py` 的角色常量 | 按 `workflows/agent-governance/role-consistency-check.md` 出一致性表；跑 `python tools/rag_index.py bootstrap` |
| 新增硬规则 | `config/active-rules.yaml` 的 `rule_layers` | 回读确认登记 |
| 新增或改名工作流 | `config/rag-x-routing.yaml` 的 `task_routing` | 确认每个工作流都能从路由走到 |
| 改契约 Schema、模板或状态策略 | `contracts/`、`orchestration/policies/` | `python orchestration/tools/contract_validate.py contracts/templates orchestration/examples` 和 `python -m unittest discover -s orchestration/tests -v` |
| 改 `tools/rag_index.py` 的索引来源 | `knowledge/indexes/rag-manifest.yaml` 的 `sources` / `excluded` | `python -m unittest discover -s tools/tests -v`；跑 `bootstrap` 后回填 manifest |

## 删除文件

- 没有引用不等于没用：模板、示例、历史 ADR、知识卡片本来就可能不被流程直接调用。
- 要删文件，先查静态引用、通配导入（如 `rag_index.py` 的 `DOC_SOURCES`）、运行时路径约定和长期知识价值；四项都确认没用才提删除。
- 删除后跑 `bootstrap`，确认导入数量变化对得上。



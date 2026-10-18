# 项目初始化

## 通用前置检查

执行本流程前，必须先完成 `workflows/_shared/` 中的任务入口、权限、证据、验证、角色交接和完成检查。

## 目标

让新项目在进入执行前完成档案映射、环境就绪和边界确认。

## 输入

- 工作区名称/仓库名称
- 对应项目档案路径
- 目标环境（项目实际有的环境，从 `dev/test/uat` 中选，写进 `envs.yaml` 的 `default_envs`）

## 标准步骤

1. `@@X` 触发 `simpx-project-bootstrap` 进行基础检查。
2. 校验项目档案：`README.md`、`profile.yaml`、`boundaries.md`；按 `profile.yaml` 的风格名，从 `projects/profiles/README.md` 找到并读取对应风格说明，风格名不在表里时先补表。
3. 校验环境来源：项目内置配置优先；只有用户或项目档案明确要求时才检查本地 secrets 配置。
4. 确认 RAG 语料范围：项目档案、架构图、边界文档、允许生成摘要的业务代码根目录、禁止索引目录。
5. 登记项目映射和索引入口：`.cursor/rules/workspace-hosting.mdc` 的映射表、`knowledge/indexes/project-index.md`、`knowledge/indexes/rag-manifest.yaml` 的 `seed_projects`、`config/rag-init.yaml` 的 `seed_projects`，以及 `tools/rag_index.py` 的 `PROJECTS`（摘要分组）。改 `workspace-hosting.mdc` 属于规则修改，要用户确认。
6. 进入 `workflows/knowledge-project-import/README.md` 运行导入，生成 project/source/chunk/symbol、代码摘要和 ingestion_run；`knowledge/code-summaries/<project-id>/manifest.yaml` 由脚本生成，不手写。
7. 空库首次接入或重建 baseline 时，转入 `workflows/knowledge-rag-bootstrap/README.md`。
8. 已有 baseline 的项目变更，转入 `workflows/knowledge-index-maintenance/README.md`。
9. 导入或维护后，按需转入 `workflows/knowledge-rag-evaluation/README.md` 验证召回质量。
10. `@@TL` 确认初始化结果与后续任务入口。

## 输出

- 初始化检查结果
- 缺口清单
- RAG 语料范围和禁止索引范围
- 项目 summary manifest 路径
- 是否需要执行索引维护
- 是否可进入功能开发

## 收口检查

- 项目映射不清或关键档案缺失，不进入开发。
- 环境配置不可用，不进入验证阶段。
- 未明确可索引范围和禁止索引范围，不进入代码摘要导入。
- 初始化争议超过 2 轮未收敛，交用户决策。

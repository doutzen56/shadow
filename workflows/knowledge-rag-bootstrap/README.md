# 知识库 RAG 初始化

## 目标

建立或重建 RAG baseline，让规则、角色、工作流、项目档案、正式知识和代码摘要都有可追溯入口。

## 输入

- `config/rag-init.yaml`
- `config/rag-data-model.yaml`
- `config/rag-importer.yaml`
- `config/rag-retrieval.yaml`
- `config/rag-context-engine.yaml`
- `knowledge/indexes/rag-manifest.yaml`

## 执行工具

`tools/rag_index.py`（用法见文件开头）。步骤 3–8 由它完成，Agent 负责跑命令、读输出、回填 manifest。

## 标准步骤

1. `@@X` 确认初始化范围、权限和禁止索引范围。
2. `@@TL` 确认语料边界、候选知识是否参与索引、是否需要用户拍板。
3. 全量重建时删掉 `rag/index.db`；只刷新文档层时保留。
4. 运行 `python tools/rag_index.py bootstrap`：建表、导入 SimpX 文档、生成 `knowledge_item`、登记种子项目、写 Qdrant 集合占位。
5. 对每个种子项目运行 `python tools/rag_index.py project <project_id> <业务仓根目录>`，见 `knowledge-project-import`。
6. 把命令输出里的 run_id、manifest_hash、数量回填到 `rag-manifest.yaml` 的 `baseline` 段：只有本流程能改这一段；没有真实 embedding 时 `vector_status.semantic_recall_available` 必须写 false。
7. 用 `python tools/rag_index.py search <关键词>` 抽查一次，确认能查到新内容。
8. 按 `artifact_policy`，把 `rag/`、`knowledge/code-summaries/` 和 `rag-manifest.yaml` 放进同一次提交，由用户审后提交。

## 输出

- baseline version。
- manifest hash。
- SQLite schema version。
- Qdrant collection version。
- 首批项目导入结果。
- 未完成项和回滚方式。

## 收口检查

- 未明确禁止索引范围，不得初始化。
- baseline 不能回放导入过程时，不得宣称完成。
- 没有本会话里 `tools/rag_index.py` 的实际输出（run_id、counts_before/after、manifest_hash），不得写“已初始化”“已重建”。
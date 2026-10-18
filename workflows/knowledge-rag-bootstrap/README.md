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

## 标准步骤

1. `@@X` 确认初始化范围、权限和禁止索引范围。
2. `@@TL` 确认语料边界、候选知识是否参与索引、是否需要用户拍板。
3. 创建或校验 SQLite schema。
4. 创建或校验 Qdrant collections。
5. 生成 baseline manifest。
6. 导入 SimpX 文档。
7. 导入首批项目档案和代码摘要输入。
8. 记录 baseline 版本和初始化报告。

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
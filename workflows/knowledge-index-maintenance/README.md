# 知识索引维护

## 目标

在正式知识、项目档案、代码摘要或规则变更后，增量维护 manifest、SQLite、Qdrant 和人工索引。

## 触发条件

- 正式知识变更。
- 候选知识 promote、merge、reject。
- 项目档案变更。
- 代码摘要生成或过期。
- `config/rag-*.yaml` 变更。

## 标准步骤

1. 读取变更路径和影响 project。
2. 更新 `knowledge/indexes/rag-manifest.yaml`。
3. 更新 SQLite source/chunk/knowledge_item/symbol。
4. 更新 FTS。
5. 增量更新 Qdrant points。
6. 标记 stale 或 deprecated。
7. 输出维护报告。

## 输出

- 变更集。
- 受影响 project 和角色视图。
- manifest 更新结果。
- SQLite/Qdrant 同步结果。
- manifest hash。
- stale、deprecated、orphan 处理清单。
- retrieval 影响说明。
- 失败项与修复建议。

## 收口检查

- manifest 与本地文件不一致时不得宣称维护完成。
- 缺少 manifest hash、同步结果或失败项清单时，不得写“索引已维护”。
- 删除或降级正式知识必须有用户或 `@@TL` 确认。
# 知识索引维护

## 目标

在正式知识、项目档案、代码摘要或规则变更后，增量维护 manifest、SQLite、Qdrant 和人工索引。

## 触发条件

- 正式知识变更。
- 候选知识 promote、merge、reject。
- 项目档案变更。
- 代码摘要生成或过期。
- `config/rag-*.yaml` 变更。

## 执行工具

- SimpX 文档、正式知识、项目档案或配置变了：`python tools/rag_index.py bootstrap`（只刷新文档层和 `knowledge_item`，不动业务代码层）。
- 业务代码变了：`python tools/rag_index.py project <project_id> <业务仓根目录>`。
- 人工索引（`project-index.md`、`pattern-index.md`、`tag-index.md`）脚本不改，按下面步骤手工维护。

## 标准步骤

1. 读取变更路径和影响 project。
2. 新增或修改正式知识时，同步 `knowledge/indexes/pattern-index.md`、`tag-index.md` 和 `project-index.md`。
3. 新来源必须落在 `rag-manifest.yaml` 的 `sources` 已登记路径内，否则先补登记，并同步脚本里的 `DOC_SOURCES`。
4. 跑对应的脚本命令：`bootstrap` 整层重写 `rag/index/core.db`；`project` 整库重建对应的 `rag/index/projects/<id>.db`；删掉的文件下次运行后就不在库里。
5. Qdrant：`vector_status.semantic_recall_available` 为 false 时只刷新占位文件，报告里写“向量同步跳过（占位模式）”。
6. 知识降级或作废，改条目 frontmatter 的 `lifecycle_status`（deprecated / rejected），重跑 `bootstrap` 后检索会按 `rag-retrieval.yaml` 的 reject_when 排除。
7. 把命令输出回填到 `rag-manifest.yaml`：`bootstrap` 改 `baseline`，`project` 只改 `latest_run` 和 `seed_projects.<id>`；输出维护报告。

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
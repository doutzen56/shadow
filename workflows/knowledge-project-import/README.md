# 项目 RAG 导入

## 目标

把一个业务项目或 SimpX 文档集注册到 RAG 控制面，生成 project、source、chunk、symbol 和 ingestion_run 记录。

## 输入

- 业务仓根目录。
- 解决方案文件名或显式项目名。
- `workspace-hosting.mdc` 映射结果。
- 项目档案：`README.md`、`profile.yaml`、`boundaries.md`、可选 `architecture-map.md`。
- `config/rag-importer.yaml`。

## 标准步骤

1. `@@X` 确认任务类型为项目接入或知识库/RAG运维。
2. 按 `.sln` 和 `workspace-hosting.mdc` 识别 project；不确定时询问用户。
3. 读取项目档案并校验必需文件。
4. 按 `rag-importer.yaml` 扫描允许的文档和代码根目录。
5. 跳过禁止文件和敏感内容，记录 skip reason。
6. 为文档生成 source/chunk，为代码生成摘要输入，不复制完整源码。
7. 写入 ingestion_run 结果和缺口清单。
8. 更新 `knowledge/indexes/rag-manifest.yaml` 的 `latest_run` 和 `seed_projects.<project-id>`；不改 `baseline`。

## 输出

- project 注册结果。
- source/chunk 生成结果。
- 代码摘要待生成清单。
- 跳过文件清单。
- ingestion_run id。
- manifest hash。
- 导入前后 source/chunk/symbol 数量。
- 是否需要进入 `knowledge-index-maintenance`。

## 收口检查

- 未识别 project 时不得继续导入。
- 未明确禁止索引范围时不得扫描代码。
- 发现密钥或生产配置时必须跳过并报告。
- 缺少 ingestion_run、manifest hash 或导入前后数量时，不得宣称导入完成。
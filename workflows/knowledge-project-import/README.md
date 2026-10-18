# 项目 RAG 导入

## 目标

把一个业务项目或 SimpX 文档集注册到 RAG 控制面，生成 project、source、chunk、symbol 和 ingestion_run 记录。

## 输入

- 业务仓根目录。
- 解决方案文件名或显式项目名。
- `workspace-hosting.mdc` 映射结果。
- 项目档案：`README.md`、`profile.yaml`、`boundaries.md`、可选 `architecture-map.md`。
- `config/rag-importer.yaml`。

## 执行工具

`python tools/rag_index.py project <project_id> <业务仓根目录>`。新项目要先在脚本的 `PROJECTS` 里登记摘要分组（扫描根目录、层次、默认风险标签、职责说明），再按 `project-init` 接入。

## 标准步骤

1. `@@X` 确认任务类型为项目接入或知识库/RAG运维。
2. 按 `.sln` 和 `workspace-hosting.mdc` 识别 project；不确定时询问用户。
3. 读取项目档案并校验必需文件。
4. 运行导入命令：脚本按 `rag-importer.yaml` 只扫 `.cs` 和 `.csproj`，跳过构建产物目录、密钥、证书和配置文件，跳过原因写进初始化报告。
5. 脚本为代码生成 source/chunk/symbol 和代码摘要 Markdown，不复制源码；项目档案文档由 `bootstrap` 负责。
6. 脚本写入 ingestion_run 和 `rag/init-reports/<run_id>.json`。
7. 把命令输出回填到 `knowledge/indexes/rag-manifest.yaml` 的 `latest_run` 和 `seed_projects.<project-id>`；不改 `baseline`。

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
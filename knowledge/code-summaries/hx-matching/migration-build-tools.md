# 迁移与构建工具

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `hx-matching`
- summary_group: `migration-build-tools`
- host_or_layer: `tooling`
- solution_file: `HX.Matching.sln`
- last_ingestion_run: `20261018T090035Z-project-hx-matching`
- file_count: 9
- project_count: 2
- risk_tags: `elasticsearch`, `production-config`

## 职责摘要

ES 迁移、构建辅助和工程工具入口。

## 扫描根目录

- `src/EsMigrate.Console`
- `HX.BuildService`

## 项目文件

- `src/EsMigrate.Console/EsMigrate.Console.csproj`
- `HX.BuildService/HX.BuildService.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `HX.BuildService/HX.BuildService.csproj`：符号 `未提取到符号`；风险 `elasticsearch, production-config`
- `HX.BuildService/Models/GitSyncOptions.cs`：符号 `GitSyncOptions, GitProjectConfig`；风险 `elasticsearch, production-config`
- `HX.BuildService/Program.cs`：符号 `Program, Main`；风险 `elasticsearch, production-config`
- `HX.BuildService/Services/GitSyncWorker.cs`：符号 `GitSyncWorker, ExecuteAsync, ProcessProjectAsync, EnsureRepoExistsAsync, GitPullAsync, BuildVueAsync, GetLatestCommitHash, BuildDotnetAsync`；风险 `elasticsearch, production-config`
- `HX.BuildService/Utils/NodeTools.cs`：符号 `NodeTools, FindPnpm, FindNpm, FindNode, TryReadNodeInstallPath`；风险 `elasticsearch, production-config`
- `HX.BuildService/Utils/ProcessRunner.cs`：符号 `ProcessRunner, Result, RunAsync`；风险 `elasticsearch, production-config`
- `HX.BuildService/Worker.cs`：符号 `Worker, ExecuteAsync`；风险 `elasticsearch, production-config`
- `src/EsMigrate.Console/EsMigrate.Console.csproj`：符号 `未提取到符号`；风险 `elasticsearch, production-config`
- `src/EsMigrate.Console/Program.cs`：符号 `Program, Main`；风险 `elasticsearch, production-config`

## 跳过项

- pruned_directory 4 项、sensitive_file 2 项；明细只写入 `rag/init-reports/20261018T090035Z-project-hx-matching.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

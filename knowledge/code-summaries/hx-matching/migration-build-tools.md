# 迁移与构建工具

project_id: hx-matching
summary_group: migration-build-tools
host_or_layer: tooling
source_repo: F:/HX.MatchingApi
last_ingestion_run: 20261018T070046Z-project-hx-matching
file_count: 7
project_count: 2
risk_tags: [elasticsearch, production-config]

## 职责摘要

ES 迁移、构建辅助和工程工具入口。

## 扫描根目录

- src/EsMigrate.Console
- HX.BuildService

## 项目文件

- HX.BuildService/HX.BuildService.csproj
- src/EsMigrate.Console/EsMigrate.Console.csproj

## 定位样例

- HX.BuildService/Models/GitSyncOptions.cs
- HX.BuildService/Program.cs
- HX.BuildService/Services/GitSyncWorker.cs
- HX.BuildService/Utils/NodeTools.cs
- HX.BuildService/Utils/ProcessRunner.cs
- HX.BuildService/Worker.cs
- src/EsMigrate.Console/Program.cs

## 使用约束

- 本摘要只用于 RAG 定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 摘要生成时跳过 bin、obj、.git、logs、证书、密钥和 secrets 文件。

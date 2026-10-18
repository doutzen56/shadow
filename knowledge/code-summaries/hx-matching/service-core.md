# 业务核心服务

project_id: hx-matching
summary_group: service-core
host_or_layer: domain-service
source_repo: HX.Matching.sln
last_ingestion_run: 20261018T070046Z-project-hx-matching
file_count: 459
project_count: 2
risk_tags: [funds, order-state, idempotency, cache-consistency, elasticsearch]

## 职责摘要

撮合、渠道、统计、缓存、搜索和业务规则主要承载层。

## 扫描根目录

- HX.Matching.ServiceCore
- HX.Matching.Service

## 项目文件

- HX.Matching.Service/HX.Matching.Service.csproj
- HX.Matching.ServiceCore/HX.Matching.ServiceCore.csproj

## 定位样例

- HX.Matching.Service/Es/EsIndexMigrator.cs
- HX.Matching.Service/Es/EsMigrateCheckpoint.cs
- HX.Matching.Service/Es/EsMigrateResult.cs
- HX.Matching.Service/Es/EsMigrateTarget.cs
- HX.Matching.Service/GlobalUsing.cs
- HX.Matching.ServiceCore/AmazonS3/AmazonS3Helper.cs
- HX.Matching.ServiceCore/AmazonS3/IAmazonS3Helper.cs
- HX.Matching.ServiceCore/BaseService.cs
- HX.Matching.ServiceCore/Bloom/BloomOrderWriteHelper.cs
- HX.Matching.ServiceCore/Bloom/BloomWarmupService.cs
- HX.Matching.ServiceCore/Cap/CapDisabledEventBus.cs
- HX.Matching.ServiceCore/Cap/CapFailedManualQueueWriter.cs

## 使用约束

- 本摘要只用于 RAG 定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 摘要生成时跳过 bin、obj、.git、logs、证书、密钥和 secrets 文件。

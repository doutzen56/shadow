# 基础设施与公共能力

project_id: hx-matching
summary_group: infrastructure-common
host_or_layer: infrastructure
source_repo: F:/HX.MatchingApi
last_ingestion_run: 20261018T070046Z-project-hx-matching
file_count: 201
project_count: 2
risk_tags: [cache-consistency, mq, permission, login]

## 职责摘要

缓存、消息、认证、限流、异常处理、帮助类和横切能力。

## 扫描根目录

- HX.Matching.Infrastructure
- HX.Matching.Common

## 项目文件

- HX.Matching.Common/HX.Matching.Common.csproj
- HX.Matching.Infrastructure/HX.Matching.Infrastructure.csproj

## 定位样例

- HX.Matching.Common/AmazonS3Helper.cs
- HX.Matching.Common/DynamicApiSimple/ApiConvention.cs
- HX.Matching.Common/DynamicApiSimple/ApiFeatureProvider.cs
- HX.Matching.Common/DynamicApiSimple/DynamicApiAttribute.cs
- HX.Matching.Common/DynamicApiSimple/Extens/DynamicApiExtens.cs
- HX.Matching.Common/DynamicApiSimple/IDynamicApi.cs
- HX.Matching.Common/DynamicApiSimple/JsonModelBinder.cs
- HX.Matching.Common/ExcelHelper.cs
- HX.Matching.Common/MailHelper.cs
- HX.Matching.Common/Tools.cs
- HX.Matching.Infrastructure/App/App.cs
- HX.Matching.Infrastructure/AppSettings.cs

## 使用约束

- 本摘要只用于 RAG 定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 摘要生成时跳过 bin、obj、.git、logs、证书、密钥和 secrets 文件。

# 模型与仓储

project_id: hx-matching
summary_group: data-model-repository
host_or_layer: data-access
source_repo: F:/HX.MatchingApi
last_ingestion_run: 20261018T070046Z-project-hx-matching
file_count: 372
project_count: 2
risk_tags: [database-schema, order-state, user-data]

## 职责摘要

实体、枚举、请求响应模型和 SqlSugar 仓储基础能力。

## 扫描根目录

- HX.Matching.Model
- HX.Matching.Repository

## 项目文件

- HX.Matching.Model/HX.Matching.Model.csproj
- HX.Matching.Repository/HX.Matching.Repository.csproj

## 定位样例

- HX.Matching.Model/ApiResponseDto.cs
- HX.Matching.Model/Enum/AuditStatusEnum.cs
- HX.Matching.Model/Enum/CommentSwitchEnum.cs
- HX.Matching.Model/Enum/UserMsgType.cs
- HX.Matching.Model/GlobalUsing.cs
- HX.Matching.Model/MultiTenant/EntityScopeMarkers.cs
- HX.Matching.Model/PagedInfo.cs
- HX.Matching.Model/PagerInfo.cs
- HX.Matching.Model/ProteryConstant.cs
- HX.Matching.Model/System/Const/BankCodeMapping.cs
- HX.Matching.Model/System/Const/CashierAccessType.cs
- HX.Matching.Model/System/Const/MerchantOpenApiConst.cs

## 使用约束

- 本摘要只用于 RAG 定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 摘要生成时跳过 bin、obj、.git、logs、证书、密钥和 secrets 文件。

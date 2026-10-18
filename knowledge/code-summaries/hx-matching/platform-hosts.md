# 平台与商户后台宿主

project_id: hx-matching
summary_group: platform-hosts
host_or_layer: web-host
source_repo: HX.Matching.sln
last_ingestion_run: 20261018T070046Z-project-hx-matching
file_count: 330
project_count: 3
risk_tags: [permission, login, user-data]

## 职责摘要

管理端和商户后台入口，承载运营、权限、报表和后台管理动作。

## 扫描根目录

- HX.Matching.Admin.WebApi
- HX.Matching.AdminV2.WebApi
- HX.Matching.MerchantAdmin.WebApi

## 项目文件

- HX.Matching.Admin.WebApi/HX.Matching.Admin.WebApi.csproj
- HX.Matching.AdminV2.WebApi/HX.Matching.AdminV2.WebApi.csproj
- HX.Matching.MerchantAdmin.WebApi/HX.Matching.MerchantAdmin.WebApi.csproj

## 定位样例

- HX.Matching.Admin.WebApi/Controllers/CommonController.cs
- HX.Matching.Admin.WebApi/Controllers/System/CallBack/ChannelCallback.cs
- HX.Matching.Admin.WebApi/Controllers/System/ChannelOrderController.cs
- HX.Matching.Admin.WebApi/Controllers/System/CommonLangController.cs
- HX.Matching.Admin.WebApi/Controllers/System/HxAutoReviewEsController.cs
- HX.Matching.Admin.WebApi/Controllers/System/HxBlacklistAccountController.cs
- HX.Matching.Admin.WebApi/Controllers/System/HxBlacklistPayController.cs
- HX.Matching.Admin.WebApi/Controllers/System/HxChannelController.cs
- HX.Matching.Admin.WebApi/Controllers/System/HxChannelMoneyController.cs
- HX.Matching.Admin.WebApi/Controllers/System/HxCustomerServiceController.cs
- HX.Matching.Admin.WebApi/Controllers/System/HxDashboardController.cs
- HX.Matching.Admin.WebApi/Controllers/System/HxFalseOrderRecordController.cs

## 使用约束

- 本摘要只用于 RAG 定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 摘要生成时跳过 bin、obj、.git、logs、证书、密钥和 secrets 文件。

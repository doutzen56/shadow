# 商户与合作方开放接口

project_id: hx-matching
summary_group: open-apis
host_or_layer: public-api
source_repo: HX.Matching.sln
last_ingestion_run: 20261018T070046Z-project-hx-matching
file_count: 11
project_count: 2
risk_tags: [public-contract, funds, order-state, gateway]

## 职责摘要

商户和合作方开放接口热路径，涉及验签、订单创建、查单和外部契约。

## 扫描根目录

- HX.Matching.MerchantApi
- HX.Matching.PartnerApi

## 项目文件

- HX.Matching.MerchantApi/HX.Matching.MerchantApi.csproj
- HX.Matching.PartnerApi/HX.Matching.PartnerApi.csproj

## 定位样例

- HX.Matching.MerchantApi/Cashier/CashierPageRenderer.cs
- HX.Matching.MerchantApi/Cashier/CashierPayQrCodeHelper.cs
- HX.Matching.MerchantApi/MerchantApiRequestDiagnosticsMiddleware.cs
- HX.Matching.MerchantApi/MerchantApiTimeoutPolicies.cs
- HX.Matching.MerchantApi/MerchantOpenApiResponse.cs
- HX.Matching.MerchantApi/MinimalApiEndpoints.cs
- HX.Matching.MerchantApi/Program.cs
- HX.Matching.PartnerApi/PartnerApiEndpoints.cs
- HX.Matching.PartnerApi/PartnerApiTimeoutPolicies.cs
- HX.Matching.PartnerApi/PartnerRequestValidator.cs
- HX.Matching.PartnerApi/Program.cs

## 使用约束

- 本摘要只用于 RAG 定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 摘要生成时跳过 bin、obj、.git、logs、证书、密钥和 secrets 文件。

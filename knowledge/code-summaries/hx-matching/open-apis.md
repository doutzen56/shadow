# 商户与合作方开放接口

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `hx-matching`
- summary_group: `open-apis`
- host_or_layer: `public-api`
- solution_file: `HX.Matching.sln`
- last_ingestion_run: `20261018T083109Z-project-hx-matching`
- file_count: 13
- project_count: 2
- risk_tags: `funds`, `gateway`, `order-state`, `public-contract`

## 职责摘要

商户和合作方开放接口热路径，涉及验签、订单创建、查单和外部契约。

## 扫描根目录

- `HX.Matching.MerchantApi`
- `HX.Matching.PartnerApi`

## 项目文件

- `HX.Matching.MerchantApi/HX.Matching.MerchantApi.csproj`
- `HX.Matching.PartnerApi/HX.Matching.PartnerApi.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `HX.Matching.MerchantApi/Cashier/CashierPageRenderer.cs`：符号 `CashierPageRenderer, LoadAntiFraudImageBase64, Render, RenderLoading, RenderError, IsTerminalState, ShouldStopStatusPolling, GetStateSubText`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.MerchantApi/Cashier/CashierPayQrCodeHelper.cs`：符号 `CashierPayQrCodeHelper, HasValue, IsH5PayMode, TryGetH5Url, EnrichDisplaySrc, EnrichDisplaySrcAsync, IsImageSource, ResolveImgSrc`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.MerchantApi/HX.Matching.MerchantApi.csproj`：符号 `未提取到符号`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.MerchantApi/MerchantApiRequestDiagnosticsMiddleware.cs`：符号 `MerchantApiRequestDiagnosticsMiddleware, InvokeAsync`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.MerchantApi/MerchantApiTimeoutPolicies.cs`：符号 `MerchantApiTimeoutPolicies`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.MerchantApi/MerchantOpenApiResponse.cs`：符号 `MerchantOpenApiResponse, UnifiedError`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.MerchantApi/MinimalApiEndpoints.cs`：符号 `MinimalApiEndpoints, TestRequest, MapMerchantApiEndpoints, MapMerchantOpenApi, MapCashierOpenApi, GetAllOrderList, HandleMerchantOpenApiMerchantInfoAsync, FormatOpenApiRequestForLog`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.MerchantApi/Program.cs`：符号 `未提取到符号`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.PartnerApi/HX.Matching.PartnerApi.csproj`：符号 `未提取到符号`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.PartnerApi/PartnerApiEndpoints.cs`：符号 `PartnerApiEndpoints, MapPartnerApiEndpoints, MapPartnerOpenApi`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.PartnerApi/PartnerApiTimeoutPolicies.cs`：符号 `PartnerApiTimeoutPolicies`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.PartnerApi/PartnerRequestValidator.cs`：符号 `PartnerRequestValidator, Message`；风险 `funds, gateway, order-state, public-contract`
- `HX.Matching.PartnerApi/Program.cs`：符号 `未提取到符号`；风险 `funds, gateway, order-state, public-contract`

## 跳过项

- pruned_directory 4 项、sensitive_file 4 项；明细只写入 `rag/init-reports/20261018T083109Z-project-hx-matching.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

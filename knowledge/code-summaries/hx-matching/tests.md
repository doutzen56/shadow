# 测试项目

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `hx-matching`
- summary_group: `tests`
- host_or_layer: `test`
- solution_file: `HX.Matching.sln`
- last_ingestion_run: `20261018T093230Z-project-hx-matching`
- file_count: 143
- project_count: 3
- risk_tags: `cache-consistency`, `callback`, `funds`, `gateway`, `login`, `order-state`, `permission`, `user-data`

## 职责摘要

渠道、V2 和集成测试入口，用于后续验证定位。

## 扫描根目录

- `HX.Matching.Channels.Tests`
- `HX.Matching.V2.Tests`
- `src/TestIntegration`

## 项目文件

- `HX.Matching.Channels.Tests/HX.Matching.Channels.Tests.csproj`
- `HX.Matching.V2.Tests/HX.Matching.V2.Tests.csproj`
- `src/TestIntegration/TestIntegration.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `HX.Matching.Channels.Tests/BaWangPayInRechargeTests.cs`：符号 `BaWangPayInRechargeTests, Config, Build, Submit`；风险 `funds, gateway`
- `HX.Matching.Channels.Tests/ChannelCancellationTests.cs`：符号 `ChannelCancellationTests, SiHaiHappyPath, NeoPayHappyPath, AssertCanceledFastAsync, BalancedArgs, Where`；风险 `gateway`
- `HX.Matching.Channels.Tests/ChannelClientTests.cs`：符号 `ChannelClientTests, FakeFactory, RecordingRecharge, RecordingWithdraw, Ctx, BuildContainer, GetRechargeCore, GetWithdrawCore`；风险 `gateway`
- `HX.Matching.Channels.Tests/ChannelContractMapperTests.cs`：符号 `ChannelContractMapperTests, Probe, AssertMapped`；风险 `gateway`
- `HX.Matching.Channels.Tests/ChannelContractParityTests.cs`：符号 `ChannelContractParityTests, Show`；风险 `gateway`
- `HX.Matching.Channels.Tests/ChannelDiWiringTests.cs`：符号 `ChannelDiWiringTests, BuildContainer, Ancestors`；风险 `gateway`
- `HX.Matching.Channels.Tests/ChannelEnumParityTests.cs`：符号 `ChannelEnumParityTests`；风险 `gateway`
- `HX.Matching.Channels.Tests/ChannelInvocationContextTests.cs`：符号 `ChannelInvocationContextTests`；风险 `gateway`
- `HX.Matching.Channels.Tests/ChannelStatusPipelineTests.cs`：符号 `ChannelStatusPipelineTests, Config, Build, Order, PayinConfig, BuildPayin`；风险 `gateway, order-state`
- `HX.Matching.Channels.Tests/NeoPayPayInRechargeTests.cs`：符号 `NeoPayPayInRechargeTests, Config, Build, Submit`；风险 `funds, gateway`
- `HX.Matching.Channels.Tests/PayoutAccountDecryptionTests.cs`：符号 `PayoutAccountDecryptionTests, Decrypt, Entity`；风险 `funds, gateway`
- `HX.Matching.Channels.Tests/RechargeChannelsBaseCoreTests.cs`：符号 `RechargeChannelsBaseCoreTests, TestRechargeCore, RecordingLoggerProvider, Recorder, ThrowingLoggerProvider, Thrower, RunOrder, RunQuery`；风险 `funds, gateway`
- `HX.Matching.Channels.Tests/SaoNiuWithdrawTests.cs`：符号 `SaoNiuWithdrawTests, Config, Build, Order, ExpectedSign`；风险 `funds, gateway`
- `HX.Matching.Channels.Tests/SiHaiPayInRechargeTests.cs`：符号 `SiHaiPayInRechargeTests, Config, Build, Submit, OrderOkWithPayUrl`；风险 `funds, gateway`
- `HX.Matching.Channels.Tests/XingShengWithdrawTests.cs`：符号 `XingShengWithdrawTests, CapturingLoggerProvider, Rec, Config, Build, Order, CreateLogger, Dispose`；风险 `funds, gateway`
- `HX.Matching.V2.Tests/Login/ClearCacheLiveSetup.cs`：符号 `ClearCacheLiveSetup, Clear_SysConfig_And_Mark_User_Cache, EnumerateSysConfigKeys`；风险 `cache-consistency, login`
- `HX.Matching.V2.Tests/Login/EsLogScopeTests.cs`：符号 `EsLogScopeTests, SignalRUserKeyTests, Init, Cleanup, NoContext_LeavesFieldsEmpty, Platform_WritesPlatformCode, Impersonate_WritesTargetTenant, Tenant_WritesTenantCode`；风险 `login`
- `HX.Matching.V2.Tests/Login/ImpersonationCatalogTests.cs`：符号 `ImpersonationCatalogTests, PlatformRoute_IgnoresHeader, GetReadPrefix_Allows, WriteExact_AllowsAndAudits, AddMerchantUser_Denied, UnknownPost_Denied, Login_DeniedForImpersonation, ImpersonationPerm_MatchesExactAndWildcard`；风险 `login`
- `HX.Matching.V2.Tests/Login/LoginLiveTests.cs`：符号 `LoginLiveTests, ClassInit, Init, Host_ShouldHonorTestMultiTenantSwitch, Host_ShouldUseV2SignalROnlineHash, V1AudienceToken_IsRejectedByV2, V2TokenWithoutAdminType_IsRejected, Healthz_ShouldReturnOk`；风险 `login`
- `HX.Matching.V2.Tests/Login/LoginV2Tests.cs`：符号 `LoginV2Tests, Init, Cleanup, V2Jwt_AudienceIsAdminV2, SysUser_PlatformHost_UsesPublicConfigId, SysUser_TenantHost_UsesTenantConfigId`；风险 `login`
- `HX.Matching.V2.Tests/Login/MarkGoogleBypassLiveSetup.cs`：符号 `MarkGoogleBypassLiveSetup, Apply_Mark_Google_Bypass_888888, UpsertConfigAsync`；风险 `login`
- `HX.Matching.V2.Tests/Login/RouteWhitelistScanTests.cs`：符号 `RouteWhitelistScanTests, ReadPrefixes_IncludeRuleRiskAndUserLog, ImportActions_AreDenied, Catalog_HasNoLegacyTyposOrHiddenPages`；风险 `login`
- `HX.Matching.V2.Tests/Login/V2RouteCatalogLiveTests.cs`：符号 `V2RouteCatalogLiveTests, ClassInit, AllV2Routes_HaveSwitchMapping, DangerousGet_MustBeRegistered, LiveRoutes_HaveNoHiddenPages_AndCatalogKnowsThem`；风险 `login`
- `HX.Matching.V2.Tests/MerchantAdmin/T3bMerchantAdminTenantTests.cs`：符号 `T3bMerchantAdminTenantTests, ThrowingResolver, NullResolver, RecordingResolver, CanceledResolver, Cleanup, DomainPurpose_MerchantAdmin_Is2, V2ClientApp_MerchantAdmin_Constant`；风险 `permission`
- `HX.Matching.V2.Tests/MultiTenant/AuthzP0ContractTests.cs`：符号 `AuthzP0ContractTests, Messenger_Send_Requires_InternalJobKey_Header, SeedData_Requires_Platform_And_AllowSeedFlag, OfflineDownload_Checks_CreateBy_Against_GetName, Locate`；风险 `permission`
- `HX.Matching.V2.Tests/MultiTenant/AuthzP1ContractTests.cs`：符号 `AuthzP1ContractTests, Write_Actions_Must_Declare_Permit_LoginOnly_Or_Anonymous, OrderMatching_Options_Requires_Login, Dict_Batch_Dicts_Requires_Login_Type_May_Stay_Anonymous, MqRequireTenantHeader_Documented_As_Consume_Legacy_Fallback, LocateDir, LocateFile`；风险 `permission`
- `HX.Matching.V2.Tests/MultiTenant/Cap/Async005BCapFundsEsContractTests.cs`：符号 `Async005BCapFundsEsContractTests, FakeCapPublisher, RecordingFundsEsRepo, IndexAsync, BulkIndexAsync, DeleteByQueryAsync, IntegrationEvent_DefaultTopic_Is005BContract, FundsDetail_EsIndex_IsMonthlyManaged_Hot2Close4`；风险 `funds`
- `HX.Matching.V2.Tests/MultiTenant/Cap/OrderEsCapBackfillContractTests.cs`：符号 `OrderEsCapBackfillContractTests, OrderEsBackfill_Topic_Is_Dedicated_Temporary_Name, CapWorker_Registers_OrderEsBackfillCapHandler, OrderEsCapBackfillService_HasNo_AppServiceAttribute, CapPublishHost_DoesNot_Register_OrderEsBackfillCapHandler`；风险 `order-state`
- `HX.Matching.V2.Tests/MultiTenant/Es/OrderEsCapBackfillCountLiveTests.cs`：符号 `OrderEsCapBackfillCountLiveTests, Count_LastMonth_Orders_Matching_Funds`；风险 `order-state`
- `HX.Matching.V2.Tests/MultiTenant/Es/OrderEsCapBackfillLiveTests.cs`：符号 `OrderEsCapBackfillLiveTests, Backfill_LastMonth_Via_Cap_Then_Reconcile, DrainSameAsCapHandlersAsync, WaitCapPublishedSucceededAsync`；风险 `order-state`
- `HX.Matching.V2.Tests/MultiTenant/Es/OrderQueryEsLiveTests.cs`：符号 `OrderQueryEsLiveTests, ClassInit, Cleanup, Recharge_Sync_QueryDoc_Then_ListApi_Finds_Order, FundsDetail_Insert_CapPublish_Handler_Then_ListApi_Finds_Row, Export_Mark_Is_Applied_When_Switch_Es, RewindRechargeWatermarkAsync, GetJsonAsync`；风险 `order-state`
- `HX.Matching.V2.Tests/MultiTenant/T0001V2TenantUserLiveTests.cs`：符号 `T0001V2TenantUserLiveTests, ClassInit, Insert_T0001_V2_Tenant_User_With_All_Tenant_Menus, EnsureV2RoleMenuTableAsync`；风险 `user-data`
- `HX.Matching.V2.Tests/MultiTenant/T11TenantOrderStatsContractTests.cs`：符号 `T11TenantOrderStatsContractTests, PlatformStats_Tenant_List_Route, PlatformStats_Tenant_Export_Route`；风险 `order-state`
- `HX.Matching.V2.Tests/MultiTenant/T11TenantOrderStatsLiveTests.cs`：符号 `T11TenantOrderStatsLiveTests, ClassInit, Api064_TenantOrder_List_Ok, Api065_TenantOrder_Export_Creates_Task`；风险 `order-state`
- `HX.Matching.V2.Tests/MultiTenant/T11TenantWalletContractTests.cs`：符号 `T11TenantWalletContractTests, Wallet_Models_Are_PublicDb, MatchingFee_Gate_Settle_And_Wallet_Sync_Wired, TenantWallet_Routes_And_Perms, TenantList_Dto_Has_WalletBalance_And_TenantCode, Ledger_Query_Dto_Has_TenantName, Commission_Strategies_Fixed_And_Tiered, Tiered_Strategy_Uses_Period_Volume`；风险 `funds`
- `HX.Matching.V2.Tests/MultiTenant/T11TenantWalletLiveTests.cs`：符号 `T11TenantWalletLiveTests, ClassInit, Service_Adjust_Then_Ledger_And_Balance, Service_TieredVolume_Mode_Save_Delete, Stats_Dto_Dropped_Daily_And_CrossMatched, EnsureWalletSchemaOrSkipAsync, ResolveAnyTenantCodeAsync`；风险 `funds`
- `HX.Matching.V2.Tests/MultiTenant/T8OrderEsContractTests.cs`：符号 `T8OrderEsContractTests, Order_Models_Expose_T8_Columns, Order_Es_Aliases_Are_Registered, Order_Es_Physical_Index_Uses_CreateMonth_Not_WriteAlias, Order_Es_Types_Are_Monthly_Managed, Impersonation_Still_Allows_Order_List_Reads, Tenant_Order_List_Query_Dtos_Have_TenantCode, Tenant_Risk_Channel_Tg_List_Query_Dtos_Have_TenantCode`；风险 `order-state`
- `HX.Matching.V2.Tests/MultiTenant/T8OrderEsLiveTests.cs`：符号 `T8OrderEsLiveTests, RestoreWatermark, ClassInit, Api072_OrderRecharge_List_Enriches_TenantCode, Api072A_OrderWithdrawal_List_Enriches_TenantCode, Api070_MerchantUser_List_With_Tenant, Api071_UserLabel_List_With_Tenant, Api075A_NonWhitelist_Order_Write_Is_Forbidden`；风险 `order-state`
- `HX.Matching.V2.Tests/MultiTenant/T8OrderRelationAndWriteLiveTests.cs`：符号 `T8OrderRelationAndWriteLiveTests, ClassInit, Api074_Matching_Relation_With_Tenant, Api075_Withdrawal_Remark_Write_Then_Restore`；风险 `order-state`
- `HX.Matching.V2.Tests/Order/T2b1ParityTests.cs`：符号 `PermissionCoverageTests, V2FieldParityTests, EveryAction_HasPermitOrAnonymousOrLoginOnly, ClassInit, OrderAndChannelAndSupplier_ListReturnsDataEnvelope, FillCaptchaAsync, ResolvePlatformHostAsync`；风险 `order-state`
- `HX.Matching.V2.Tests/Order/T2b2ParityTests.cs`：符号 `T2b2PermissionCoverageTests, T2b2FieldParityTests, EveryAction_HasPermitOrAnonymousOrLoginOnly, LegacyReviewAndCustomerService_HaveTenantSelfReviewGate, ClassInit, MatchingAndCustomerService_ListReturnsDataEnvelope, CustomerService_ImpersonationDenied, FillCaptchaAsync`；风险 `order-state`
- `HX.Matching.V2.Tests/Support/AdminV2LiveHost.cs`：符号 `AdminV2Factory, AdminV2LiveHost, ConfigureWebHost, CreateHost, FindAdminV2Root, EnsureStarted, IsCapWorkerIdBusy, StartOnce`；风险 `permission`
- `src/TestIntegration/DfPayCashierParserTests.cs`：符号 `DfPayCashierParserTests`；风险 `funds`
- `src/TestIntegration/Es/OrderCallbackEsTests.cs`：符号 `OrderCallbackEsTests, IndexThenGetAndCount_SameAsCallbackService`；风险 `callback, order-state`
- `src/TestIntegration/Es/OrderLifeEsTests.cs`：符号 `OrderLifeEsTests, GetOrderLifeList_ByOrderId, GetResultContentList_ByKeyword`；风险 `order-state`
- `src/TestIntegration/Es/StatsMchntMoneyEsTests.cs`：符号 `StatsMchntMoneyEsTests, SearchMoneyRange_SameAsReport`；风险 `funds`
- `src/TestIntegration/Es/StatsOrderLogEsTests.cs`：符号 `StatsOrderLogEsTests, SeedAsync, NewLog, Service, GetGYSToDay_TodayAgg, GetMchntToDay_ByMchntCode, GetNXHToDay_InnerLoop, GetSupplierToDay_BySupplierId`；风险 `order-state`
- `src/TestIntegration/Es/SysLogininforEsTests.cs`：符号 `SysLogininforEsTests, IndexThenGetLoginLog_FiltersByUserName`；风险 `login`
- `src/TestIntegration/Es/TelegramOrderFlowEsTests.cs`：符号 `TelegramOrderFlowEsTests, IndexThenSearchByOrderIdAndMessageId, SearchByStatusList_SameAsBot`；风险 `order-state`
- `src/TestIntegration/FixedMoneyPlanTests.cs`：符号 `FixedMoneyPlanTests, Segment`；风险 `funds`
- 还有 93 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 6 项；明细只写入 `rag/init-reports/20261018T093230Z-project-hx-matching.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

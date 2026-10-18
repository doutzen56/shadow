# 业务核心服务

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `hx-matching`
- summary_group: `service-core`
- host_or_layer: `domain-service`
- solution_file: `HX.Matching.sln`
- last_ingestion_run: `20261018T083109Z-project-hx-matching`
- file_count: 461
- project_count: 2
- risk_tags: `cache-consistency`, `callback`, `elasticsearch`, `funds`, `gateway`, `idempotency`, `login`, `mq`, `order-state`, `permission`, `user-data`

## 职责摘要

撮合、渠道、统计、缓存、搜索和业务规则主要承载层。

## 扫描根目录

- `HX.Matching.ServiceCore`
- `HX.Matching.Service`

## 项目文件

- `HX.Matching.ServiceCore/HX.Matching.ServiceCore.csproj`
- `HX.Matching.Service/HX.Matching.Service.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `HX.Matching.ServiceCore/Bloom/BloomOrderWriteHelper.cs`：符号 `BloomOrderWriteHelper, ScheduleRechargeAdd, ScheduleWithdrawalAdd, ScheduleRechargeChannelAdd, ScheduleWithdrawalChannelAdd, ScheduleWithdrawalMatchingOrderIdAdd, ScheduleAdd`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Cap/FundsDetail/MerchantFundsDetailEsSyncCapHandler.cs`：符号 `MerchantFundsDetailEsSyncCapHandler, HandleAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Cap/FundsDetail/MerchantFundsDetailEsSyncWriter.cs`：符号 `IMerchantFundsDetailEsSyncWriter, MerchantFundsDetailEsSyncWriter, SyncAsync, ResolveTenantCode, TryLoadFromDb, MapFromEvent`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Cap/OrderEsBackfill/IOrderEsCapBackfillService.cs`：符号 `IOrderEsCapBackfillService, OrderEsCapBackfillCounts, OrderEsCapBackfillPublishResult, OrderEsCapBackfillReconcileResult, ToString`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Cap/OrderEsBackfill/OrderEsBackfillCapHandler.cs`：符号 `OrderEsBackfillCapHandler, HandleAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Cap/OrderEsBackfill/OrderEsBackfillIndexer.cs`：符号 `IOrderEsBackfillIndexer, OrderEsBackfillIndexer, IndexAsync, ResolveSplitWindow, IndexRechargeAsync, IndexWithdrawalAsync, IndexMatchingAsync, ResolveTenantCode`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Cap/OrderEsBackfill/OrderEsCapBackfillService.cs`：符号 `OrderEsCapBackfillService, CountCurrentTenantAsync, PublishCurrentTenantAsync, ReconcileCurrentTenantAsync, PublishRechargeAsync, PublishWithdrawalAsync, PublishMatchingAsync, PublishFundsAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/HxRiskUserEsDocumentId.cs`：符号 `HxRiskUserEsDocumentId, Build`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, user-data`
- `HX.Matching.ServiceCore/Es/HxStatsMchntMoneyDocumentId.cs`：符号 `HxStatsMchntMoneyDocumentId, Build`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/OrderLifeDocumentId.cs`：符号 `OrderLifeDocumentId, Build, ShortHash`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Query/FundsDetailQueryEsService.cs`：符号 `FundsDetailQueryEsService, SearchPageAsync, BuildFilters, AddTerm, Map`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Query/IFundsDetailQueryEsService.cs`：符号 `IFundsDetailQueryEsService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Query/IOrderQueryEsService.cs`：符号 `IOrderQueryEsService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Query/OrderQueryDocMapper.cs`：符号 `OrderQueryDocMapper, ToRechargeQuery, ToWithdrawalQuery, ToMatchingQuery, SafeDecrypt, MaskAccount, MaskName`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Query/OrderQueryEsBackend.cs`：符号 `OrderQueryEsBackend, IsEs`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Query/OrderQueryEsService.cs`：符号 `OrderQueryEsService, SearchRechargePageAsync, SearchWithdrawalPageAsync, SearchMatchingPageAsync, BuildRechargeFilters, BuildWithdrawalFilters, BuildMatchingFilters, AddTenant`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Sync/IOrderEsReconcileService.cs`：符号 `IOrderEsReconcileService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Sync/IOrderEsSyncService.cs`：符号 `IOrderEsSyncService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Sync/OrderEsPhysicalIndex.cs`：符号 `OrderEsPhysicalIndex, IsOrderMasterDoc, IsOrderQueryDoc, UsesPhysicalMonthlyIndex, Resolve, GetBusinessTime`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Sync/OrderEsReconcileService.cs`：符号 `OrderEsReconcileService, ReconcileCurrentTenantAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Es/Sync/OrderEsSyncConstants.cs`：符号 `OrderEsSyncConstants`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Security/GoogleAuthLoginFlows.cs`：符号 `GoogleAuthLoginFlows, IsAdminGoogleVerifyEnabled, IsMerchantGoogleVerifyEnabledAsync, KickIfGoogleLockedAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, login, order-state, permission`
- `HX.Matching.ServiceCore/Services/EnterPrisePay/EnterPriseMatchingCallbackHandler.cs`：符号 `IEnterPriseMatchingCallbackHandler, EnterPriseMatchingCallbackHandler, ReleaseMerchantBalanceLockAsync, RechargeManualSuccessProcess, RechargeSuccessProcess, RechargeOrderSuccessCallbackProcess, RechargeOrderCancelCallbackProcess, RechargeOrderFailCallbackProcess`；风险 `cache-consistency, callback, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/HxFalseOrderUserService.cs`：符号 `HxFalseOrderUserService, QueryExp, AddFalseOrderUser, Enable, GetInfo, GetPageListAsync, IsInOrder, CalculateExpireSeconds`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, user-data`
- `HX.Matching.ServiceCore/Services/HxMerchantUserOrdercOverviewService.cs`：符号 `HxMerchantUserOrdercOverviewService, UserOrdercOverviewTypeAdd`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, user-data`
- `HX.Matching.ServiceCore/Services/HxMerchantUserWithdrawalService.cs`：符号 `HxMerchantUserWithdrawalService, QueryExp, GetPageList, DeriveTypeName`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, user-data`
- `HX.Matching.ServiceCore/Services/HxOrderMatchingRechargeSupplierService.cs`：符号 `HxOrderMatchingRechargeSupplierService, RechargeChannelOrderSqlRow, PageChannelOrderList, BuildChannelOrderResponse, GetPageStats, EmptyChannelPage, ShouldSkipChannelListByBloomAsync, GetByOrderIdRecentDaysAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/IService/IHxFalseOrderUserService.cs`：符号 `IHxFalseOrderUserService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, user-data`
- `HX.Matching.ServiceCore/Services/IService/IHxMerchantUserOrdercOverviewService.cs`：符号 `IHxMerchantUserOrdercOverviewService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, user-data`
- `HX.Matching.ServiceCore/Services/IService/IHxMerchantUserWithdrawalService.cs`：符号 `IHxMerchantUserWithdrawalService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, user-data`
- `HX.Matching.ServiceCore/Services/IService/IHxOrderMatchingRechargeSupplierService.cs`：符号 `IHxOrderMatchingRechargeSupplierService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/IService/IOrderRechargeService.cs`：符号 `IOrderRechargeService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/IService/IOrderRechargeSupplementService.cs`：符号 `IOrderRechargeSupplementService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/IService/IOrderWithdrawalService.cs`：符号 `IOrderWithdrawalService`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/IService/ISysUserRoleService.cs`：符号 `ISysUserRoleService, CountUserRoleByRoleId, DeleteUserRoleByUserId, DeleteRoleUserByUserIds, AddUserRole, GetSysUsersByRoleId, GetExcludedSysUsersByRoleId, InsertUserRole`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, permission, user-data`
- `HX.Matching.ServiceCore/Services/OfflineDownload/Handler/ChannelOrderRechargeDownloadHandler.cs`：符号 `ChannelOrderRechargeDownloadHandler, GetPagedDataAsync`；风险 `cache-consistency, elasticsearch, funds, gateway, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OfflineDownload/Handler/ChannelOrderWithdrawalDownloadHandler.cs`：符号 `ChannelOrderWithdrawalDownloadHandler, GetPagedDataAsync`；风险 `cache-consistency, elasticsearch, funds, gateway, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OfflineDownload/Handler/MerchantUserWithdrawalDownloadHandler.cs`：符号 `MerchantUserWithdrawalDownloadHandler, GetPagedDataAsync, BuildWithdrawalInfo`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, user-data`
- `HX.Matching.ServiceCore/Services/OfflineDownload/Handler/OrderRechargeDownloadHandler.cs`：符号 `OrderRechargeDownloadHandler, WriteCsvAsync, WriteEsExcelAsync, GetPagedDataAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OfflineDownload/Handler/OrderWithdrawalPriorityDownloadHandler.cs`：符号 `OrderWithdrawalPriorityDownloadHandler, GetPagedDataAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OfflineDownload/Handler/OrderWithdrawalTgPostDownloadHandler.cs`：符号 `OrderWithdrawalTgPostDownloadHandler, GetPagedDataAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OrderCallbackBackgroundService.cs`：符号 `OrderCallbackBackgroundService, CallbackTask, EnqueueCallbackAsync, EnqueueCallback, ExecuteAsync, ProcessCallbackAsync, ProcessCallbackCoreAsync, CloneForRetry`；风险 `cache-consistency, callback, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OrderMatchmakingServices/CashierRechargePullCoordinator.cs`：符号 `CashierRechargePullCoordinator, DispatchAsync, ProcessAsync, AcquireHttpPermitAsync, ReleaseHttpPermit, ProcessPullAsync, TryUnlockAsync`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OrderMatchmakingServices/WithdrawalAutoPushRule.cs`：符号 `WithdrawalAutoPushRule, HasRemainingRound`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OrderRechargeService.cs`：符号 `OrderRechargeService, LocalOrderCacheEntry, RechargeSupplierRow, GetPageList, PageMatchingOrderIdsAsync, GetOrderStats, GetOrderRechargeModel, GetOrderRecharge`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OrderRechargeSupplementService.cs`：符号 `OrderRechargeSupplementService, GetAmountLimitAsync, RequiresApprovalAsync, ResolveAmountLimitConfigKey, GetApproverOptionsAsync, EnrichOrderLifeLogs, EnrichOrderResponsesAsync, ApplyDefaults`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/OrderWithdrawalService.cs`：符号 `OrderWithdrawalService, OrderCountCacheEntry, ExactOrderQueryContext, MatchingOrderResolveResult, GetPageList, GetTgPostPageList, PagePrioritylist, GetProcessingTimePagelist`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state`
- `HX.Matching.ServiceCore/Services/SysRoleMenuService.cs`：符号 `SysRoleMenuService, AddRoleMenu, CheckMenuExistRole, DeleteRoleMenuByRoleId, DeleteRoleMenuByRoleIdMenuIds, SelectRoleMenuByRoleId, SelectRoleMenuByRoleIds`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, permission, user-data`
- `HX.Matching.ServiceCore/Services/SysUserRoleService.cs`：符号 `SysUserRoleService, CountUserRoleByRoleId, DeleteUserRoleByUserId, DeleteRoleUserByUserIds, AddUserRole, GetSysUsersByRoleId, GetExcludedSysUsersByRoleId, InsertUserRole`；风险 `cache-consistency, elasticsearch, funds, idempotency, order-state, permission, user-data`
- `HX.Matching.ServiceCore/Services/TokenExpiryNotifyHostedService.cs`：符号 `TokenExpiryNotifyHostedService, ExecuteAsync, NotifyExpiredConnectionsAsync, LoadOnlineClientsAsync`；风险 `cache-consistency, callback, elasticsearch, funds, idempotency, login, order-state`
- 还有 411 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 4 项；明细只写入 `rag/init-reports/20261018T083109Z-project-hx-matching.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

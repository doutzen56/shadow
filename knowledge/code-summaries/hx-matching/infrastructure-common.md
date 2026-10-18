# 基础设施与公共能力

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `hx-matching`
- summary_group: `infrastructure-common`
- host_or_layer: `infrastructure`
- solution_file: `HX.Matching.sln`
- last_ingestion_run: `20261018T083109Z-project-hx-matching`
- file_count: 203
- project_count: 2
- risk_tags: `cache-consistency`, `callback`, `funds`, `gateway`, `login`, `mq`, `order-state`, `permission`, `user-data`

## 职责摘要

缓存、消息、认证、限流、异常处理、帮助类和横切能力。

## 扫描根目录

- `HX.Matching.Infrastructure`
- `HX.Matching.Common`

## 项目文件

- `HX.Matching.Infrastructure/HX.Matching.Infrastructure.csproj`
- `HX.Matching.Common/HX.Matching.Common.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `HX.Matching.Common/AmazonS3Helper.cs`：符号 `AmazonS3Helper, Initialize, InitializeFromConfig, CreateS3Client, PutObjectFromFileAsync, PutObjectFromStreamAsync, DeleteFileAsync, GetObjectStreamAsync`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Common/DynamicApiSimple/ApiConvention.cs`：符号 `ApiConvention, Apply, ClearAction, ConfigureApiExplorer, ConfigureSelector, RemoveEmptySelectors, AddServiceSelector, NormalizeSelectorRoutes`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Common/DynamicApiSimple/ApiFeatureProvider.cs`：符号 `ApiFeatureProvider, IsController`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Common/DynamicApiSimple/DynamicApiAttribute.cs`：符号 `DynamicApiAttribute`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Common/DynamicApiSimple/Extens/DynamicApiExtens.cs`：符号 `DynamicApiExtens, AddDynamicApi`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Common/DynamicApiSimple/IDynamicApi.cs`：符号 `IDynamicApi`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Common/DynamicApiSimple/JsonModelBinder.cs`：符号 `JsonModelBinder, BindModelAsync`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Infrastructure/Cache/UserPermCache.cs`：符号 `UserPermCache, TryRedis, Get, Set, Remove, ClearAll, ClearAllV2Faces, TryDeleteWithPrefix`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/Cap/Events/MerchantFundsDetailEsSyncIntegrationEvent.cs`：符号 `MerchantFundsDetailEsSyncIntegrationEvent, BindFundsDetail`；风险 `cache-consistency, funds, login, mq, permission`
- `HX.Matching.Infrastructure/Cap/Events/OrderEsBackfillIntegrationEvent.cs`：符号 `OrderEsBackfillIntegrationEvent, BindBatch`；风险 `cache-consistency, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Constant/HttpStatus.cs`：符号 `HttpStatus`；风险 `cache-consistency, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Enums/StatsMoneyTypeEnum.cs`：符号 `StatsMoneyTypeEnum`；风险 `cache-consistency, funds, login, mq, permission`
- `HX.Matching.Infrastructure/Enums/StatsOrderTypeEnum.cs`：符号 `StatsOrderTypeEnum`；风险 `cache-consistency, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Helper/AlipayToCardWakeLinkHelper.cs`：符号 `AlipayToCardWakeLinkHelper, IsAlipayToCardCashierProfile, BuildToCardSchemeUri, BuildDsAlipayWakeUrl, BuildH5WakeUrl, WrapDsAlipayScheme, TryBuildServerCashierWakePayload`；风险 `cache-consistency, funds, login, mq, permission`
- `HX.Matching.Infrastructure/Helper/CallbackUrlHelper.cs`：符号 `CallbackUrlHelper, NormalizeCallbackBaseUrl, BuildChannelWithdrawNotifyUrl, BuildChannelRechargeNotifyUrl, ResolveChannelCallbackPrefix, CollapsePathDuplicateSlashes`；风险 `cache-consistency, callback, login, mq, permission`
- `HX.Matching.Infrastructure/Helper/GoogleAuthHelper.cs`：符号 `GoogleAuthHelper, SysConfigRedisEntry, TryConfigBypass, GenerateSecretKey, VerifyCode, GetCurrentCode, GenerateOtpUri`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Infrastructure/Helper/MerchantCashierTokenHelper.cs`：符号 `MerchantCashierTokenHelper, Generate, Sign, SignLegacy, Validate`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Infrastructure/Helper/OrderNumberGenerator.cs`：符号 `OrderNumberGenerator, OrderTypeCode, IsPlatformRechargeOrderId, IsPlatformWithdrawalOrderId, IsMatchingOrderId, IsChannelWithdrawalOrderId, Generate, GenerateSnowFlakePlatformWithdrawal`；风险 `cache-consistency, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Helper/OrderShardingHelper.cs`：符号 `OrderShardingHelper`；风险 `cache-consistency, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Helper/PayQrCodeDisplayHelper.cs`：符号 `PayQrCodeDisplayHelper, HasValue, IsH5PayMode, IsIframePayMode, TryGetIframeUrl, ResolveDisplaySrcAsync, ResolveDisplaySrcSync, IsImageSource`；风险 `cache-consistency, funds, login, mq, permission`
- `HX.Matching.Infrastructure/Helper/WithdrawalPayQrCodeHelper.cs`：符号 `WithdrawalPayQrCodeHelper, ParseResult, IsAlipayAccount, IsMobileAccount, LooksLikeBankCard, IsAlipayToCardBankPayee, IsWithdrawalImageUrl, TryParseApiAccountNumber`；风险 `cache-consistency, funds, login, mq, permission`
- `HX.Matching.Infrastructure/Messaging/CashierRechargePullMessage.cs`：符号 `CashierRechargePullMessage`；风险 `cache-consistency, funds, login, mq, permission`
- `HX.Matching.Infrastructure/Messaging/DisabledMerchantNotifyCallbackBus.cs`：符号 `DisabledMerchantNotifyCallbackBus, TryPublishAsync`；风险 `cache-consistency, callback, login, mq, permission`
- `HX.Matching.Infrastructure/Messaging/IChannelOrderQueryBus.cs`：符号 `IChannelOrderQueryBus, ChannelOrderQueryMessage, ChannelOrderQueryBus, DisabledChannelOrderQueryBus, TryPublishAsync`；风险 `cache-consistency, gateway, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Messaging/IMerchantNotifyCallbackBus.cs`：符号 `IMerchantNotifyCallbackBus`；风险 `cache-consistency, callback, login, mq, permission`
- `HX.Matching.Infrastructure/Messaging/MerchantFundsdetailMessage.cs`：符号 `MerchantFundsdetailMessage, MerchantStatementMessage`；风险 `cache-consistency, funds, login, mq, permission`
- `HX.Matching.Infrastructure/Messaging/MerchantNotifyCallbackBus.cs`：符号 `MerchantNotifyCallbackBus, TryPublishAsync`；风险 `cache-consistency, callback, login, mq, permission`
- `HX.Matching.Infrastructure/Messaging/MerchantNotifyCallbackMessage.cs`：符号 `MerchantNotifyCallbackMessage`；风险 `cache-consistency, callback, login, mq, permission`
- `HX.Matching.Infrastructure/Messaging/MerchantNotifyHttpClientServiceCollectionExtensions.cs`：符号 `MerchantNotifyHttpClientServiceCollectionExtensions, AddMatchingMerchantNotifyHttpClient`；风险 `cache-consistency, callback, login, mq, permission`
- `HX.Matching.Infrastructure/Messaging/MerchantUserTwoFactorStorageBus.cs`：符号 `IMerchantUserTwoFactorStorageBus, MerchantUserTwoFactorStorageBus, PublishMerchantUserTwoFactorStorageAsync`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/Messaging/MerchantUserTwoFactorStorageMessage.cs`：符号 `MerchantUserTwoFactorStorageMessage`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/Messaging/OrderLifeMessage.cs`：符号 `OrderLifeMessage`；风险 `cache-consistency, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Messaging/OrderMessageBus.cs`：符号 `IOrderMessageBus, OrderMessageBus, PublishApiStoreAsync, PublishOrderLifeAsync, PublishserUpDownAsync, PublishserMatchmakingAsync, PublishserMatchmakingAgainAsync, PublishCashierRechargePullAsync`；风险 `cache-consistency, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Messaging/OrderSubmittedMessage.cs`：符号 `OrderSubmittedMessage`；风险 `cache-consistency, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Messaging/OrderWithdrawalMessage.cs`：符号 `OrderWithdrawalMessage, OrderWithdrawalAgainMessage`；风险 `cache-consistency, funds, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Messaging/StatsOrderMessage.cs`：符号 `StatsOrderMessage`；风险 `cache-consistency, login, mq, order-state, permission`
- `HX.Matching.Infrastructure/Messaging/UserMessageBus.cs`：符号 `IUserMessageBus, UserMessageBus, PublishRemoveBlacklistAsync, PublishUserSaveAsync`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/Messaging/UserSaveMessage.cs`：符号 `UserSaveMessage`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/Messaging/UserUpDownMessage.cs`：符号 `UserUpDownMessage`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/Model/SendEmailDto.cs`：符号 `SendEmailDto`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/Model/TokenModel.cs`：符号 `TokenModel, Roles, HasPermission, IsAdmin`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Infrastructure/MultiTenant/LoginDateWriteHold.cs`：符号 `LoginDateWriteHold, Restore, Begin, Dispose`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Infrastructure/MultiTenant/SignalRUserKey.cs`：符号 `SignalRUserKey, Build, FromPrincipal, FromContext`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/RequestUserContext.cs`：符号 `RequestUserContext`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/Security/AuthPromptMessages.cs`：符号 `AuthPromptMessages, PasswordChangeFailed, ForLogOutType`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Infrastructure/Security/GoogleAuthFailGuard.cs`：符号 `GoogleAuthFailGuard, FormatWrongMessage, ClearAsync, MarkLockedAsync, ClearLockedAsync, IsLockedAsync, IsWrongCodeMessage, FailKey`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Infrastructure/Security/GoogleAuthUsedCodeGuard.cs`：符号 `GoogleAuthUsedCodeGuard, ConsumeResult, TryConsumeAsync, TryConsume, OwnerFromUserId, OwnerFromUserName, BuildKey`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Infrastructure/Security/IUserSessionKickService.cs`：符号 `IUserSessionKickService`；风险 `cache-consistency, login, mq, permission, user-data`
- `HX.Matching.Infrastructure/Security/SessionKickStore.cs`：符号 `SessionKickStore, SessionKickInfo, RememberAsync, TryGetAsync, Key`；风险 `cache-consistency, login, mq, permission`
- `HX.Matching.Infrastructure/Services/IPermissionResolver.cs`：符号 `IPermissionResolver`；风险 `cache-consistency, login, mq, permission`
- 还有 153 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 4 项；明细只写入 `rag/init-reports/20261018T083109Z-project-hx-matching.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

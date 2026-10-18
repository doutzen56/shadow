# 后台任务与异步处理

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `hx-matching`
- summary_group: `background-jobs`
- host_or_layer: `background-job`
- solution_file: `HX.Matching.sln`
- last_ingestion_run: `20261018T090035Z-project-hx-matching`
- file_count: 74
- project_count: 2
- risk_tags: `async`, `callback`, `funds`, `mq`, `order-state`, `permission`, `retry`, `user-data`

## 职责摘要

定时任务、消息消费、回调、统计、超时处理和异步补偿入口。

## 扫描根目录

- `HX.Matching.Quartz.Job`
- `HX.Matching.Tasks`

## 项目文件

- `HX.Matching.Quartz.Job/HX.Matching.Quartz.Job.csproj`
- `HX.Matching.Tasks/HX.Matching.Tasks.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `HX.Matching.Quartz.Job/BackService/Telegram/BalanceCommandParser.cs`：符号 `BalanceCommandParser, ParseQueryArguments, TryParseAmountExpression, SuccessQuery, Fail, SplitBySpace, IsExpression, TryCalculate`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/BalanceCommandResult.cs`：符号 `BalanceCommandResult`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/BalanceCommandType.cs`：符号 `BalanceCommandType`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/BalanceReceiptContext.cs`：符号 `BalanceReceiptContext`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/BalanceReceiptMarkdownV1Builder.cs`：符号 `BalanceReceiptMarkdownV1Builder, BuildSuccess, BuildFailure, BuildRemarkLine`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/EnumPayLedgerAmountType.cs`：符号 `EnumPayLedgerAmountType`；风险 `async, callback, funds, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/ManualBalanceTelegramNotifyHelper.cs`：符号 `ManualBalanceTelegramNotifyHelper, TryNotifyAsync, BuildReceiptTextAsync, EscapeMarkdownForPlainText`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/MediaGroupData.cs`：符号 `MediaGroupData, AddMediaMessage, GetMediaMessage`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/MultiTelegramBotHostedService.cs`：符号 `MultiTelegramBotHostedService, ExecuteAsync, HandleUpdateAsync, HandleErrorAsync, SaveOrUpdateGroupAsync, EnsureUserExistsAsync, SaveOrUpdateUserAsync, SaveUserInGroupAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/OkxResultBase.cs`：符号 `OkxResultBase`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/OkxResultData.cs`：符号 `OkxResultData`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/OkxResultSell.cs`：符号 `OkxResultSell`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/OrderExtractResult.cs`：符号 `OrderExtractResult`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/OrderExtractor.cs`：符号 `OrderExtractor, Extract, IsPlausibleOrderId`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/OrderPlaceholder.cs`：符号 `OrderPlaceholder`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/PlatformIDResult.cs`：符号 `PlatformIDResult`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/PureExpressionDetector.cs`：符号 `PureExpressionDetector, IsPureExpression, TryCompute`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/RevokeDto.cs`：符号 `RevokeDto`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/TelegramBotHostedService.cs`：符号 `TelegramBotHostedService, TelegramDbScope, IsPermissionExemptCommand, Dispose, ExecuteAsync, EnsureReloadSubscription, RefreshRateLimitOptionsAsync, StartBotsForCurrentTenantAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/BackService/Telegram/TgMarkdownV1.cs`：符号 `TgMarkdownV1, SafeText, Bold, Code`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Controllers/CommonController.cs`：符号 `CommonController, Index`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Controllers/TasksController.cs`：符号 `TasksController, Start, Stop, Run, RunParam, Update`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/DataInitialization/DataInitializationService.cs`：符号 `DataInitializationService, ExecuteAsync, RunForCurrentTenantAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/DataInitialization/DataTimeOutService.cs`：符号 `DataTimeOutService, ExecuteAsync, DataTimeOutServiceWorkAsync, RunForCurrentTenantAsync, Dispose`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/AdminOrderConsumerHostedService.cs`：符号 `AdminOrderConsumerHostedService, ExecuteAsync, GetOrCreateConnectionAsync, CleanupOldResourcesAsync, HandleOrderLifeQueueMessage, BulkSave, HandleUserUpDownQueueMessage, UpDownServices`；风险 `async, callback, mq, order-state, permission, retry`
- `HX.Matching.Quartz.Job/Messaging/ApiOrderConsumerHostedService.cs`：符号 `ApiOrderConsumerHostedService, ExecuteAsync, GetOrCreateConnectionAsync, CleanupOldResourcesAsync, HandleApiStoreQueueMessage, StopAsync, Dispose`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/CashierAccessLogConsumerHostedService.cs`：符号 `CashierAccessLogConsumerHostedService, ExecuteAsync, HandleMessageAsync, GetOrCreateConnectionAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/ErrorConsumerHostedService.cs`：符号 `ErrorConsumerHostedService, ExecuteAsync, GetOrCreateConnectionAsync, CleanupOldResourcesAsync, StatsErrorSaveQueueMessage, StopAsync, Dispose`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/ExportConsumerHostedService.cs`：符号 `ExportConsumerHostedService, ExecuteAsync, InitTopology, ProcessAsync, HandleRetry`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/ExportExecutionHostedService.cs`：符号 `ExportTaskChannel, ExportExecutionHostedService, WriteAsync, ReadAllAsync, ExecuteAsync, CompensateZombieTasksAsync, ProcessSingleTaskAsync, ProcessSingleTaskCoreAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/MatchmakingHostedService.cs`：符号 `MatchmakingHostedService, ExecuteAsync, GetOrCreateConnectionAsync, CleanupOldResourcesAsync, HandleMatchmakingStoreQueueMessage, HandleMatchmakingAgainStoreQueueMessage, HandleCashierRechargePullMessage, StopAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/MerchantStatementHostedService.cs`：符号 `MerchantStatementHostedService, ExecuteAsync, GetOrCreateConnectionAsync, CleanupOldResourcesAsync, IsMoneyColumnOverflow, MerchantFundsdetailQueueMessage, TryPublishFundsDetailEsSyncAsync, RequeueMessageAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/MerchantUserTwoFactorStorageService.cs`：符号 `MerchantUserTwoFactorStorageService, ExecuteAsync, GetOrCreateConnectionAsync, CleanupOldResourcesAsync, MerchantUserTwoFactorStorageQueueMessage`；风险 `async, callback, mq, order-state, retry, user-data`
- `HX.Matching.Quartz.Job/Messaging/OperLogConsumerHostedService.cs`：符号 `OperLogConsumerHostedService, ExecuteAsync, HandleMessageAsync, GetOrCreateConnectionAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/StatsConsumerHostedService.cs`：符号 `StatsConsumerHostedService, ExecuteAsync, GetOrCreateConnectionAsync, CleanupOldResourcesAsync, HandleOrderMessage, StopAsync, Dispose`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/Messaging/UserConsumerHostedService.cs`：符号 `UserConsumerHostedService, ExecuteAsync, GetOrCreateConnectionAsync, CleanupOldResourcesAsync, HandleUserSaveQueueMessage, HandleRemoveBlacklistQueueMessage, CancelOrder, StopAsync`；风险 `async, callback, mq, order-state, retry, user-data`
- `HX.Matching.Quartz.Job/OrderEsCapBackfillOnceHostedService.cs`：符号 `OrderEsCapBackfillOnceHostedService, StartAsync, StopAsync, RunAndStopAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Quartz.Job/TasksExtension.cs`：符号 `TasksExtension, AddTaskSchedulers, UseAddTaskSchedulers`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_FalseOrder.cs`：符号 `Job_FalseOrder, Execute, Run`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_MaintenanceWithdrawalDrain.cs`：符号 `Job_MaintenanceWithdrawalDrain, Execute, Run`；风险 `async, callback, funds, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_OrderEsCapBackfill.cs`：符号 `Job_OrderEsCapBackfill, Execute, RunAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_OrderEsReconcile.cs`：符号 `Job_OrderEsReconcile, Execute, RunAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_OrderEsSync.cs`：符号 `Job_OrderEsSync, Execute, RunAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_OrderReview.cs`：符号 `Job_OrderReview, FirstResultDto, ReviewResultDto, Execute, Run, FireHttpRequestAsync`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_OrderReviewAi.cs`：符号 `Job_OrderReviewAi, AIRecognitionResultDto, Execute, Run, GetAiResult, OrderFail, GetLog, AreSameByHeadAndTail`；风险 `async, callback, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_RechargeOrderTimeout.cs`：符号 `Job_RechargeOrderTimeout, Execute, Run`；风险 `async, callback, funds, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_RechargeSuccess.cs`：符号 `Job_RechargeSuccess, Execute, Run`；风险 `async, callback, funds, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_WithdrawalDelayOrder.cs`：符号 `Job_WithdrawalDelayOrder, Execute, Run`；风险 `async, callback, funds, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_WithdrawalMantissa.cs`：符号 `Job_WithdrawalMantissa, Execute, Run`；风险 `async, callback, funds, mq, order-state, retry`
- `HX.Matching.Tasks/TaskScheduler/Job_WithdrawalTimeOut.cs`：符号 `Job_WithdrawalTimeOut, Execute, Run`；风险 `async, callback, funds, mq, order-state, retry`
- 还有 24 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 4 项、sensitive_file 2 项；明细只写入 `rag/init-reports/20261018T090035Z-project-hx-matching.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

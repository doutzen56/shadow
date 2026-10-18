# 后台服务

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `betwin`
- summary_group: `background-services`
- host_or_layer: `background-service`
- solution_file: `BetWin5.0.sln`
- last_ingestion_run: `20261018T083037Z-project-betwin`
- file_count: 61
- project_count: 4
- risk_tags: `async`, `cache-consistency`, `callback`, `elasticsearch`, `funds`, `mq`, `order-state`, `retry`, `user-data`

## 职责摘要

Quartz 调度、数据分析与对账、注单采集、RabbitMQ 消费等后台进程。

## 扫描根目录

- `Services/JobSchedule`
- `Services/DataAnalysis`
- `Services/GameLog`
- `Services/MqServer`

## 项目文件

- `Services/JobSchedule/JobSchedule.csproj`
- `Services/DataAnalysis/DataAnalysis.csproj`
- `Services/GameLog/GameLog.csproj`
- `Services/MqServer/MqServer.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `Services/DataAnalysis/Action/AddPointAction.cs`：符号 `AddPointAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/AgentReturnPlan.cs`：符号 `AgentReturnPlan, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/AutoEventLog.cs`：符号 `AutoEventLog, Run, ShowResult, UserInviteTask, InviteRewardTask, InviteTaskTask, CompetitionTask, WashingTask`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/CheckGemeLogDelayAction.cs`：符号 `CheckGemeLogDelayAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/CheckInEvent.cs`：符号 `CheckInEvent, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/CheckWithdraw.cs`：符号 `CheckWithdraw, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/DailyReportAction.cs`：符号 `DailyReportAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/DataMaintenanceAction.cs`：符号 `DataMaintenanceAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/GameEnteranceAction.cs`：符号 `GameEnteranceAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/GameLogImport.cs`：符号 `GameLogImport, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/GameMaintainPlanAction.cs`：符号 `GameMaintainPlanAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/IAction.cs`：符号 `IAction`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/PointRecordAction.cs`：符号 `PointRecordAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/ProxyStatistics.cs`：符号 `ProxyStatistics, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/RiskAction.cs`：符号 `RiskAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/SynGameLog.cs`：符号 `SynGameLog, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/SyncAgentGameDateAction.cs`：符号 `SyncAgentGameDateAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/SyncUserGameDateAction.cs`：符号 `SyncUserGameDateAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry, user-data`
- `Services/DataAnalysis/Action/TeamStatistics.cs`：符号 `TeamStatistics, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/TelegramAction.cs`：符号 `TelegramAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/TicketNotify.cs`：符号 `TicketNotify, Run, ShowResult`；风险 `async, callback, elasticsearch, funds, mq, order-state, retry`
- `Services/DataAnalysis/Action/UpdateSystem.cs`：符号 `UpdateSystem, ShowResult, Run`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Action/UpdateUserActive.cs`：符号 `UpdateUserActive, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry, user-data`
- `Services/DataAnalysis/Action/UpdateUserExper.cs`：符号 `UpdateUserExper, ShowResult, Run`；风险 `async, elasticsearch, funds, mq, retry, user-data`
- `Services/DataAnalysis/Action/UpdateUserMoney.cs`：符号 `UpdateUserMoney, ShowResult, Run`；风险 `async, elasticsearch, funds, mq, retry, user-data`
- `Services/DataAnalysis/Action/UpdateUserVIP.cs`：符号 `UpdateUserVIP, ShowResult, Run`；风险 `async, elasticsearch, funds, mq, retry, user-data`
- `Services/DataAnalysis/Action/WashFlowAction.cs`：符号 `WashFlowAction, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/BackgroundService.cs`：符号 `BackgroundService, Start, Stop`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/DataTools/GameLogImport.cs`：符号 `GameLogImport, Run, importGameLog, syncOrder, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/DataTools/GameRedisHandle.cs`：符号 `GameRedisHandle, Run, ShowResult, Handle`；风险 `async, cache-consistency, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/DataTools/SynGameLogImport.cs`：符号 `SynGameLogImport, importGameLogLossData, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/DataTools/TransferByGame.cs`：符号 `TransferByGame, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/DataTools/UpdateUserAgent.cs`：符号 `UpdateUserAgent, Run, ShowResult`；风险 `async, elasticsearch, funds, mq, retry, user-data`
- `Services/DataAnalysis/DataTools/UserInfoCache.cs`：符号 `UserInfoCache, Run, ShowResult`；风险 `async, cache-consistency, elasticsearch, funds, mq, retry, user-data`
- `Services/DataAnalysis/DataTools/UserMoneyCache.cs`：符号 `UserMoneyCache, Run, ShowResult`；风险 `async, cache-consistency, elasticsearch, funds, mq, retry, user-data`
- `Services/DataAnalysis/DevNull.cs`：符号 `DevNull`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/DataAnalysis/Program.cs`：符号 `Program, Main`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/GameLog/BackgroundService.cs`：符号 `BackgroundService, StartAsync, Stop`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/GameLog/BackgroundServiceNew.cs`：符号 `BackgroundServiceNew, DownloadJob, Start, Stop, LoadAssemblyJobs, ExecuteDelayJobAsync`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/GameLog/BackgroundServiceNewV2.cs`：符号 `BackgroundServiceNewV2, SyncPlatformJob, DownloadJobV2, Start, Stop, SyncPlatformJobs, ScheduleSyncJob, Execute`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/GameLog/Debug.cs`：符号 `Debug`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/GameLog/Games/Config.cs`：符号 `Config, Run, RunV2, NewRun, SportFbLog, Dispose`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/GameLog/Program.cs`：符号 `Program, Main`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/JobSchedule/BackgroundService.cs`：符号 `BackgroundService, Start, Stop`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/JobSchedule/Program.cs`：符号 `Program, Main`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/MqServer/Consumer/DataProviderNewsConsumer.cs`：符号 `DataProviderNewsConsumer, Consumer`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/MqServer/MessageQueue/ConsumerBase.cs`：符号 `ConsumerBase, FailureHandling, SaveFaildMessage, SaveLog`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/MqServer/MessageQueue/DataProviderNewsQueue.cs`：符号 `DataProviderNewsQueue`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/MqServer/MessageQueue/ExchangeName.cs`：符号 `ExchangeName`；风险 `async, elasticsearch, funds, mq, retry`
- `Services/MqServer/MessageQueue/Producer.cs`：符号 `Producer, Send`；风险 `async, elasticsearch, funds, mq, retry`
- 还有 11 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 6 项、sensitive_file 12 项；明细只写入 `rag/init-reports/20261018T083037Z-project-betwin.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

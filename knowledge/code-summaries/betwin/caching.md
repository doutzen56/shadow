# 业务缓存

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `betwin`
- summary_group: `caching`
- host_or_layer: `cache`
- solution_file: `BetWin5.0.sln`
- last_ingestion_run: `20261018T083037Z-project-betwin`
- file_count: 51
- project_count: 1
- risk_tags: `cache-consistency`, `funds`, `gateway`, `idempotency`, `order-state`, `permission`, `user-data`

## 职责摘要

Redis 业务缓存：用户态、资金点位、活动次数、站点配置等热点数据。

## 扫描根目录

- `Library/BetWin.Caching`

## 项目文件

- `Library/BetWin.Caching/BetWin.Caching.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `Library/BetWin.Caching/BetWin.Caching.csproj`：符号 `未提取到符号`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/CacheBase.cs`：符号 `CacheBase, NewExecutor, Server, Connection, KeyDelete, Instance`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Comm/RepeatSubmitCaching.cs`：符号 `RepeatSubmitCaching, SaveRepeatSubmitCnt, GetRepeatSubmitCnt`；风险 `cache-consistency, idempotency, user-data`
- `Library/BetWin.Caching/Comm/ValidateCodeCaching.cs`：符号 `ValidateCodeCaching, SaveValidateCode, GetValidateCode`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Events/BindDataCaching.cs`：符号 `BindDataCaching, ISExecToday, GetExecToday, SaveBindDataPoint, GetExistBindData, GetBindDataPoint`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Events/FirstRechargeCaching.cs`：符号 `FirstRechargeCaching, SaveFirstRechargePoint, GetExistFirstRecharge, GetFirstRechargePoint, GettFirstRechargeList`；风险 `cache-consistency, funds, user-data`
- `Library/BetWin.Caching/Events/InviteTaskCaching.cs`：符号 `InviteTaskCaching, ISExecToday, GetExecToday, SaveInviteTaskPoint, GetExistInviteTask, GetInviteTaskPoint`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Events/LongChechInCaching.cs`：符号 `LongCheckInCaching, SaveLongCheckInPoint, GetExistLongCheckIn, GetLongCheckInPoint`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Events/RechargeByLevel.cs`：符号 `RechargeByLevelCaching, ISExecToday, GetExecToday, SaveRechargeByLevelPoint, GetExistRechargeByLevel, GetExistRechargeByLevelSingle, GetRechargeByLevelSingleCanOpen, GetRechargeByLevelPoint`；风险 `cache-consistency, funds, user-data`
- `Library/BetWin.Caching/Events/RechargeCaching.cs`：符号 `RechargeCaching, SaveRechargePoint, GetRechargePoint, GetRechargeRecord, AddRechargeQueue, GetRechargeQueue, RemoveRechargeQueue`；风险 `cache-consistency, funds, user-data`
- `Library/BetWin.Caching/Events/RedEnvelopeCaching.cs`：符号 `RedEnvelopeCaching, ISExecToday, GetExecToday, SaveRedEnvelopePoint, GetExistRedEnvelope, GetRedEnvelopePoint, GetRedEnvelopePoints, GetRedEnvelopeTimes`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Events/SiteEventCaching.cs`：符号 `SiteEventCaching, GetSiteEventList, GetSiteEventName, SaveSiteEventName, SaveSiteEventInfo, GetSiteEventInfo, GetSiteEventLockStatus, UpdateSiteEventInfo`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Events/WashingCaching .cs`：符号 `WashingCaching, ISExecToday, GetExecToday, SaveWashingPoint, GetExistWashing, GetWashingPoint, GetWashingLastDate`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Events/WeekTasksCaching.cs`：符号 `FullWeekCaching, ISExecToday, GetExecToday, SaveFullWeekPoint, GetExistFullWeek, GetFullWeekPoint`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Events/WithdrawCaching.cs`：符号 `WithdrawCaching, SaveWithdrawPoint, GetWithdrawPoint`；风险 `cache-consistency, funds, user-data`
- `Library/BetWin.Caching/Filter/FilterCaching.cs`：符号 `FilterCaching, FilterInitializeOption, FilterConfig, FilterStatus, InitializeFilterConfigs, Initialize, With, AddAsync`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Filter/FilterConfigBootstrapper.cs`：符号 `FilterConfigBootstrapper, InitializeDefaults`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Filter/FilterType.cs`：符号 `FilterType`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Games/GameCaching.cs`：符号 `GameCaching, SaveGameTypesByHourToRedis, GetGameTypesByHourFromRedis, GetGameSetting, SaveGameSetting, CheckTransferTime, GetDictionary, SaveDictionary`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Page/PageCaching.cs`：符号 `PageCaching, Save, Get`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Reports/ReportCaching.cs`：符号 `ReportCaching, AddMoneyLogQueue, GetMoneyLogQueue, AddRechargeQueue, AddRechargeBackQueue, GetRechargeQueue, GetRechargeList, GetRechargeBackQueue`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Risk/RiskCaching.cs`：符号 `RiskCaching, CheckRiskUserIP, CheckRiskUserDevice, CheckReqIp, SetLockLoginIp, CheckLockIp, RemoveLockIp, GetReqCount`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Sites/AdminCaching.cs`：符号 `AdminCaching, GetAdminToken, SaveAdminToken, DeleteAdminToken, SaveAdminInfo, GetAdminInfo, SaveGroupPermission, DeleteGroup`；风险 `cache-consistency, permission, user-data`
- `Library/BetWin.Caching/Sites/OrderCaching.cs`：符号 `OrderCaching, GetOrderID, SaveOrderID, DeleteOrderID`；风险 `cache-consistency, order-state, user-data`
- `Library/BetWin.Caching/Sites/SiteCaching.cs`：符号 `SiteCaching, SaveSiteBaseCurrency, GetSiteBaseCurrency, SaveJackpot, AddJackpot, GetJackpot, SaveSiteCurrency, GetSiteCurrency`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Sites/SiteGameCaching.cs`：符号 `SiteGameCaching, SaveSiteGameRecommend, GetSiteGameRecommend, SaveSiteGameNew, GetSiteGameNew, GetSiteGameListHashKey, GetSiteGameListCacheKey, InvalidateSiteGameListCache`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Sites/SiteHostCaching.cs`：符号 `SiteHostCaching, GetSiteID, SaveSiteID, DeleteSiteID`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Sites/SiteVipConfigCaching.cs`：符号 `SiteVipConfigCaching, SetSiteVipStartTime, GetSiteVipStartTime, SetSiteVipStartTimeEnabled, GetSiteVipStartTimeEnabled, GetUserBetAmount, SaveUserBetAmount, GetUserRecharge`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Sites/WithdrawChannelCaching.cs`：符号 `WithdrawChannelCaching, SaveChannelName, GetChannelName, GetChannelInfo, SaveChannelInfo, RemoveChannelInfo`；风险 `cache-consistency, funds, gateway, user-data`
- `Library/BetWin.Caching/Sports/SportsCaching.cs`：符号 `SportsCaching, Save, Get`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Systems/AnalysisCaching.cs`：符号 `AnalysisCaching, GetBirthdayAction, SaveBirthdayAction, GetBetAction, SaveBetAction, GetBirthdayGiftAction, SaveBirthdayGiftAction`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Systems/ChartCaching.cs`：符号 `ChartCaching, GetChart, CleanChart, SaveChart, RemoveChart`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Systems/ConfigCaching.cs`：符号 `ConfigCaching, GetFaildCount, SaveFaildCount, ClearFaildCount, SaveExchangeRate, GetExchangeRate, SaveEnumConfig, GetEnumValue`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Systems/GameTagCaching.cs`：符号 `GameTagCaching, SaveGameTag, GetGameTag`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Systems/ServerCaching.cs`：符号 `ServerCaching, GetSystemServer, SaveSiteServer, GetSiteServer`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Systems/SystemCaching.cs`：符号 `SystemCaching, SaveAdminToken, DeleteAdminToken, GetAdminID, GetAdminToken, SavePermission, DeleteSystemAdmin, GetPermission`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Systems/TagCaching.cs`：符号 `TagCaching, GetTagInfo, GetTagList, GetPageError, SaveTagInfo, DeleteTag`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/AgentUserCaching.cs`：符号 `AgentUserCaching, UpdateAgentUserInfo`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/CompetitionCaching.cs`：符号 `CompetitionCaching, SaveCompetitionPoint, GetCompetitionExist, SaveCompetitionExist, GetCompetitionPoint, GetCompetitionScore, GetCompetitionRank`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/DailyTasksCaching .cs`：符号 `DailyTasksCaching, ISExecToday, GetExecToday, SaveDailyTasksPoint, GetExistDailyTasks, GetExistDailySingle, GetDailySingleCanOpen, GetDailyTasksPoint`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/EventCaching.cs`：符号 `EventCaching, ISExecToday, GetExecToday, SaveDelotteryCount, SaveDelotteryInviteCount, GetExistLotteryCount, GetInviteCount, UseDelotteryCount`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/MoneyCaching.cs`：符号 `MoneyCaching, GetWalletKey, SaveUserMoney, AddUserMoney, GetLogID, SetLogID, AddUserLockMoney, GetUserMoney`；风险 `cache-consistency, funds, user-data`
- `Library/BetWin.Caching/Users/NewDailyTasksCaching.cs`：符号 `NewDailyTasksCaching, ISExecToday, GetExecToday, SaveNewDailyTasksPoint, GetExistNewDailyTasks, GetExistNewDailySingle, GetNewDailySingleCanOpen, GetNewDailyTasksPoint`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/NewWeekTasksCaching.cs`：符号 `NewWeekTasksCaching, ISExecToday, GetExecToday, SaveNewWeekTasksPoint, GetExistNewWeekTasks, GetNewWeekTasksCanOpen, GetNewWeekTasksPoint`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/RebateV2Caching.cs`：符号 `RebateV2Caching, ClaimMarkCacheItem, TryGetSyncMark, SetSyncMark, TryGetClaimMark, SetClaimMark, SetClaimMarkAbsent, TryGetClaimMarks`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/RebateV2Time.cs`：符号 `RebateV2Time, FromEs, FromAppClock, ToAppClock, FormatUtc, TryParseUtc`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/RescueCaching.cs`：符号 `RescueCaching, ISExecToday, GetExecToday, SaveRescuePoint, GetExistRescue, GetMaxCanOpen, GetRescuePoint, GetRescueMoney`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/UserCaching.cs`：符号 `UserCaching, DelAll, GetGroupName, SaveGroupInfo, SaveUserActiveStatus, GetUserActive, SetUserSessionExpired, UserSessionExpiredKeyIsExist`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/UserMarkCaching.cs`：符号 `UserMarkCaching, GetMark, SaveMark, DeleteMark`；风险 `cache-consistency, user-data`
- `Library/BetWin.Caching/Users/WeekTasksCaching.cs`：符号 `WeekTasksCaching, ISExecToday, GetExecToday, SaveWeekTasksPoint, GetExistWeekTasks, GetWeekTasksPoint`；风险 `cache-consistency, user-data`
- 还有 1 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 1 项、sensitive_file 2 项；明细只写入 `rag/init-reports/20261018T083037Z-project-betwin.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

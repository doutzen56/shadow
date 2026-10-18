# 业务缓存

- project_id: `betwin`
- host_or_layer: `cache`
- roots: `Library/BetWin.Caching`
- file_count: 53
- generated_at: 2026-10-18T06:47:04Z

## 风险标签

`cache-consistency`, `funds`, `gateway`, `idempotency`, `order-state`, `permission`, `user-data`

## 代表文件

- `Library/BetWin.Caching/app.config`：符号 `未提取到公开符号`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/BetWin.Caching.csproj`：符号 `未提取到公开符号`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/CacheBase.cs`：符号 `CacheBase, NewExecutor, Connection, Instance`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Comm/RepeatSubmitCaching.cs`：符号 `RepeatSubmitCaching, SaveRepeatSubmitCnt, GetRepeatSubmitCnt`；风险 `cache-consistency, idempotency, user-data`。
- `Library/BetWin.Caching/Comm/ValidateCodeCaching.cs`：符号 `ValidateCodeCaching, SaveValidateCode, GetValidateCode`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Events/BindDataCaching.cs`：符号 `BindDataCaching, ISExecToday, GetExecToday, SaveBindDataPoint, GetExistBindData, GetBindDataPoint`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Events/FirstRechargeCaching.cs`：符号 `FirstRechargeCaching, SaveFirstRechargePoint, GetExistFirstRecharge, GetFirstRechargePoint, GettFirstRechargeList`；风险 `cache-consistency, funds, user-data`。
- `Library/BetWin.Caching/Events/InviteTaskCaching.cs`：符号 `InviteTaskCaching, ISExecToday, GetExecToday, SaveInviteTaskPoint, GetExistInviteTask, GetInviteTaskPoint`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Events/LongChechInCaching.cs`：符号 `LongCheckInCaching, SaveLongCheckInPoint, GetExistLongCheckIn, GetLongCheckInPoint`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Events/RechargeByLevel.cs`：符号 `RechargeByLevelCaching, ISExecToday, GetExecToday, SaveRechargeByLevelPoint, GetExistRechargeByLevel, GetExistRechargeByLevelSingle, GetRechargeByLevelSingleCanOpen, GetRechargeByLevelPoint`；风险 `cache-consistency, funds, user-data`。
- `Library/BetWin.Caching/Events/RechargeCaching.cs`：符号 `RechargeCaching, SaveRechargePoint, GetRechargePoint, GetRechargeRecord, AddRechargeQueue, GetRechargeQueue, RemoveRechargeQueue`；风险 `cache-consistency, funds, user-data`。
- `Library/BetWin.Caching/Events/RedEnvelopeCaching.cs`：符号 `RedEnvelopeCaching, ISExecToday, GetExecToday, SaveRedEnvelopePoint, GetExistRedEnvelope, GetRedEnvelopePoint, GetRedEnvelopePoints, GetRedEnvelopeTimes`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Events/SiteEventCaching.cs`：符号 `SiteEventCaching, GetSiteEventList, GetSiteEventName, SaveSiteEventName, SaveSiteEventInfo, GetSiteEventInfo, UpdateSiteEventInfo, GetSiteID`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Events/WashingCaching .cs`：符号 `WashingCaching, ISExecToday, GetExecToday, SaveWashingPoint, GetExistWashing, GetWashingPoint, GetWashingLastDate`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Events/WeekTasksCaching.cs`：符号 `FullWeekCaching, ISExecToday, GetExecToday, SaveFullWeekPoint, GetExistFullWeek, GetFullWeekPoint`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Events/WithdrawCaching.cs`：符号 `WithdrawCaching, SaveWithdrawPoint, GetWithdrawPoint`；风险 `cache-consistency, funds, gateway, user-data`。
- `Library/BetWin.Caching/Filter/FilterCaching.cs`：符号 `FilterCaching, FilterInitializeOption, FilterConfig, FilterStatus, InitializeFilterConfigs, Initialize, With, AddAsync`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Filter/FilterConfigBootstrapper.cs`：符号 `FilterConfigBootstrapper, InitializeDefaults`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Filter/FilterType.cs`：符号 `FilterType`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Games/GameCaching.cs`：符号 `GameCaching, SaveGameTypesByHourToRedis, GetGameTypesByHourFromRedis, GetGameSetting, SaveGameSetting, CheckTransferTime, GetDictionary, SaveDictionary`；风险 `cache-consistency, gateway, user-data`。
- `Library/BetWin.Caching/packages.config`：符号 `未提取到公开符号`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Page/PageCaching.cs`：符号 `PageCaching, Save, Get`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Properties/AssemblyInfo.cs`：符号 `未提取到公开符号`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Reports/ReportCaching.cs`：符号 `ReportCaching, AddMoneyLogQueue, GetMoneyLogQueue, AddRechargeQueue, AddRechargeBackQueue, GetRechargeQueue, GetRechargeList, GetRechargeBackQueue`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Risk/RiskCaching.cs`：符号 `RiskCaching, CheckRiskUserIP, CheckRiskUserDevice, CheckReqIp, SetLockLoginIp, CheckLockIp, RemoveLockIp, GetReqCount`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Sites/AdminCaching.cs`：符号 `AdminCaching, GetAdminToken, SaveAdminToken, DeleteAdminToken, SaveAdminInfo, GetAdminInfo, SaveGroupPermission, DeleteGroup`；风险 `cache-consistency, permission, user-data`。
- `Library/BetWin.Caching/Sites/OrderCaching.cs`：符号 `OrderCaching, GetOrderID, SaveOrderID, DeleteOrderID`；风险 `cache-consistency, order-state, user-data`。
- `Library/BetWin.Caching/Sites/SiteCaching.cs`：符号 `SiteCaching, SaveSiteBaseCurrency, GetSiteBaseCurrency, SaveJackpot, AddJackpot, GetJackpot, SaveSiteCurrency, GetSiteCurrency`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Sites/SiteGameCaching.cs`：符号 `SiteGameCaching, SaveSiteGameRecommend, GetSiteGameRecommend, SaveSiteGameNew, GetSiteGameNew, GetSiteGameListHashKey, GetSiteGameListCacheKey, InvalidateSiteGameListCache`；风险 `cache-consistency, gateway, user-data`。
- `Library/BetWin.Caching/Sites/SiteHostCaching.cs`：符号 `SiteHostCaching, GetSiteID, SaveSiteID, DeleteSiteID`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Sites/SiteVipConfigCaching.cs`：符号 `SiteVipConfigCaching, SetSiteVipStartTime, GetSiteVipStartTime, SetSiteVipStartTimeEnabled, GetSiteVipStartTimeEnabled, SaveUserBetAmount, SaveUserRecharge, SaveThreeMonthBetAmount`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Sites/WithdrawChannelCaching.cs`：符号 `WithdrawChannelCaching, SaveChannelName, GetChannelName, GetChannelInfo, SaveChannelInfo, RemoveChannelInfo`；风险 `cache-consistency, funds, gateway, user-data`。
- `Library/BetWin.Caching/Sports/SportsCaching.cs`：符号 `SportsCaching, Save, Get`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Systems/AnalysisCaching.cs`：符号 `AnalysisCaching, GetBirthdayAction, SaveBirthdayAction, GetBetAction, SaveBetAction, GetBirthdayGiftAction, SaveBirthdayGiftAction`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Systems/ChartCaching.cs`：符号 `ChartCaching, GetChart, CleanChart, SaveChart, RemoveChart`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Systems/ConfigCaching.cs`：符号 `ConfigCaching, GetFaildCount, SaveFaildCount, ClearFaildCount, SaveExchangeRate, GetExchangeRate, SaveEnumConfig, GetEnumValue`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Systems/GameTagCaching.cs`：符号 `GameTagCaching, SaveGameTag, GetGameTag`；风险 `cache-consistency, gateway, user-data`。
- `Library/BetWin.Caching/Systems/ServerCaching.cs`：符号 `ServerCaching, GetSystemServer, SaveSiteServer, GetSiteServer`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Systems/SystemCaching.cs`：符号 `SystemCaching, SaveAdminToken, DeleteAdminToken, GetAdminID, GetAdminToken, SavePermission, DeleteSystemAdmin, GetPermission`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Systems/TagCaching.cs`：符号 `TagCaching, GetTagInfo, GetTagList, GetPageError, SaveTagInfo, DeleteTag`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/AgentUserCaching.cs`：符号 `AgentUserCaching, UpdateAgentUserInfo`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/CompetitionCaching.cs`：符号 `CompetitionCaching, SaveCompetitionPoint, GetCompetitionExist, SaveCompetitionExist, GetCompetitionRank`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/DailyTasksCaching .cs`：符号 `DailyTasksCaching, ISExecToday, GetExecToday, SaveDailyTasksPoint, GetExistDailyTasks, GetExistDailySingle, GetDailySingleCanOpen, GetDailyTasksPoint`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/EventCaching.cs`：符号 `EventCaching, ISExecToday, GetExecToday, SaveDelotteryCount, SaveDelotteryInviteCount, GetExistLotteryCount, GetInviteCount, UseDelotteryCount`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/MoneyCaching.cs`：符号 `MoneyCaching, GetWalletKey, SaveUserMoney, AddUserMoney, GetLogID, SetLogID, AddUserLockMoney, SaveUserWallet`；风险 `cache-consistency, funds, user-data`。
- `Library/BetWin.Caching/Users/NewDailyTasksCaching.cs`：符号 `NewDailyTasksCaching, ISExecToday, GetExecToday, SaveNewDailyTasksPoint, GetExistNewDailyTasks, GetExistNewDailySingle, GetNewDailySingleCanOpen, GetNewDailyTasksPoint`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/NewWeekTasksCaching.cs`：符号 `NewWeekTasksCaching, ISExecToday, GetExecToday, SaveNewWeekTasksPoint, GetExistNewWeekTasks, GetNewWeekTasksCanOpen, GetNewWeekTasksPoint`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/RebateV2Caching.cs`：符号 `RebateV2Caching, ClaimMarkCacheItem, TryGetSyncMark, SetSyncMark, TryGetClaimMark, SetClaimMark, SetClaimMarkAbsent, TryGetClaimMarks`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/RebateV2Time.cs`：符号 `RebateV2Time, FromEs, FromAppClock, ToAppClock, FormatUtc, TryParseUtc`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/RescueCaching.cs`：符号 `RescueCaching, ISExecToday, GetExecToday, SaveRescuePoint, GetExistRescue, GetMaxCanOpen, GetRescuePoint, GetRescueMoney`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/UserCaching.cs`：符号 `UserCaching, DelAll, GetGroupName, SaveGroupInfo, SaveUserActiveStatus, GetUserActive, SetUserSessionExpired, UserSessionExpiredKeyIsExist`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/UserMarkCaching.cs`：符号 `UserMarkCaching, GetMark, SaveMark, DeleteMark`；风险 `cache-consistency, user-data`。
- `Library/BetWin.Caching/Users/WeekTasksCaching.cs`：符号 `WeekTasksCaching, ISExecToday, GetExecToday, SaveWeekTasksPoint, GetExistWeekTasks, GetWeekTasksPoint`；风险 `cache-consistency, user-data`。

## 跳过项

- `Library/BetWin.Caching/obj/Debug/.NETFramework,Version=v4.8.AssemblyAttributes.cs`：forbidden_directory
- `Library/BetWin.Caching/obj/Debug/BetWin.C.54E0C57B.Up2Date`：forbidden_directory
- `Library/BetWin.Caching/obj/Debug/BetWin.Caching.csproj.AssemblyReference.cache`：forbidden_directory
- `Library/BetWin.Caching/obj/Debug/BetWin.Caching.csproj.CoreCompileInputs.cache`：forbidden_directory
- `Library/BetWin.Caching/obj/Debug/BetWin.Caching.csproj.FileListAbsolute.txt`：forbidden_directory
- `Library/BetWin.Caching/obj/Debug/BetWin.Caching.dll`：forbidden_directory
- `Library/BetWin.Caching/obj/Debug/BetWin.Caching.pdb`：forbidden_directory
- `Library/BetWin.Caching/obj/Debug/DesignTimeResolveAssemblyReferencesInput.cache`：forbidden_directory

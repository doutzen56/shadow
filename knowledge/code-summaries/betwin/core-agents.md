# 业务 Agent

- project_id: `betwin`
- host_or_layer: `business-agent`
- roots: `Library/BetWin`
- file_count: 178
- generated_at: 2026-10-18T06:47:04Z

## 风险标签

`cache-consistency`, `callback`, `elasticsearch`, `funds`, `gateway`, `order-state`, `permission`, `user-data`

## 代表文件

- `Library/BetWin/Agent/AgentBase.cs`：符号 `AgentBase, Instance`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Cache/GameSettingRateCache.cs`：符号 `GameSettingRateCache, DeleteGameSettingRates`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Cache/SiteExchangeRateCache.cs`：符号 `SiteExchangeRateCache, GetSiteExchangeRate, DeleteSiteExchangeRate`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Cache/TelegramSettingCache.cs`：符号 `TelegramSettingCache, GetTelegramConfig, DeleteTelegramConfigCache`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Exchange/ExchangeRateAgent.cs`：符号 `ExchangeRateAgent, SetSiteBaseCurrency, UpdateSiteRate, SaveSiteExchangeRate, GetExchangeRate, GetbaseCurrency, GetExchangeRateByBaseCurrency, GetSiteExchangeRateBase`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Exchange/ExchangeReviewAgent.cs`：符号 `ExchangeReviewAgent, Save, GetExchangeRate, ExchangeCurrency, Check`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Exper/ExperAgent.cs`：符号 `ExperAgent, AddExper, WriteLog, IsExistsLog, ExceAdd, ExceManualAdd, UpdateUserExper`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Game/GameAgent-User.cs`：符号 `GameAgent, GetGameUser, AddGameUser, GetGameUserList, CheckPlayerName, GetUserID, GetFlowRate, GetPlayerName`；风险 `cache-consistency, funds, gateway, order-state, user-data`。
- `Library/BetWin/Agent/Games/GameDelegate.cs`：符号 `GameDelegate, SetGetAll, GetDictionary, GetGameUser, GetMarkTime, GetTransferOrder, SaveDictionary, SaveMarkTime`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Games/GameEnteranceAgent.cs`：符号 `GameEnteranceAgent, SiteGameEnteranceSync, SynchronousCloseData`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Games/GameLogAgent.cs`：符号 `GameLogAgent, PreProcessing, GetStatus, Save, ImportLog, UpdateStatusV1, UpdateStatus, GetOrderQueryV1`；风险 `cache-consistency, elasticsearch, funds, gateway, order-state`。
- `Library/BetWin/Agent/Games/GameSettingAgent.cs`：符号 `GameSettingAgent, GetSiteIDByGameIsSelf, GetGameSetting, GetGameName, GetGameSettingInfo, SaveGameSettingSort, GetGameSettingList, UpdateGameSettingSort`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Games/GameTagAgent.cs`：符号 `GameTagAgent, GetGameTagInfo, SaveTagInfo, DeleteGameTag, SaveGameTagCaching, GetGameTags, GetTagName`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Games/PlayerAgent.cs`：符号 `PlayerAgent, GetUserID, GetGameUserList`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/HttpAgentConfig.cs`：符号 `HttpAgentConfig, GetUrl`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Points/PointsAgent.cs`：符号 `PointsAgent, AddPoint, WriteLog, PointReduce, IsExistsLog, ExceAdd, LockPoint, UnLockPoint`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/PROC.cs`：符号 `PROC, usr, rpt, game, site, data`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Proxy/AgentUserAgent.cs`：符号 `AgentUserAgent, GetInfo, UpdateAgentUserInfo, UpdateAgentUserCommission, UpdateAgentUserReturn, UpdateToAgent, AddAgentUser`；风险 `cache-consistency, funds, gateway, order-state, user-data`。
- `Library/BetWin/Agent/Proxy/AssignAgent.cs`：符号 `AssignAgent`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Proxy/CommissionAgent.cs`：符号 `CommissionAgent, GetCommissionInfo, GetAgentCommissions, GetAllAgentCommissions, UpdateComissionsAgentCount, SaveCommissionInfo, DeleteCommission`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Proxy/ProxyRateAgent.cs`：符号 `ProxyRateAgent, GetProxyRateInfo, GetProxyRateLast, AddProxyRate, UpdateProxyRate, GetWashingRatios`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Proxy/ReturnAgent.cs`：符号 `ReturnAgent, SaveReturnPlan, GetReturnPlan, GetAgentReturn, ExecuteAgentReturn, CheckReturnLog, ReExecute, DeleteAgentReturn`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Proxy/SportsAgent.cs`：符号 `SportsAgent, GetYaboResult, GetYaboContentResult`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Pusher/SocketAgent.cs`：符号 `SocketAgent, SocketType, Socket, SendMessage`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Pusher/TelegramAgent.cs`：符号 `TelegramAgent, SendAsync, SendAsyncByInterval, SendAsyncByRecharge, SendAsyncByWithdrawal, SendAsyncByFRecharge, SendAsyncByBet, SendAsyncByVip`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Report/ReportAgent-User.cs`：符号 `ReportAgent, GetUserDateMoney, GetUserMoneyType, SaveUserBetAmount, SaveUserRechargeAmount, GetUserDayBetAmount, GetUserWeekBetAmount, GetUserMonthBetAmount`；风险 `cache-consistency, funds, gateway, order-state, user-data`。
- `Library/BetWin/Agent/Reports/ChartAgent.cs`：符号 `ChartSettingAgent, GetDataTable, GetDbParameters, SaveChartSetting`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Reports/GameReportAgent.cs`：符号 `GameReportAgent, CurrencyAgg, EsUserTypeCurrencyAggRow, SaveUserDateGame, SaveUserDateGameFromES, ParseCompositeAggregationResult, SaveUserDateGameBattleFromES, ParseCompositeAggregationBattleResult`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Reports/ProxyReportAgent.cs`：符号 `ProxyReportAgent, SaveProxyMoneySts`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Reports/SiteGameReportAgent.cs`：符号 `SiteGameReportAgent, GetBetAmount`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Reports/SiteReportAgent.cs`：符号 `SiteReportAgent, GetSiteProfit, GetSiteMoneyCategory, GetSiteSystemDataReport, IsExistDataByRptStatis`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Reports/TeamReportAgent.cs`：符号 `TeamReportAgent, GetValidUserCount, GetNewUserCount, AgentCommissionData, GetTeamReport, GetTeamBetAmountStatistic, SaveTeamRecharge, SaveTeamRechargeDue`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Risk/IpRiskDetector.cs`：符号 `IpRiskDetector, IpCompareResult, AnalyzeIpRisk`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Risk/RiskAgent.cs`：符号 `RiskAgent, RiskUserDangerContextExtensions, DayModifyAmountPassword, DayModifyLoinPassword, NoBindPhoneOrEmall, IpLoginDifUser, UserLoginDifIp, UserLoginIpRisk`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Risk/UserDeviceAgent.cs`：符号 `UserDeviceAgent, SaveUserDevice`；风险 `cache-consistency, funds, gateway, order-state, user-data`。
- `Library/BetWin/Agent/Risk/UserIPAgent.cs`：符号 `UserIPAgent, SaveUserIP`；风险 `cache-consistency, funds, gateway, order-state, user-data`。
- `Library/BetWin/Agent/Shop/ShopAgent.cs`：符号 `ShopAgent, GetCategoryInfo, GetCategories, SaveCateInfo, DeleteCate, SaveProductInfo, GetProductInfo, GetShopProduct`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/AdminAgent.cs`：符号 `AdminAgent, GetAdminInfo, GetAdminID, GetAdminName, GetRealName, Login, CheckGoogleSecret, Logout`；风险 `cache-consistency, funds, gateway, order-state, permission`。
- `Library/BetWin/Agent/Sites/APPAgent.cs`：符号 `SiteAPPAgent, UploadAPPFile, SaveAPPInfo, GetSiteAPPInfo, SaveQRCode, GetAPPUrl, UpdateWgtVersion, SaveSiteAPP`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/AuthAgent.cs`：符号 `AuthAgent, GetAuth, GetAuthSetting, SaveSiteAuthorization`；风险 `cache-consistency, funds, gateway, order-state, permission`。
- `Library/BetWin/Agent/Sites/AwardAgent.cs`：符号 `AwardAgent`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/CertAgent.cs`：符号 `CertAgent, GetCertInfo, SaveCertInfo, DeleteCertInfo`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/CommisionAgent.cs`：符号 `CommissionAgent, CreateCommissionLogNew, CreateCommissionLogNewSingle, CreateCommissionLog, AddCommissionLog, CheckCommissionLog, GetCommissionLogInfo, GetTeamCommission`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/ConfigAgent.cs`：符号 `ConfigAgent, GetBankList, GetVirtualCoinList, GetBlackRegion, SaveBlackRegion, CheckIPLimit, SaveBlackIP, GetBlackIP`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/DomainAgent.cs`：符号 `DomainAgent, SaveSiteDomain, GetSiteDomainInfo, DeleteDomain, GetSiteDomainList, GetSiteDomain, GetTopSiteDomain, GetAppSiteDomain`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/EventAgent.cs`：符号 `EventAgent, SaveColumn, GetColumnInfo, SaveColumnSort, DeleteColumn, GetColumnList, GetEventValidCount, GetEventValidTotal`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/FeedbackAgent.cs`：符号 `FeedbackAgent, AddFeedback, GetFeedbackInfo, DeleteFeedback`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/HelpAgent.cs`：符号 `HelpAgent, GetHelpColumnInfo, SaveHelpColumn, DeleteHelpColumn, UpdateHelpColumnSort, GetSiteHelp, SaveSiteHelp, DeleteSiteHelp`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/NewsAgent.cs`：符号 `NewsAgent, UpdateNewsColumnSort, SaveNewsColumn, SaveNewsColumnCode, GetNewsColumnList, GetNewsColumnInfo, DeleteNewsColumn, SaveNewsInfo`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/NotifyAgent.cs`：符号 `NotifyAgent, RefrsehAdminNotify, Push`；风险 `cache-consistency, callback, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/OnlineAgent.cs`：符号 `OnlineAgent, SaveUserOnline, GetSiteOnline`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/PageAgent.cs`：符号 `PageAgent, GetPageSetting, GetPageSettingInfo, SavePageItemInfo, GetSitePageSetting, SavePageItemSort, GetPageItemList, SaveCache`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/PaymentAgent.cs`：符号 `PaymentAgent, GetGatewaySetting, GetSystemPayment, GetCallbackIP, GoGateway, GetIP, ShowCallback, AccountCheck`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/PusherAgent.cs`：符号 `PusherAgent, GetSiteAdminChannel, GetSystemAdminChannel, GetUserChannel, GetUserName`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/RebateAgent.cs`：符号 `RebateAgent, SaveRebateSetting, GetRebateSettingInfo, UpdateRebateSettingIsAuto, UpdateRebateSettingEnabled, DeleteRebateSetting, GetRebateSettingList, GetRebateLog`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/RebateAgent.V2.cs`：符号 `RebateAgent, RebateV2UserPendingSnapshot, RebateV2CurrencyPending, RebateV2UserPrefetch, RebateV2CalcItem, RebateV2PreviewResult, RebateV2PreviewCurrencyItem, ResolveEsVisibleLag`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/RegionAgent.cs`：符号 `RegionAgent, GetSiteRegion, SaveSiteRegion, DeleteSiteRegion, GetSiteRegionContent, GetSiteRegionContentList, GetUrlTitle, GetNewsTitle`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteAgent.cs`：符号 `SiteAgent, GetSiteInfo, GetAllSiteList, GetSiteInfoAndCache, RemoveSiteInfoCache, SaveSiteMaintainDescription, GetSiteMaintainDescription, SaveSiteInfo`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteAPPAgent.cs`：符号 `SiteAPPAgent, UploadAPPFile, SaveAPPInfo, GetSiteAPPInfo, GetSiteAPPList, SaveQRCode, SaveCustomQRCode, GetAPPUrl`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteCurrencyAgent.cs`：符号 `SiteCurrencyAgent, Save, GetSiteCurrency, SaveCurrencySettingSort, SaveSiteCurrencys, GetCurrencys, GetSiteFristCurrency, SetSiteCurrency`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteFileAgent.cs`：符号 `SiteFileAgent, SaveReportInfo, SaveReportFile, UpdateStatus, DeleteFile`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteGameCateAgent.cs`：符号 `SiteGameCateAgent, GetGameCateList, SaveSiteGameCate, UpdateSiteGameCate, DelSiteGameCate, GetSiteGameCate, GetSysCate`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteGameCreditLogAgent.cs`：符号 `SiteGameCreditLogAgent, AddLog`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteGameListAgent.cs`：符号 `SiteGameListAgent, GetGameCateList, GetSiteGameLists, SaveSiteGameCate, UpdateSiteGameCate, DelSiteGameList, UpdateSiteGameList, UpdateSiteGameRecommends`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteHostAgent.cs`：符号 `SiteHostAgent, AddSiteHost, GetSiteHostInfo, GetSiteID, DeleteSiteHost, UpdatePaymentIsEnable, GetSiteHostList`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteLanguageAgent.cs`：符号 `SiteLanguageAgent, SaveSiteLanguage, GetSiteLanguage, GetSiteLangContent, DeleteSiteLanguage`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteSupportLanguageAgent.cs`：符号 `SiteSupportLanguageAgent, Save, GetSiteLanguage, GetSiteLanguages`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SiteVipConfigAgent.cs`：符号 `SiteVipConfigAgent, GetSiteVipConfigInfo, GetSiteVipConfigInfoAndCache, GetSiteVipLevelName, InitSiteVIPRightSetting, SaveSiteVipConfigRuleClause, SaveSiteVipConfigPointRule, SaveSiteVipConfigReturnRule`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SpecialOperationAgent.cs`：符号 `SpecialOperationAgent, GetOrderID, GetUserID, GetSubBankName, GetSubBankNameByUserID, GetSubBankCode, GetSubBankCodeByUserID, GetLevel`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/StoreAgent.cs`：符号 `StoreAgent, GetStoreInfo, GetStoreAdress, DeleteStoreInfo`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/SurveryAgent.cs`：符号 `SurveryAgent, SaveSurvery, GetSurvery, SaveAnswer, SavePay, GetSurveryUser`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/ValidCodeAgent.cs`：符号 `ValidCodeAgent, CheckMobileCode, CheckEmailCode, SendSMSCode, SendMailCode, AddValidCode, AddSMSLog`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/WithdrawAgent.cs`：符号 `WithdrawAgent, SaveChannel, SaveWithdrawSort, GetWithdrawCount, GetChannel, RemoveChannelInfo, GetChannelName, DeleteChannel`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/Sites/WithdrawSpecialOperationAgent.cs`：符号 `WithdrawSpecialOperationAgent, GetUserName, GetUserId, GetSubBankName, GetSubBankCode, GetMobile, GetIdCard, GetEmail`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/System/AdminAgent.cs`：符号 `AdminAgent, Login, Create, SaveAdminFace, UpdateAdminInfo, Logout, GetAdminInfo, GetAdminToken`；风险 `cache-consistency, funds, gateway, order-state, permission`。
- `Library/BetWin/Agent/System/BankAccountBlackListAgent.cs`：符号 `BankAccountBlackListAgent, SaveBankAccountBlackList, GetBankAccountBlackListInfo, IsExists`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/System/BillAgent.cs`：符号 `BillAgent, GetSystemBillInfo`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/System/ChartAgent.cs`：符号 `ChartAgent, GetChartInfo, GetReportData, CheckPermission, ResultHandle, ResultHandleStatistics, ExceQuery, DataColumnHandle`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/System/ConfigAgent.cs`：符号 `ConfigAgent, GetEnumValue, UpdateGameDictionaryCache, CreateMoneyTypeCategory, CreateMoneyTypeProfit, UpdateUserMoney, GetSystemConfig, SaveSystemConfig`；风险 `cache-consistency, funds, gateway, order-state`。
- `Library/BetWin/Agent/System/ExchangeAgent.cs`：符号 `ExchangeAgent, GetSystemExchange, UpdateExchengeMarketPrice, SaveExchenge`；风险 `cache-consistency, funds, gateway, order-state`。
- 还有 98 个文件仅写入 SQLite 控制面，未在 Markdown 摘要里展开。

## 跳过项

- 共跳过 9 个文件，原因：forbidden_directory。明细只写入 `rag/init-reports/20261018T064704Z-baseline-betwin.json`，不进入可召回摘要。

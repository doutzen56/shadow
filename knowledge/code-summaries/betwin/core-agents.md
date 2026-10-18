# 业务 Agent

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `betwin`
- summary_group: `core-agents`
- host_or_layer: `business-agent`
- solution_file: `BetWin5.0.sln`
- last_ingestion_run: `20261018T091023Z-project-betwin`
- file_count: 177
- project_count: 1
- risk_tags: `cache-consistency`, `callback`, `elasticsearch`, `funds`, `gateway`, `order-state`, `permission`, `user-data`

## 职责摘要

站点、用户、资金、游戏、代理、风控的业务 Agent 和定时作业实现，资金和状态规则主要在这里。

## 扫描根目录

- `Library/BetWin`

## 项目文件

- `Library/BetWin/BetWin.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `Library/BetWin/Agent/Game/GameAgent-User.cs`：符号 `GameAgent, GetGameUser, AddGameUser, GetGameUserList, CheckPlayerName, GetUserID, GetFlowRate, GetPlayerName`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Proxy/AgentUserAgent.cs`：符号 `AgentUserAgent, GetInfo, UpdateAgentUserInfo, UpdateAgentUserCommission, UpdateAgentUserReturn, UpdateToAgent, AddAgentUser`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Report/ReportAgent-User.cs`：符号 `ReportAgent, GetUserDateMoney, GetUserMoneyType, SaveUserBetAmount, SaveUserRechargeAmount, GetUserDayBetAmount, GetUserWeekBetAmount, GetUserMonthBetAmount`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Risk/UserDeviceAgent.cs`：符号 `UserDeviceAgent, SaveUserDevice`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Risk/UserIPAgent.cs`：符号 `UserIPAgent, SaveUserIP`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Sites/AdminAgent.cs`：符号 `AdminAgent, GetAdminInfo, GetAdminID, GetAdminName, GetRealName, Login, CheckGoogleSecret, Logout`；风险 `cache-consistency, funds, gateway, order-state, permission`
- `Library/BetWin/Agent/Sites/AuthAgent.cs`：符号 `AuthAgent, GetAuth, GetAuthSetting, SaveSiteAuthorization`；风险 `cache-consistency, funds, gateway, order-state, permission`
- `Library/BetWin/Agent/Sites/NotifyAgent.cs`：符号 `NotifyAgent, RefrsehAdminNotify, Push`；风险 `cache-consistency, callback, funds, gateway, order-state`
- `Library/BetWin/Agent/Sites/PaymentAgent.cs`：符号 `PaymentAgent, GetGatewaySetting, GetSystemPayment, GetCallbackIP, GoGateway, GetIP, ShowCallback, AccountCheck`；风险 `cache-consistency, funds, gateway, order-state`
- `Library/BetWin/Agent/Sites/WithdrawAgent.cs`：符号 `WithdrawAgent, SaveChannel, SaveWithdrawSort, GetWithdrawCount, GetChannel, RemoveChannelInfo, GetChannelName, DeleteChannel`；风险 `cache-consistency, funds, gateway, order-state`
- `Library/BetWin/Agent/Sites/WithdrawSpecialOperationAgent.cs`：符号 `WithdrawSpecialOperationAgent, GetUserName, GetUserId, GetSubBankName, GetSubBankCode, GetMobile, GetIdCard, GetEmail`；风险 `cache-consistency, funds, gateway, order-state`
- `Library/BetWin/Agent/System/AdminAgent.cs`：符号 `AdminAgent, Login, Create, SaveAdminFace, UpdateAdminInfo, Logout, GetAdminInfo, GetAdminToken`；风险 `cache-consistency, funds, gateway, order-state, permission`
- `Library/BetWin/Agent/System/PaymentConfigAgent.cs`：符号 `PaymentConfigAgent, GetSystemPayment, SaveSystemPayment, DeleteSystemPayment, CopySystemPayment, UpdatePaymentName, UpdatePaymentIsOpen, SavePaymentGroup`；风险 `cache-consistency, funds, gateway, order-state`
- `Library/BetWin/Agent/System/WithdrawConfigAgent.cs`：符号 `WithdrawConfigAgent, GetSystemWithdraw, SaveSystemWithdraw, DeleteSystemWithdraw, CopySystemWithdraw, UpdateWithdrawName, UpdateWithdrawIsOpen, SaveSystemWithdrawBank`；风险 `cache-consistency, funds, gateway, order-state`
- `Library/BetWin/Agent/Tickets/TicketAgent.cs`：符号 `TicketAgent, AddTicket, GetTicketInfo, AddComplement, AddFollow, SaveRank, SaveCategory, DeleteCategory`；风险 `cache-consistency, funds, gateway, order-state`
- `Library/BetWin/Agent/User/UserAgent-Exchange.cs`：符号 `UserAgent, GetCurrencyExchangeOrderInfo`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/User/UserAgent-Game.cs`：符号 `UserAgent, GetGameConfig`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/User/UserAgent-Group.cs`：符号 `UserAgent, SaveGroup, UpdateGroupDefault, GetGroupInfo, GetGroupNotID, UpdateGroupMember, UpdateGroupPayment, GetPayment`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/User/UserAgent-Import.cs`：符号 `UserAgent, SaveImportUser, GetImportUser, DeleteImportUser, SaveImportMobile, DeleteImportMobile, SaveImportBlackMobile, UpdateImportBlackMobile`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/User/UserAgent-Money.cs`：符号 `UserAgent, AddGameMoneyLog, AddRechargeOrderWithAccount, AddRechargeOrder, CreateRechargeOrder, GetGatewaySetting, CheckRechargeOrder, ErrorOrderAddEvent`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/User/UserAgent-Setting.cs`：符号 `UserAgent, GetUserWithdrawSetting`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/User/UserAgent.cs`：符号 `UserAgent, GetSiteID, GetUserName, GetAgentID, GetCurrency, GetAgentName, AddUser, AddAgent`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/AgentApplyAgent.cs`：符号 `AgentApplyAgent, AddAgentApply, GetApplyInfo, UpdateApply`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/LogAgent.cs`：符号 `LogAgent, AddLoginLog`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/MessageAgent.cs`：符号 `MessageAgent, SendMessage, SendMail, RefreshUnReadMessageQty, GetUnReadMessageQty, GetUserMessage, DeleteMessage, UpdateMessage`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/MoneyAgent.cs`：符号 `MoneyAgent, GetUserWallet, GetUserWallets, GetUserWalletByDb, GetUserMoney, CreateLogID, AddMoneyLog, GetUserTotalMoney`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/MoneyReviewAgent.cs`：符号 `MoneyReviewAgent, SaveMoneyReview, CheckMoneyReview`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserAgentUpdateLogAgent.cs`：符号 `UserAgentUpdateLogAgent, AddLog, UpdateLog, RunUpdateTask, UpdateAgentSts`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserEventAgent.cs`：符号 `UserEventAgent, SaveEventLog, GetEventLog, CheckEventLog, GetEventLogCount`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserEventDelegate.cs`：符号 `UserEventDelegate, SaveDelotteryProductCount, ExecDelotteryProdcut, GetDelotteryCount, GetDelotteryItemCount, SaveDelotteryRecharge, WriteCountLog, WriteRedEnvelopeCountLog`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserExternalAgent.cs`：符号 `UserExternalAgent, SendContext, SendInfoType, AddUserExternal, SendUserExternal, RequestReturnMesssage`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserGameFavoriteAgent.cs`：符号 `UserGameFavoriteAgent, SaveFavorite, CancelFavorite`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserIdCardAgent.cs`：符号 `UserIdCardAgent, UpdateStatus, DeleteCard, DeleteCardByUserId, CreateCard, SaveCardImg, GetIdCard, Notify`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserInfoAgent.cs`：符号 `UserInfoAgent, UpdateUserInfo, ResetUserInfo, UpdateUserStatus, UpdateUserLockStatus, UpdateAllUserLockStatus, UpdateUserLockStatusByProxy, IsLock`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserMarkAgent.cs`：符号 `MarkAgent, AddUserMark, GetMark, DeleteMark, DeleteMarkCache, UpdateUserMark`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserRelationAgent.cs`：符号 `UserRelationAgent, UpdateUserAgentAndTop`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserSignAgent.cs`：符号 `UserSignAgent, AddSign, AddSignAndOpen, sign, OpenLongCheckIn, GetLoginDay, IsSigned`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserVipConfigAgent.cs`：符号 `UserVipConfigAgent, GetTotalBetAmount, GetTotalRecharge, GetThreeMonthTotalBetAmount, GetVIPConditionItem, GetNextVIPConditionItem, GetPreVIPConditionItem, GetUpgradePercent`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/UserVipGiftLogAgent.cs`：符号 `UserVipGiftLogAgent, CreateSourceID, SaveSiteVipUserSourceID, IsExistSiteVipUserSourceID, GetLastUpgradeDateTime`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/VipConfigSettingAgent.cs`：符号 `VipConfigSettingAgent, TempUserList, GetPrivilegeItem, GetLevelItem, GetLevelItems, SaveLevelItem, SavePrivilegeItem, SavePrivilegeItemStatus`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/WashFlowAgent.cs`：符号 `WashFlowAgent, CreateWashFlow, SaveWashFlow, GetWashFlowList, UpdateWashFlow, AddWashFlowLog, AddWashFlowByID, AddGameWashFlowLog`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/WithdrawCreditAgent.cs`：符号 `WithdrawCreditAgent`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Agent/Users/WithdrawOrderAgent.cs`：符号 `WithdrawOrderAgent, GetOrderInfo, HasRisk, Check, Exec, WithDrawReward, Query, WithdrawCheckforService`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Jobs/AutoCheck/CheckWithdrawJob.cs`：符号 `CheckWithdrawJob, ExecuteJobAsync`；风险 `cache-consistency, funds, gateway, order-state`
- `Library/BetWin/Jobs/AutoCheck/TicketNotifyJob.cs`：符号 `TicketNotifyJob, ExecuteJobAsync`；风险 `cache-consistency, callback, funds, gateway, order-state`
- `Library/BetWin/Jobs/AutoCheck/UpdateUserAgentJob.cs`：符号 `UpdateUserAgentJob, UpdateUserAgentRunner, ExecuteJobAsync, Run`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Jobs/AutoCheck/UserMoneyCacheJob.cs`：符号 `UserMoneyCacheJob, UserMoneyCache, ExecuteDelayJobAsync, Run`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- `Library/BetWin/Jobs/CleanUp/AdminLogClearJob.cs`：符号 `AdminLogClearJob`；风险 `cache-consistency, funds, gateway, order-state, permission`
- `Library/BetWin/Jobs/Rebate/RebateOrderV2SyncJob.cs`：符号 `RebateOrderV2SyncJob, ExecuteJobAsync`；风险 `cache-consistency, funds, gateway, order-state`
- `Library/BetWin/Jobs/ReportStatistics/UpdateUserMoneyJob.cs`：符号 `UpdateUserMoneyJob, ExecuteJobAsync`；风险 `cache-consistency, funds, gateway, order-state, user-data`
- 还有 127 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 1 项、sensitive_file 2 项；明细只写入 `rag/init-reports/20261018T091023Z-project-betwin.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

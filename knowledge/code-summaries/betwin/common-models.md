# 公共模型

- project_id: `betwin`
- host_or_layer: `domain-model`
- roots: `Library/BetWin.Common`
- file_count: 427
- generated_at: 2026-10-18T06:47:04Z

## 风险标签

`cache-consistency`, `callback`, `database-schema`, `elasticsearch`, `funds`, `gateway`, `login`, `mq`, `order-state`, `permission`, `user-data`

## 代表文件

- `Library/BetWin.Common/Agents/Agent.cs`：符号 `Agent`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/AgentCommission.cs`：符号 `AgentCommission`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/AgentContract.cs`：符号 `AgentContract`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/AgentReturn.cs`：符号 `AgentReturn`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/AgentReturnLog.cs`：符号 `AgentReturnLog`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/AgentReturnSetting.cs`：符号 `AgentReturn, AgentReturnSetting, RankItem, GameItem, RankItemShow, IsMatch`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/AgentUser.desinger.cs`：符号 `AgentUser`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/parts/Agent-enum.cs`：符号 `Agent, AgentStatus`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/parts/AgentCommission-Setting.cs`：符号 `AgentCommission`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/parts/AgentReturnCollect.cs`：符号 `AgentReturnCollect, GameKeyConverter`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/parts/AgentReturnLog-enum.cs`：符号 `AgentReturnLog, LogStatus, DetailView`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/parts/AgentUser-enum.cs`：符号 `AgentUser, AgentStatus, PermissionFlag`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/parts/CommissionSetting.cs`：符号 `CommissionSetting, ToJson, GetCommission, GetMaxRate, ToXElement`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/parts/WashRatio.cs`：符号 `WashRatio`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Agents/ProxyRate.cs`：符号 `ProxyRate`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/app.config`：符号 `未提取到公开符号`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Attributes/CheckStatus.cs`：符号 `CheckStatus`；风险 `database-schema, elasticsearch, order-state, user-data`。
- `Library/BetWin.Common/Attributes/DirectionType.cs`：符号 `DirectionType`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Attributes/OpenStatus.cs`：符号 `OpenStatus`；风险 `database-schema, elasticsearch, order-state, user-data`。
- `Library/BetWin.Common/Attributes/SportOddsType.cs`：符号 `SportOddsType`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/BetWin.Common.csproj`：符号 `未提取到公开符号`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Config.cs`：符号 `Config, GetAPIName`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext-Agent.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext-Game.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext-Report.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext-Risk.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext-Shop.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext-Site.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext-System.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext-Ticket.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, order-state, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext-User.cs`：符号 `BetDataContext, Refresh`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/DataContext/BetDataContext.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/ElasticSearch/EsHelper.cs`：符号 `EsHelper, InconsistentRangeResult, InitAllIndex, EnsureLastDownloadTimePipelineAsync, GetIndexSuffix, TryDecimal, TryInt, GetEsID`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/ElasticSearch/EsIndexManager.cs`：符号 `EsIndexManager, IsMonthlyManaged, RunMonthlyMaintenanceAsync, EnsureTypeReadyAsync, GetRetentionMonths, RunMonthlyRolloverAsync, EnsureIlmPolicyAsync, EnsureIndexTemplateAsync`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/ElasticSearch/EsQueryResult.cs`：符号 `EsQueryAgg, EsQueryByGroupAgg, EsQueryReuslt, EsQueryResultByAgg, EsQueryResultByGroupAgg`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/ElasticSearch/ExpressionExtension.cs`：符号 `ExpressionExtension`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/ErrorType.cs`：符号 `ErrorType`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Excel/BattleLogExcel.cs`：符号 `BattleLogExcel`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Excel/GameDataLogExcel.cs`：符号 `GameDataLogExcel`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Excel/GameRecordExcel.cs`：符号 `GameRecordExcel`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Excel/MoneyLogExcel.cs`：符号 `MoneyLogExcel`；风险 `database-schema, elasticsearch, funds, user-data`。
- `Library/BetWin.Common/Excel/RechargeChannelExcel.cs`：符号 `RechargeChannelExcel, RechargeChannel`；风险 `database-schema, elasticsearch, funds, user-data`。
- `Library/BetWin.Common/Excel/RewardLogExcel.cs`：符号 `RewardLogExcel`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Excel/TeamExcel.cs`：符号 `TeamExcel, TeamRptExcel`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Excel/UserStatExcel.cs`：符号 `UserStatExcel, UserStat`；风险 `database-schema, elasticsearch, user-data`。
- `Library/BetWin.Common/Games/AnimalLog.desinger.cs`：符号 `AnimalLog`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/BattleLog.desinger.cs`：符号 `BattleLog`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/ChessLog.desinger.cs`：符号 `ChessLog`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/EsModel.cs`：符号 `EsModel, EsModelLog, EsModelBase`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/ESportLog.desinger.cs`：符号 `ESportLog`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/FishLog.desinger.cs`：符号 `FishLog`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameCategory.desinger.cs`：符号 `GameEntrance`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameDictionary.cs`：符号 `GameDictionary`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameEntrance.desinger.cs`：符号 `GameEntrance`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameEntranceCategory.desinger.cs`：符号 `GameEntranceCategory`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameEntranceTag.desinger.cs`：符号 `GameEntranceTag`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameFlowRate.desinger.cs`：符号 `GameFlowRate`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameKey.cs`：符号 `未提取到公开符号`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameLog.cs`：符号 `未提取到公开符号`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameLog.desinger.cs`：符号 `GameLog`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameLogRawDataV2.cs`：符号 `GameLogRawDataV2`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameLogStatistic.designer.cs`：符号 `GameLogStatistic`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameLogV2.cs`：符号 `GameLogV2`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameMark.desinger.cs`：符号 `GameMark`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameSetting.desinger.cs`：符号 `GameSetting`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameSubCategory.cs`：符号 `GameSubCategory`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameSubCategoryEnum.cs`：符号 `GameSubCategoryEnumAttribute, GameSubCategoryEnum`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameTag.desinger.cs`：符号 `GameTag`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameTransferOrder.desinger.cs`：符号 `GameTransferOrder`；风险 `database-schema, elasticsearch, gateway, order-state, user-data`。
- `Library/BetWin.Common/Games/GameType.cs`：符号 `GameType, GameCategory, GameTypeAttribute`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameUETimeZone.cs`：符号 `GameUETimeZone`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/GameUser.desinger.cs`：符号 `GameUser`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/LiveLog.cs`：符号 `LiveLog`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/LotteryLog.desinger.cs`：符号 `LotteryLog`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/MiniLog.desinger.cs`：符号 `MiniGameLog`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/part/Animal-part.cs`：符号 `AnimalLog, LogStatus`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/part/ChessLog-part.cs`：符号 `ChessLog, LogStatus`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/part/ESportLog-enum.cs`：符号 `ESportLog, LogStatus, IsFinish`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/part/FantasyLog-enum.cs`：符号 `BattleLog, LogStatus`；风险 `database-schema, elasticsearch, gateway, user-data`。
- `Library/BetWin.Common/Games/part/FantasyLog-sum.cs`：符号 `AgentFantasySum`；风险 `database-schema, elasticsearch, gateway, user-data`。
- 还有 347 个文件仅写入 SQLite 控制面，未在 Markdown 摘要里展开。

## 跳过项

- `Library/BetWin.Common/Logs/HandlerLog.cs`：forbidden_directory
- `Library/BetWin.Common/Logs/LogGameAPI.desinger.cs`：forbidden_directory
- `Library/BetWin.Common/Logs/LogHelper.cs`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/.NETFramework,Version=v4.8.AssemblyAttributes.cs`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/BetWin.C.2BCD3591.Up2Date`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/BetWin.Common.csproj.AssemblyReference.cache`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/BetWin.Common.csproj.CoreCompileInputs.cache`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/BetWin.Common.csproj.FileListAbsolute.txt`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/BetWin.Common.csproj.GenerateResource.cache`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/BetWin.Common.dll`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/BetWin.Common.pdb`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/BW.Common.Resources.Res.resources`：forbidden_directory
- `Library/BetWin.Common/obj/Debug/DesignTimeResolveAssemblyReferencesInput.cache`：forbidden_directory

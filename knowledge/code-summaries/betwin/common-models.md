# 公共模型

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `betwin`
- summary_group: `common-models`
- host_or_layer: `domain-model`
- solution_file: `BetWin5.0.sln`
- last_ingestion_run: `20261018T091023Z-project-betwin`
- file_count: 428
- project_count: 1
- risk_tags: `cache-consistency`, `callback`, `database-schema`, `elasticsearch`, `funds`, `gateway`, `login`, `mq`, `order-state`, `permission`, `user-data`

## 职责摘要

实体、枚举、报表模型和 Elasticsearch 索引注册，读写作业和查询共用。

## 扫描根目录

- `Library/BetWin.Common`

## 项目文件

- `Library/BetWin.Common/BetWin.Common.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `Library/BetWin.Common/Agents/AgentUser.desinger.cs`：符号 `AgentUser`；风险 `database-schema, elasticsearch, user-data`
- `Library/BetWin.Common/Agents/parts/AgentUser-enum.cs`：符号 `AgentUser, AgentStatus, PermissionFlag`；风险 `database-schema, elasticsearch, user-data`
- `Library/BetWin.Common/Attributes/CheckStatus.cs`：符号 `CheckStatus`；风险 `database-schema, elasticsearch, order-state, user-data`
- `Library/BetWin.Common/Attributes/OpenStatus.cs`：符号 `OpenStatus`；风险 `database-schema, elasticsearch, order-state, user-data`
- `Library/BetWin.Common/DataContext/BetDataContext-Ticket.cs`：符号 `BetDataContext`；风险 `database-schema, elasticsearch, order-state, user-data`
- `Library/BetWin.Common/DataContext/BetDataContext-User.cs`：符号 `BetDataContext, Refresh`；风险 `database-schema, elasticsearch, user-data`
- `Library/BetWin.Common/Excel/MoneyLogExcel.cs`：符号 `MoneyLogExcel`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Excel/RechargeChannelExcel.cs`：符号 `RechargeChannelExcel, RechargeChannel`；风险 `database-schema, elasticsearch, funds, gateway, user-data`
- `Library/BetWin.Common/Excel/UserStatExcel.cs`：符号 `UserStatExcel, UserStat`；风险 `database-schema, elasticsearch, user-data`
- `Library/BetWin.Common/Games/GameTransferOrder.desinger.cs`：符号 `GameTransferOrder`；风险 `database-schema, elasticsearch, order-state, user-data`
- `Library/BetWin.Common/Games/GameUser.desinger.cs`：符号 `GameUser`；风险 `database-schema, elasticsearch, user-data`
- `Library/BetWin.Common/Games/RebateOrderV2.cs`：符号 `RebateOrderV2`；风险 `database-schema, elasticsearch, order-state, user-data`
- `Library/BetWin.Common/Games/SportLogStatus.desinger.cs`：符号 `SportLogStatus`；风险 `database-schema, elasticsearch, order-state, user-data`
- `Library/BetWin.Common/Games/UserDateGame.cs`：符号 `UserDateGameLog, GameRate`；风险 `database-schema, elasticsearch, user-data`
- `Library/BetWin.Common/Games/part/GameTransferOrder-enum.cs`：符号 `GameTransferOrder`；风险 `database-schema, elasticsearch, order-state, user-data`
- `Library/BetWin.Common/Games/part/GameUser-part.cs`：符号 `GameUser, GameUserLock`；风险 `database-schema, elasticsearch, user-data`
- `Library/BetWin.Common/Games/part/LiveLog-status.cs`：符号 `LiveLog, LogStatus`；风险 `database-schema, elasticsearch, order-state, user-data`
- `Library/BetWin.Common/Permission/Menu.cs`：符号 `Menu, ToString`；风险 `database-schema, elasticsearch, permission, user-data`
- `Library/BetWin.Common/Permission/Param.cs`：符号 `ParamAttribute`；风险 `database-schema, elasticsearch, permission, user-data`
- `Library/BetWin.Common/Reports/UserDateMoney.desinger.cs`：符号 `UserDateMoney`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Reports/UserDateMoneyCategory.desinger.cs`：符号 `UserDateMoneyCategory`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Reports/UserDateMoneyProfit.views.cs`：符号 `UserDateMoneyProfit`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Sites/AdminNotify.cs`：符号 `AdminNotify`；风险 `callback, database-schema, elasticsearch, permission, user-data`
- `Library/BetWin.Common/Sites/SitePaymentCallbackLog.cs`：符号 `SitePaymentCallbackLog`；风险 `callback, database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Sites/SiteWithdrawCallbackLog.desinger.cs`：符号 `SiteWithdrawCallbackLog`；风险 `callback, database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Systems/SystemAdminToken.cs`：符号 `SystemAdminToken`；风险 `database-schema, elasticsearch, login, permission, user-data`
- `Library/BetWin.Common/Users/BankRechargeOrder.desinger.cs`：符号 `BankRechargeOrder`；风险 `database-schema, elasticsearch, funds, order-state, user-data`
- `Library/BetWin.Common/Users/Events/FirstRechargeModel.cs`：符号 `FirstRechargeModel, FirstRechargeType, FirstRechargeItem, FirstRechargeRuleItem, ToString`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/GameMoneyLog.desinger.cs`：符号 `GameMoneyLog`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/MoneyLock.desinger.cs`：符号 `MoneyLock`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/MoneyLog.cs`：符号 `MoneyLog`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/MoneyReview.desinger.cs`：符号 `MoneyReview`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/RechargeOrder.desinger.cs`：符号 `RechargeOrder`；风险 `database-schema, elasticsearch, funds, order-state, user-data`
- `Library/BetWin.Common/Users/UserAuthorization.cs`：符号 `UserAuthorization`；风险 `database-schema, elasticsearch, permission, user-data`
- `Library/BetWin.Common/Users/UserFund.desinger.cs`：符号 `UserFund`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/UserLoginLog.cs`：符号 `LoginLog`；风险 `database-schema, elasticsearch, login, user-data`
- `Library/BetWin.Common/Users/UserMoney.desinger.cs`：符号 `UserMoney`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/UserToken.desinger.cs`：符号 `UserToken`；风险 `database-schema, elasticsearch, login, user-data`
- `Library/BetWin.Common/Users/UserWithdrawInfo.cs`：符号 `UserWithdrawInfo, ToString, GetFee`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/Withdraw/WithdrawOrder-enum.cs`：符号 `WithdrawOrder, WithdrawStatus, ToString, GetBankInfo`；风险 `database-schema, elasticsearch, funds, order-state, user-data`
- `Library/BetWin.Common/Users/Withdraw/WithdrawOrder.desinger.cs`：符号 `WithdrawOrder`；风险 `database-schema, elasticsearch, funds, order-state, user-data`
- `Library/BetWin.Common/Users/Withdraw/WitndrawOrderLog.cs`：符号 `WithdrawOrderLog`；风险 `database-schema, elasticsearch, funds, order-state, user-data`
- `Library/BetWin.Common/Users/WithdrawCreditLog.cs`：符号 `WithdrawCreditLog`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/part/GameMoneyLog-enum.cs`：符号 `GameMoneyLog, GameMoneyType`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/part/MoneyLock-enum.cs`：符号 `MoneyLock, LockType`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/part/MoneyLog-enum.cs`：符号 `MoneyLog, MoneyType, MoneyType1002, CategoryType, ActionType, MoneyTypeAttribute`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/part/MoneyReview-enum.cs`：符号 `MoneyReview, ReviewStatus, ActionType`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/part/UserFund-part.cs`：符号 `UserFund`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/part/UserMoney-part.cs`：符号 `UserMoney, FillHashEntry, ToHashEntry`；风险 `database-schema, elasticsearch, funds, user-data`
- `Library/BetWin.Common/Users/part/UserToken-enum.cs`：符号 `User, TokenType`；风险 `database-schema, elasticsearch, login, user-data`
- 还有 378 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 1 项、sensitive_file 2 项；明细只写入 `rag/init-reports/20261018T091023Z-project-betwin.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

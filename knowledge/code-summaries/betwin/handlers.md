# 请求 Handler

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `betwin`
- summary_group: `handlers`
- host_or_layer: `http-handler`
- solution_file: `BetWin5.0.sln`
- last_ingestion_run: `20261026T051855Z-project-betwin`
- file_count: 292
- project_count: 5
- risk_tags: `cache-consistency`, `callback`, `funds`, `login`, `order-state`, `permission`, `user-data`

## 职责摘要

请求分发基座与四个请求面：会员与代理前台、站点管理端、平台系统端、游戏回调与运维接口。

## 扫描根目录

- `Library/BetWin.Handler`
- `Library/BetWin.Handler.Request`
- `Library/BetWin.Handler.Admin`
- `Library/BetWin.Handler.System`
- `Library/Handler/BetWin.Handler.Api`

## 项目文件

- `Library/BetWin.Handler/BetWin.Handler.csproj`
- `Library/BetWin.Handler.Request/BetWin.Handler.Request.csproj`
- `Library/BetWin.Handler.Admin/BetWin.Handler.Admin.csproj`
- `Library/BetWin.Handler.System/BetWin.Handler.System.csproj`
- `Library/Handler/BetWin.Handler.Api/BetWin.Handler.Api.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `Library/BetWin.Handler.Admin/ResultHandler.cs`：符号 `ResultHandler, GetAssembly`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/account.cs`：符号 `account, login, systemlogin, logout`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/award/delottery.cs`：符号 `delottery, getproductcount, updateproductcount`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/award/delotterylog.cs`：符号 `delotterylog, pointlog`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/award/events.cs`：符号 `events, columninfo, columnlist, columnsave, savecolumnsort, columndelete, info, initplatform`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/award/log.cs`：符号 `log, list, info, savebet, savelotterycount, saveredenvelopecount, save, agree`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/award/redenvelope.cs`：符号 `redenvelope, getproductcount, updateproductcount`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/award/redenvelopelog.cs`：符号 `redenvelopelog, pointlog`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/award/survery.cs`：符号 `survery, list, savesurvery, surveryinfo, saveanswer, pay, log, answerinfo`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/chart.cs`：符号 `chart, rechargeandwitdrawtotal, profitpayoff, productcollect, userstatistics`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/config.cs`：符号 `config, moneytype`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/exchange/rate.cs`：符号 `rate, list, getcurrency, getcurrencyinfo, savebasecurrency, GetSysExchange, getbasecurrency, getmode`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/exchange/review.cs`：符号 `review, log, list, check, summary`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/game/user.cs`：符号 `user, userlist, updatelock, updatetest, updateLimit, getbalance, register, GetUserBalanceList`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/money/commission.cs`：符号 `commission, create, loglist, logagree, logreject, logdetail`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/money/moneyreview.cs`：符号 `moneyreview, list, check, batchsuccess, batchfail`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/money/operate.cs`：符号 `operate, addmoney, addmoneybatch, addmoneybatchbyexcel, submoneybatchbyexcel, redeductmoney, filtergame, initplatform`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/money/rebate.cs`：符号 `rebate, BlockWhenAutoReturnV2Enabled, create, grouplist, execute, executev2, executeagent, updateenabled`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/money/recharge.cs`：符号 `recharge, checklist, checkinfo, checkagree, checkreject, checkprocessing, create, settinglist`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/money/rescue.cs`：符号 `rescue, loglist, query, post`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/money/returnplan.cs`：符号 `returnplan, execute, loglist, logagree, logreject`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/money/washflow.cs`：符号 `washflow, list, log, updatewashflow, updatewashflowsingle`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/money/withdraw.cs`：符号 `withdraw, orderlist, dealString, exportwithdraworder, orderinfo, checkorder, execorder, openchannelist`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/payment.cs`：符号 `payment, getpaymentsetting, getsyspaymentgroup`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/recharge.cs`：符号 `recharge, orderlist, orderlistfail, exportrechargeorder, orderinfo, checkorder, deleteorder, settinglist`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/report/money.cs`：符号 `money, log, exportlog, locked`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/report/user.cs`：符号 `user, loginlog, signinlog, signinlogreport, pointlog, addpointlog, subpointlog, addsignlog`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/risk/user.cs`：符号 `user, list, ip, info, ipdifuser, userdifip, login, amount`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/shop/order.cs`：符号 `order, info, update, list`；风险 `callback, funds, login, order-state, permission, user-data`
- `Library/BetWin.Handler.Admin/site/notify.cs`：符号 `notify, get, update, withdraw, events`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/site/ticket.cs`：符号 `ticket, list, addnew, category, info, addcomplement, saverank`；风险 `callback, funds, login, order-state, permission, user-data`
- `Library/BetWin.Handler.Admin/user/agent/commission.cs`：符号 `commission, list, list0, save, info, commissionlist, allagentcommissionlist, delete`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/agent/domain.cs`：符号 `domain, list, save, delete, info, background`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/agent/grade.cs`：符号 `grade, list, info, check, save, add`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/agent/info.cs`：符号 `info, add, agentlist, permission, applylist, execapply, updatecommission, updatereturn`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/agent/invite.cs`：符号 `invite, list, save, delete, info, updateinviteopen`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/agent/returnplan.cs`：符号 `returnplan, list, list0, info, save, delete, loglist, logdetail`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/agent/usragent.cs`：符号 `usragent, list, info, check, save, saveblockgame, getblockgame`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/customer/feedback.cs`：符号 `feedback, list, delete`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/customer/mail.cs`：符号 `mail, list, send, delete, info, update`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/customer/message.cs`：符号 `message, list, send, delete, info, update`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/customer/mobile.cs`：符号 `mobile, list, getusercondition, send`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/customer/sms.cs`：符号 `sms, list`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/member/bank.cs`：符号 `bank, list, info, updateinfo, delete, reset, verifyuser, createbank`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/member/group.cs`：符号 `group, list, delete, save, batchedit, updatemember, info, updategroupisdefault`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/member/idcard.cs`：符号 `idcard, list, info, updateinfo, delete, reset, verifyuser, createidcard`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/member/info.cs`：符号 `info, getusercondition, orderUsername, query, ArgIsEmpty, LimitUserSearch, UserSearch, list`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/user/member/rebate.cs`：符号 `rebate, info, returntype, list, save, updateauto, delete`；风险 `callback, funds, login, permission, user-data`
- `Library/BetWin.Handler.Admin/voucher/order.cs`：符号 `order, info, update, list, detail`；风险 `callback, funds, login, order-state, permission, user-data`
- `Library/BetWin.Handler.Admin/withdraw.cs`：符号 `withdraw, settinglist, getsettinginfo, getsystemsetting, getsystemwithdrawlist, savesetting, savewithdrawsort, deletewithdraw`；风险 `callback, funds, login, permission, user-data`
- 还有 242 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 5 项、sensitive_file 13 项；明细只写入 `rag/init-reports/20261026T051855Z-project-betwin.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

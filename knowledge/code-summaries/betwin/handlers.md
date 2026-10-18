# 请求 Handler

- project_id: `betwin`
- host_or_layer: `http-handler`
- roots: `Library/BetWin.Handler.Request`, `Library/BetWin.Handler.Admin`, `Library/BetWin.Handler.System`, `Library/Handler/BetWin.Handler.Api`
- file_count: 285
- generated_at: 2026-10-18T06:47:04Z

## 风险标签

`cache-consistency`, `callback`, `funds`, `gateway`, `login`, `order-state`, `permission`, `user-data`

## 代表文件

- `Library/BetWin.Handler.Request/agent/bill.cs`：符号 `bill, recharge, profit`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/agent/chart.cs`：符号 `chart, finace, finacesingle, gamedetail, gamerate, commissionlog, commissionlogdetail, inviteurl`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/agent/game.cs`：符号 `game, reward, rewardinfo, reward2excel, list, list2excel`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/agent/invite.cs`：符号 `invite, list, delete, save, open, joinsetting, join`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/agent/money.cs`：符号 `money, transfer`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/agent/report.cs`：符号 `report, livelog, esportlog, sportlog, chesslog, chessinfo, lotterylog, lotteryinfo`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/agent/team.cs`：符号 `team, membercount, moneytotal, userlist, list2excel, list, moremoney, GetUserBalanceList`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/agent/user.cs`：符号 `user, list, userlist, agentlist, info, getuserid, add, updatenotes`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/admin.cs`：符号 `admin, token`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/app.cs`：符号 `app`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/auth.cs`：符号 `auth, login, gettype`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/cmd.cs`：符号 `cmd, token`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/data.cs`：符号 `data, GetAPIKey, ShowError, AccessLimit, userlist, banklist, withdrawlist, rechargelist`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/ebet.cs`：符号 `ebet, token`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/ivi.cs`：符号 `ivi, GetAPIKey, ShowError, token, withdraw, approve, customer, CheckCallbackIP`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/mail.cs`：符号 `mail, register, sendcode, code`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/payment.cs`：符号 `payment, bank, usdt, wechat, alipay, qrcode, savebankorder`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/sa.cs`：符号 `sa, token, ReturnData`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/sms.cs`：符号 `sms, register, code`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/sports.cs`：符号 `sports, yabo`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/TFGaming.cs`：符号 `tfgaming, token`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/api/user.cs`：符号 `user, GetAPIKey, register, token, login, gameresult, money`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/BetWin.Handler.Request.csproj`：符号 `未提取到公开符号`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/config.cs`：符号 `config, register, withdraw, banklist, enumtype, ping, header, login`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/content/events.cs`：符号 `events, column, list, getStats, info, recharge, nonopen, open`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/content/help.cs`：符号 `help, footercolumn, column, info`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/content/news.cs`：符号 `news, list, column, info, tip`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/content/notice.cs`：符号 `notice, list, info`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/content/region.cs`：符号 `region, get`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/binddata.cs`：符号 `binddata, info, getpoint, open, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/competition.cs`：符号 `competition, info, infodetal, myrank, open, allrank, allrankhistory, test`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/dailytasks.cs`：符号 `dailytasks, info, getpoint, open, opensingle, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/delottery.cs`：符号 `delottery, info, getcount, bet, winner`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/firstrecharge.cs`：符号 `firstrecharge, info, getpoint, open, getOrderid, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/fullweek.cs`：符号 `fullweek, info, getpoint, open, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/invitetask.cs`：符号 `invitetask, info, getpoint, getLastSourceID, getLastValue, detail, inviterecord, open`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/longcheckin.cs`：符号 `longcheckin, info, getday, open, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/newdailytasks.cs`：符号 `newdailytasks, info, getpoint, open, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/newweektasks.cs`：符号 `newweektasks, info, getpoint, open, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/order.cs`：符号 `order, list, types, status`；风险 `callback, funds, login, order-state, permission, user-data`。
- `Library/BetWin.Handler.Request/events/redenvelope.cs`：符号 `redenvelope, info, getpoint, getredpoint, open, openred, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/rescue.cs`：符号 `rescue, info, getpoint, open, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/survery.cs`：符号 `survery, list, surveryinfo, saveanswer`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/washing.cs`：符号 `washing, info, getpoint, open, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/events/weektasks.cs`：符号 `weektasks, info, getpoint, open, record`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/external.cs`：符号 `external, updatepushid`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/game/api.cs`：符号 `api, login, login2, register, transfer, transfertocenter, transferalltogame, getbalance`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/game/info.cs`：符号 `info, gamelist, maintain, gameuser, account, gametype, gamecategory, entrance`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/game/log.cs`：符号 `log, esportv1, esport, esportdetail, esportinfov1, esportinfo, lotteryv1, lottery`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/game/order.cs`：符号 `order, transferorderlist`；风险 `callback, funds, gateway, login, order-state, permission, user-data`。
- `Library/BetWin.Handler.Request/game/rebate.cs`：符号 `rebate, info, returntype, list`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/game/recommend.cs`：符号 `recommend, list, news, favorites, saveFavorite`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/game/remote/avia.cs`：符号 `avia, result, category, matchinfo, match`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/game/setting.cs`：符号 `setting, gamelist`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/game/slot.cs`：符号 `slot, list`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/game/subassort.cs`：符号 `subassort, list, SiteGameSubAssort, SiteGameList`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/member/forget.cs`：符号 `forget, info, mobile, email, question`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/member/message.cs`：符号 `message, list, unreadmsgqty, info, read, delete`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/member/report.cs`：符号 `report, profit`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/member/signin.cs`：符号 `signin, list, signresult`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/member/vip.cs`：符号 `vip, getlevels, GetUpgradeLogCondition, upgradelog, exportupgrade`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/money.cs`：符号 `money, moneylog, gamemoneylog, gettype, moneytype, locked, rechargeorderlist, rechargeorderinfo`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/packages.config`：符号 `未提取到公开符号`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/payment.cs`：符号 `payment, recharge, getsettings, rechargelist, getValue, withdraw, specialaccessories, rechargenumber`；风险 `callback, funds, gateway, login, permission, user-data`。
- `Library/BetWin.Handler.Request/Properties/AssemblyInfo.cs`：符号 `未提取到公开符号`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/proxy.cs`：符号 `proxy, login, GetTimeSpan, logout, register, info, islogin, channel`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/report.cs`：符号 `report, getuserdatemoney, getusermoneytotal, getusermoneycategorytotal, getusermoneyprofit, agentindexgamewin, agentindexcommission, getfundinfo`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/Resources/Res.Designer.cs`：符号 `Res`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/Resources/web.config`：符号 `未提取到公开符号`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/ResultHandler.cs`：符号 `ResultHandler`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/shop/product.cs`：符号 `product, list, catedict, order`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/site/app.cs`：符号 `app, update, install, apk, savecount`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/site/domain.cs`：符号 `domain, info, domaininfo, list, nodelist, seo`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/site/info.cs`：符号 `info, get, getip, getagentip, getadress, getareacode, logo, app`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/site/page.cs`：符号 `page, info`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/site/region.cs`：符号 `region, content, list`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/site/style.cs`：符号 `style, mobile`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/site/vip.cs`：符号 `vip, index, pointrule`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/SlideCaptcha/Captcha.cs`：符号 `Captcha, GenerateBase64, Generate, GetGraphics, GetSliderPath, CaptureSlider, CaptureImage, RandomNext`；风险 `callback, funds, login, permission, user-data`。
- `Library/BetWin.Handler.Request/SlideCaptcha/CaptchaModel.cs`：符号 `CaptchaModel, Captcha64Model`；风险 `callback, funds, login, permission, user-data`。
- 还有 205 个文件仅写入 SQLite 控制面，未在 Markdown 摘要里展开。

## 跳过项

- 共跳过 44 个文件，原因：forbidden_directory, forbidden_sensitive_suffix。明细只写入 `rag/init-reports/20261018T064704Z-baseline-betwin.json`，不进入可召回摘要。

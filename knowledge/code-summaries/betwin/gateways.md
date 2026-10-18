# 第三方网关

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `betwin`
- summary_group: `gateways`
- host_or_layer: `external-gateway`
- solution_file: `BetWin5.0.sln`
- last_ingestion_run: `20261018T091023Z-project-betwin`
- file_count: 524
- project_count: 5
- risk_tags: `callback`, `funds`, `gateway`, `idempotency`, `login`, `mq`, `order-state`, `permission`, `user-data`

## 职责摘要

第三方游戏、代收、代付插件；各供应商的签名、状态码、金额精度和回调时机都不一样。

## 扫描根目录

- `Library/GateWay/BetWin.Gateway.Game`
- `Library/GateWay/BetWin.Gateway.Payment`
- `Library/GateWay/BetWin.Gateway.Withdraw`
- `Library/GateWay/BetWin.Gateway.Payment.Special`
- `Library/GateWay/BetWin.Gateway.Withdraw.Special`

## 项目文件

- `Library/GateWay/BetWin.Gateway.Game/BetWin.Gateway.Game.csproj`
- `Library/GateWay/BetWin.Gateway.Payment/BetWin.Gateway.Payment.csproj`
- `Library/GateWay/BetWin.Gateway.Withdraw/BetWin.Gateway.Withdraw.csproj`
- `Library/GateWay/BetWin.Gateway.Payment.Special/BetWin.Gateway.Payment.Special.csproj`
- `Library/GateWay/BetWin.Gateway.Withdraw.Special/BetWin.Gateway.Withdraw.Special.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `Library/GateWay/BetWin.Gateway.Game/Model/JBD/UserLogin.cs`：符号 `UserLogin, UserLoginResult`；风险 `callback, funds, gateway, idempotency, login, user-data`
- `Library/GateWay/BetWin.Gateway.Game/Model/TCG/Entities/GetLotterySettledOrderByMember.cs`：符号 `GetLotterySettledOrderByMember, GetLotterySettledOrderByMemberResult`；风险 `callback, funds, gateway, idempotency, order-state, user-data`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/CallbackFactory.cs`：符号 `CallbackFactory, GetCallback`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/CheckType.cs`：符号 `CheckType`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/HMAC.cs`：符号 `HMAC, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/HMACSHA1.cs`：符号 `HMACSHA1, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/HMACSHA256.cs`：符号 `HMACSHA256, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/HMACSHA256Base64.cs`：符号 `HMACSHA256Base64, Check, EncodeBase64Url`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/HMACSHA256LASTKEY.cs`：符号 `HMACSHA256LASTKEY, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/ICallback.cs`：符号 `ICallback, ToSignString, GetSign, CheckCode, Check, GetMoney`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/MD5.cs`：符号 `MD5, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/MD5SHA1.cs`：符号 `MD5SHA1, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/None.cs`：符号 `None, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/PHPSHA1.cs`：符号 `PHPSHA1, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/QianGuiMd5.cs`：符号 `QianGuiMd5, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/RAS.cs`：符号 `RAS, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/RSA.cs`：符号 `RSA, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/RSAMD5.cs`：符号 `RSAMD5, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/RSASHA256.cs`：符号 `RSASHA256, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/SHA256MD5.cs`：符号 `SHA256MD5, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/SHA256Standard.cs`：符号 `SHA256Standard, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/CallBackSign/UrlSHA256.cs`：符号 `UrlSHA256, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/Model/CallbackResult.cs`：符号 `CallbackResult, ToString`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Payment/Model/CallbackSetting.cs`：符号 `CallbackSetting`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackCheckSign/CallbackCheckFactory.cs`：符号 `CallbackCheckFactory, GetCallback`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackCheckSign/CheckCheckType.cs`：符号 `CheckCheckType`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackCheckSign/HMAC.cs`：符号 `HMAC, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackCheckSign/HMACSHA1.cs`：符号 `HMACSHA1, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackCheckSign/HMACSHA256.cs`：符号 `HMACSHA256, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackCheckSign/ICallbackCheck.cs`：符号 `ICallbackCheck, ToSignString, GetSign, CheckCode, Check, GetMoney`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackCheckSign/MD5.cs`：符号 `MD5, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackCheckSign/RAS.cs`：符号 `RAS, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackCheckSign/RSA.cs`：符号 `RSA, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/CallbackFactory.cs`：符号 `CallbackFactory, GetCallback`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/CheckType.cs`：符号 `CheckType`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/HMAC.cs`：符号 `HMAC, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/HMACSHA1.cs`：符号 `HMACSHA1, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/HMACSHA256.cs`：符号 `HMACSHA256, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/HMACSHA256Base64.cs`：符号 `HMACSHA256Base64, Check, EncodeBase64Url`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/ICallback.cs`：符号 `ICallback, ToSignString, UrlEncode, GetSign, CheckCode, CheckFailCode, Check, GetMoney`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/MD5.cs`：符号 `MD5, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/MD5SHA1.cs`：符号 `MD5SHA1, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/None.cs`：符号 `None, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/PHPSHA1.cs`：符号 `PHPSHA1, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/RAS.cs`：符号 `RAS, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/RSA.cs`：符号 `RSA, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/SHA256MD5.cs`：符号 `SHA256MD5, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/SHA256Standard.cs`：符号 `SHA256Standard, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/UrlSHA256S.cs`：符号 `UrlSHA256, Check`；风险 `callback, funds, gateway, idempotency`
- `Library/GateWay/BetWin.Gateway.Withdraw/Model/CallbackCheckSetting.cs`：符号 `CallbackCheckSetting`；风险 `callback, funds, gateway, idempotency`
- 还有 474 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 5 项、sensitive_file 6 项；明细只写入 `rag/init-reports/20261018T091023Z-project-betwin.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

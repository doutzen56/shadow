# 2026-10-18 BetWin 深入阅读原始记录

- project_id: betwin
- solution_file: BetWin5.0.sln
- task: 用户召唤 `@@X`，要求深入读 BetWin 并补充 RAG 知识库
- permission: L1（只写 SimpX 知识，不改业务仓）
- generated_at: 2026-10-18

## 阅读范围

并行只读了四条热路径：

1. 会员充值（代收）下单 → 回调 → 到账加钱
2. 会员提现 → 审核 → 代付提交 → 回调 → 成功/失败
3. 资金核心（`MoneyAgent` / Redis 钱包）与游戏转账、注单
4. 请求分发、登录权限、后台作业、MQ、返水返佣

## 证据来源（业务仓相对路径）

- `Library/BetWin/Agent/User/UserAgent-Money.cs`
- `Library/BetWin/Agent/Users/MoneyAgent.cs`
- `Library/BetWin/Agent/Users/WithdrawOrderAgent.cs`
- `Library/BetWin/Agent/Sites/PaymentAgent.cs`
- `Library/BetWin/Agent/Sites/WithdrawAgent.cs`
- `Library/BetWin/Agent/Sites/AdminAgent.cs`
- `Library/BetWin/Agent/Sites/RebateAgent.cs`
- `Library/BetWin.Handler/HandlerBase/ResultInterface.cs`
- `Library/Handler/BetWin.Handler.Api/server/cert.cs`
- `Library/GateWay/BetWin.Gateway.Payment/`、`Withdraw/`、`Game/`
- `Services/JobSchedule/`、`DataAnalysis/`、`GameLog/`、`MqServer/`

未读：`envs.secrets.yaml`、业务仓 `*.config`、`appsettings*.json`、证书密钥文件。

## 已沉淀的产物

- 正式模式：`knowledge/patterns/data-consistency/money-agent-addmoneylog-v1.md`
- 正式模式：`knowledge/patterns/reliability/payment-callback-idempotency-checklist-v1.md`
- 事故：`knowledge/incidents/2026/INC-2026-10-18-betwin-recharge-callback-swallow.md`
- 事故：`knowledge/incidents/2026/INC-2026-10-18-betwin-withdraw-fail-back-to-wait.md`
- 事故：`knowledge/incidents/2026/INC-2026-10-18-betwin-api-cert-unauth.md`
- 深挖卡：
  - `knowledge/code-summaries/betwin/cards/check-recharge-order.md`
  - `knowledge/code-summaries/betwin/cards/money-agent-addmoneylog.md`
  - `knowledge/code-summaries/betwin/cards/withdraw-fail-order.md`
  - `knowledge/code-summaries/betwin/cards/api-cert-down.md`
  - `knowledge/code-summaries/betwin/cards/result-interface-api-auth.md`
  - `knowledge/code-summaries/betwin/cards/admin-token-site-binding.md`
- 项目档案补充：`projects/examples/betwin/architecture-map.md`、`boundaries.md`
- 人工索引：`project-index.md` / `pattern-index.md` / `tag-index.md`

## 未覆盖

- 各支付/代付供应商 Special 插件的逐家验签细节
- 存储过程 `usr_AddMoneyLog`、`data_UpdateUserMoney` 的内部实现（不在仓库）
- Web.config 里 Handler 与 Module 的具体挂载方式
- 返水 V2 全量字段与 ES 映射

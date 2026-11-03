---
id: adr-2026-11-01-betwin-migrate-layering
title: BetWin 迁移：分层职责、目录共置与可合并边界
doc_type: decision
projects:
  - simpx-net
  - betwin
lifecycle_status: confirmed
confirmed_by: user
user_reviewed: false
risk_tags:
  - public-contract
evidence_refs:
  - 会话 2026-11-01：用户 @@X「根据职责对代码进行规范化」
  - 会话 2026-11-01：用户 @@X「很多类可以合并…检查哪些应该合并」并拍板 1234
  - 会话 2026-11-01：用户 @@X「按你说的所有，整体都处理一下」
  - 会话 2026-11-01：用户 @@X「NonUnify 能否 class 级别」确认可类级
  - 会话 2026-11-01：用户 @@X「把这些改造的规范都并入到迁移的规范里面去」
  - orchestration/runs/2026-11-01-wave-norm、2026-11-01-wave-merge
---

# ADR-2026-11-01：迁移代码分层与共置

- 日期：2026-11-01
- 范围：SimpX.Net 上 BetWin 业务迁移（`feature/betwin1.0`）Shared / Application.* / Test
- 状态：生效
- 来源：用户 `@@X` 拍板（规范化 + 合并评估落地）

## 决定

### 1. 分层：Shared 厚、宿主 App 薄

1. **领域编排、入账、验签、网关插件、账本副作用**放在 `Application.Shared`。
2. **Admin / WebApi / Channels / SuperAdmin / Schedule** 的 `*AppService` 只做：定路由、读 Form/Query、鉴权特性、调 Shared、返回旧版或统一壳。禁止在 AppService 里堆业务状态机。
3. 宿主入口字段注入领域服务时，单服务场景字段名优先 `_svc`（与命名 ADR 一致）。

### 2. 目录按业务能力共置，不要上帝类

1. 同一聚合的多生命周期服务（如充值建单 / 回调 / 到账）**可以同目录**，**禁止合成一个类**。
2. 支付示例（已落地形态）：
   - `Service/Payment/Recharge/`：建单、回调编排、到账、出站/回调插件、同额检查、线下入款审核等
   - `Service/Payment/Withdraw/`：代付操作、回调、渠道、网关插件等
   - `Service/Payment/` 根：另一套 `PaymentOrder*`、站点通道设定等，**不得**与 `Recharge*` 硬并成一套抽象
3. DTO 镜像同目录：`Dto/Payment/Recharge/`、`Dto/Payment/Withdraw/`；同一能力一个 `*Dtos.cs`（见命名 ADR）。

### 3. 什么该合并、什么不该并

| 该合并 / 共文件 | 不要硬并 |
|---|---|
| 出站/回调**契约 + 插件 + Stub/DTO** 同文件（如提现 `IWithdrawGateway` 与 Plugins；充值 NotifyPlugins + Security + Mock 解析） | `Create` ↔ `Notify` ↔ `Check` / `Operate`（生命周期与宿主不同） |
| 薄接口与唯一实现同文件（如 `IUserActivityLock*`、`IEventLogCheck*`、`IDelottery*`、`IAutoEventStats*`） | `PaymentOrder*` ↔ `Recharge*`（两套支付模型） |
| 同横切的小件：`LegacyLedgerRedis` Factory+Holder；旁路队列接口+Memory+Redis 实现；`UserWalletCacheItem` 可贴 `IMoneyLedger` | 佣金/返点/返水三个 `*PayoutService` 互并（账变类型/表不同；各自已含 Create+Pay） |
| OldApi 壳：`ErrorInfo` / Guest / PermissionAttr / RequestParam 与 `OldApiResult` 共文件 | `OldApiAuthAttribute`、`OldApiPermissionEvaluator`（过滤器/求值逻辑独立） |
| Games：厂商 Transfer/Push 的接口+Stub/Http/Mock 适配共文件；Login 契约可与 `GameLoginService` 同文件 | 把 `MoneyLedger` 并进任意订单服务 |
| 单测假账本：`Test/Fakes/RecordingMoneyLedger` 一处，禁止各测再嵌套一份残缺实现 | 为「文件变少」把活动发放 `RechargePromoGrant` 拖进到账核心 |

合并时注意：`using var` **不是** using 指令，脚本/合并工具不得误抽文件头。

### 4. 命名补充

1. **禁止 `Guard` 命名**（类/方法/文件）：用 `Checker` / `Validator` 等；与全局 `coding.mdc` 一致，迁移代码同样遵守。
2. 应用服务按**业务能力**命名，禁止 Query/Write/Admin 后缀拆分（命名 ADR 第 7、8 条）。

### 5. 旧版返回与 NonUnify

1. 返回 `OldApiResult`（`success/msg/info`）的动作必须跳过统一包装：加 `[NonUnify]`。
2. **允许类级** `[NonUnify]`：该类全部动作都是旧版返回时，挂在类上即可，不必每个方法重复。
3. 同一类里混有「统一 `code/result`」与「旧版返回」时，只给旧版方法挂，不要类级一刀切。

### 6. 外部 HTTP

1. 插件与业务出站继续遵守 `ADR-2026-11-01-betwin-migrate-http-caller`：只用 `IHttpCaller`。
2. 单测用假 `IHttpCaller`，不要再包 `IHttpClientFactory`。

## 不做

- 不借本条改对外路径、资金语义、或把 Admin 退回 Layui。
- 不把 AutoEventStats 大类强行拆/并成上帝类（高风险，另案拍板）。
- 不把本条写入全局 Cursor `.cursor/rules`（除已有 `Guard`/`coding` 外）；迁移执行以 **AGENT-迁移硬约束 + 本 ADR** 为准。

## 后果

- 新迁模块默认按「同目录共置 + 生命周期分服务 + 插件与契约共文件 + App 薄」落地。
- Agent 评估「能否合并」时以本 ADR 表格为准，不得仅因「都是充值」合成单类。

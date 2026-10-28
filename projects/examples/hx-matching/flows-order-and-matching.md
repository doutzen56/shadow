# HX.Matching 订单与撮合主链路

> 路径相对 `HX.Matching.sln` 根。改热路径前回读所列类。

## 1. 商户代收（充值）

| 环节 | 说明 | 证据 |
| --- | --- | --- |
| 入口 | `POST /api/merchant/payin/submit`；收银台 `GET /api/cashier/{orderId}/{token}/info` | `MerchantApi/MinimalApiEndpoints.cs` |
| 管道 | 短超时（Submit≈12s，Cashier≈8s）→ 维护模式 → `OpenApiMerchantTenantMiddleware`（body `merchantCode` / query `mc`）→ IP+MD5 验签 | `MerchantApi/Program.cs`；`OpenApiMerchantTenantMiddleware` |
| 业务 | `MerchantOpenApiService.SubmitDsAsync` → `MerchantApiService.SubmitRechargeAsync`（准入闸门、费率、黑名单、分表插入） | `Services/MerchantOpenApiService.cs`；`MerchantApiService.cs`；`MerchantSubmitAdmissionGate` |
| 出码 | 收银台轮询触发 `CashierRechargePullCoordinator` → `AsyncOrderService` / `MatchmakingService.RechargeInMatchmaking` | `CashierRechargePullCoordinator.cs` |
| 状态 | 创建多为 `None`/`NoneCode` → 出码/撮合后 `Processing`/`Reviewing` → 审过或失败终态 | `OrderStateEnum` |
| 异步 | 写 Bloom、订单生命周期消息；超时 `Job_RechargeOrderTimeout` | `BloomOrderWriteHelper`；`HX.Matching.Tasks` |
| 终态 | `EnterPriseMatchingCallbackHandler.RechargeSuccessProcess` 等 → `HxCallbackService` → 商户 Notify | `Services/EnterPrisePay/` |

合作方旁路：`PartnerApi` `POST /api/partner/order/create` 落单后 **同步** `PullPayeeAsync`（Submit 超时约 20s）。证据：`PartnerOpenApiService.CreateOrderAsync`。

## 2. 商户代付（提现）

| 环节 | 说明 | 证据 |
| --- | --- | --- |
| 入口 | `POST /api/merchant/payout/submit`；Partner `/withdrawal/create` | Minimal / Partner endpoints |
| 业务 | `SubmitWithdrawalAsync` → `MatchmakingService.WithdrawalOrder`（原子扣余额+账变）→ Publish 撮合 | `MerchantApiService`；`MatchmakingService.WithdrawalOrder` |
| 入池消费 | Quartz `MatchmakingHostedService` → `WithdrawInMatchmaking` / 再撮合 | `Quartz.Job/Messaging/MatchmakingHostedService.cs` |
| 推供应商 | `SupplierWithdrawInMatchmaking` / `WithdrawInSupplierMatchmaking`；超时自动推 `Job_WithdrawalTimeOut` 等 | `MatchmakingService`；Tasks |
| 状态 | `Processing` → `SupplierProcessing` → 累计成功/失败终态 | `OrderStateEnum` |
| 终态回调 | 渠道查单结果 → `WithdrawOrderCallbackProcess` → 商户 Notify | `EnterPriseMatchingCallbackHandler` |

## 2.1 代付进撮合池：阶段、对象关系与判定（排障必读）

> 2026-10-26 更正：此前用「库表 `HxMatchingPondConfig` 行数为 0」推断「无池配置故不进池」**不成立**。  
> `GetPondConfigListByMerchantId` 在缺少代付配置时会**默认补 `PondId=1`**，代码路径上几乎不会因「无配置行」走进「未找到撮合池配置」。

### 两阶段（钱已扣再分流）

| 阶段 | 做什么 | 关键对象 |
| --- | --- | --- |
| A. 下单落库 | `MerchantApiService` 算余数 → `WithdrawalOrder`：插代付单 + **原子扣商户余额/手续费** + 账变 → MQ `PublishserMatchmakingAsync` | `HxOrderWithdrawal`、`HxMerchantInfo`、账变 |
| B. 消费分流 | Quartz `MatchmakingHostedService` 调 `WithdrawInMatchmaking`：进 **NXH 撮合池** 或 **SF 推供应商** | 出售规则、风控/骗单、池配置（含默认池）、租户预付费门禁、供应商费率 |

商户 OpenAPI 对阶段 A 成功即返回成功；阶段 B 失败由 HostedService 把 `None→SupplierFail` 并写 `SystemRemark=返回 Message`（失败路径**不一定回写** `OrderPlatformType`，库里可能仍为空）。

### 业务对象怎么咬合

```
商户用户(HxMerchantUser: LabelId / RiskLabelIds / UserName)
    ├─ 等级标签 LabelId → 出售规则 LevelStr + 金额分段 MatchSellRule（标签已删且未改挂 = 孤儿 LabelId → 必 miss）
    ├─ 风控：个人 RiskLabelIds OR 该商户在该等级下升降级规则挂的 ForbidMatch/ForbidChannel
    └─ 骗单：UserName ∈ 代付骗单登记 → 直接 skip 撮合
商户(HxMerchantInfo.SalesRules)
    ├─ 1=系统出售规则（MerchantCode 空）
    └─ 2=商户自有出售规则
出售规则匹配成功 → MatchedSegment(MatchTime/MatchNum/MinMatchAmt) + Rule(IsFixed/FixedMoney/IsAutoRemainder)
撮合池配置 GetPondConfigListByMerchantId(商户)
    ├─ Redis/库有 Withdrawal 行 → 用其 PondId
    └─ 没有 → **默认 PondId=1**（仍算有池，可进 NXH）
进 NXH 前：非首租户预付撮合费门禁（T0001 豁免）；失败则整单失败，不落 SF
进池快照：金额−余数（IsAutoRemainder 可清余数）；固定金额组合只影响池内匹配，不挡入池
未进 NXH：若 ForbidChannel → 拉空失败；否则 SF → GetSupplierFesforChannelId(ChannelId, DF) → WithdrawInSupplierMatchmaking
```

### 判定顺序（`WithdrawInMatchmaking`）

| 序 | 条件 | 结果 |
| --- | --- | --- |
| 1 | 测试商户 | 挂 Processing + SF 字段语义；不推渠道；IsSuccess=true |
| 2 | ForbidMatch∧ForbidChannel | 拉空失败 |
| 3 | 骗单 ∨ ForbidMatch | skip 撮合，后面可走 SF（除非再撞 ForbidChannel） |
| 4 | `MatchSellRule` 未命中（含：**等级标签已删致 LabelId 孤儿**、支付类型/等级/金额段/规则集不匹配） | skip → 尝试 SF；failReason 常为「未找到对应的出售单规则」 |
| 5 | 取代付池（含默认 PondId=1）后预付费门禁失败 | **整单失败，不走 SF** |
| 6 | （理论上）代付池项为 null | 「未找到撮合池配置」→ SF；现网因默认池，**极罕见** |
| 7 | 以上通过 | NXH：写池字段 + `OrderMemoryStoreServices.AddAsync` |
| 8 | 走 SF 但无可用供应商/费率 | IsSuccess=false，Message「计算手续费失败」→ HostedService 落 SupplierFail |

### 读库时怎么解读（避免再误判）

- `MatchingPondId=0` + `RemainMatchingAmount=0`：**没有成功走过 NXH 赋值**，不等于「库里没配池」。
- `State=SupplierFail` + `SystemRemark=提款下单失败，计算手续费失败`：已进 **SF 分支**，死在供应商费率；根因在更早的 skip（出售规则/骗单/禁撮合）或供应商本身，需看订单生命周期「订单撮合处理失败,{failReason}，尝试推送供应商」。
- `OrderPlatformType` 空：常见于 **IsSuccess=false** 时 HostedService 未回写平台类型。

### `MatchSellRule` 与用户等级标签（高频坑）

- 匹配维度：`PayType` + 用户 `LabelId`→`LabelLevel` + 金额落在规则分段 + 商户 `SalesRules`（系统/商户规则集）。
- `LabelId==0` 时代码改按 **1**（默认等级）查。
- Redis 用户标签列表里 **找不到该 `LabelId`**（含**等级标签已删、用户未改挂**）→ 直接 `NotMatched` → 与「未找到出售单规则」同路径 → skip NXH → 可走 SF。
- 删等级标签（`DeleteUserLabel`）**不会**把存量用户的 `LabelId` 迁到默认；默认标签本身删不掉。删**风控标签**会从 `RiskLabelIds` 清掉，一般不单独造成「进不了池」。

### 案例（GWTK2114698014701563904，2026-10-26）

| 项 | 内容 |
| --- | --- |
| 单 | T0001 / 商户 `5JYCKR` / 300 / ZFB / ChannelId=3 |
| 落库 | `MatchingPondId=0`，`RemainMatchingAmount=0`，`State=SupplierFail`，`SystemRemark=提款下单失败，计算手续费失败`，`OrderPlatformType` 可空 |
| 误判 | 库表池配置行数=0 ≠ 不进池（有默认 PondId=1） |
| 现象链 | 未进 NXH → 走 SF → 供应商费率失败 |
| 现场结论 | **用户等级标签被删**，用户仍挂旧 `LabelId` → `MatchSellRule` miss → skip 撮合；生命周期对应「未找到对应的出售单规则」类 failReason |
| 修复面 | 给用户改挂有效等级标签，或重建同级标签并迁移 `LabelId`；顺带确认该通道代付供应商可用 |

证据：`MatchmakingService.WithdrawInMatchmaking`；`HxMatchingPondConfigService.GetPondConfigListByMerchantId`；`HxMerchantSellRuleService.MatchSellRule`（`userLabel==null`）；`HxMerchantUserLabelService.DeleteUserLabel`；`MatchmakingHostedService` 失败回写；`TenantMatchingFeeRules`（T0001 豁免）。

## 3. 撮合（池内匹配）

| 环节 | 说明 | 证据 |
| --- | --- | --- |
| 池存储 | Redis Hash 存订单 DTO + 按 Pond 的 Sorted Set；替代进程内内存池以支持多实例 | `OrderMemoryStoreServices` |
| 代收入池匹配 | `RechargeInMatchmaking`：按金额/通道/拆单规则选代付候选 | `MatchmakingService` |
| 代付入池 | `WithdrawInMatchmaking`；失败可再入队 `matchmakingagain` | 同上；`RabbitMqTopology` |
| 撮合单 | 成功写 `HxOrderMatching`；后续凭证/审单 | 审单服务 |

队列名：`hx.queue.order.matchmaking`、`hx.queue.order.matchmakingagain`（`Infrastructure/Messaging/RabbitMqTopology.cs`）。

## 4. 渠道拉单与回调

### 拉单

- 工厂：`ChannelCoreFactory` + `ChannelCoreKnownImplementations`（`SupplierCode`=适配器类名）。
- 门面：`IChannelClient` / `InProcessChannelClient`。
- 代收：`AsyncOrderService` → `GetRechargeCore` → `RechargeOrder`。
- 并发：`CashierRechargePullCoordinator` 进程内信号量 + Redis 锁（约 30s）。

### 渠道回调（ACK + 异步查单）

1. `Admin.WebApi` `ChannelCallback.Notify*`：IP 白名单 → 立刻 ACK → **不解析业务终态**。
2. `ChannelOrderQueryQueueBackgroundService.EnqueueAsync` → MQ `hx.queue.channel.order.query`。
3. 失败进 TTL 重试队列 `channel.order.query.retry`（到期死信回主队列）；消息体记 `RetryCount`。
4. 适配器 `RechargeOrderQuery` / `WithdrawOrderQuery` → `EnterPriseMatchingCallbackHandler`。

证据：`Admin.WebApi/Controllers/System/CallBack/ChannelCallback.cs`；`ChannelOrderQueryQueueBackgroundService`。

## 5. 商户回调

1. 终态写完后 `HxCallbackService.ExecuteRechargeCallbackAsync` / `ExecuteWithdrawalCallbackAsync`（MD5 签名）。
2. `OrderCallbackBackgroundService`：优先发 MQ `merchant.notify.callback`，失败入本机队列。
3. Quartz `MerchantNotifyCallbackConsumerHostedService` 消费；失败靠 `CallbackCount` 再 Publish。

注意：本机降级队列在进程重启时可能丢失，需监控 MQ 与回调失败率。

## 6. 审单（与终态衔接）

- 三阶段：`OrderMatchingReviewService.FirstReviewAsync` / `SecondReviewAsync` / `FinalReviewAsync`。
- 平台写路径：AdminV2 `/api/v2/platform/review/*`，要求 `ReviewOwner=Platform`。
- 租户旧写路径：`[TenantSelfReviewOnly]`，仅 `ReviewOwner=Tenant` 可写。
- 定时分单：`Job_OrderReview`、`Job_OrderReviewAi`、`Job_PlatformReview`。
- 终审通过常触发入账与商户回调（经 CallbackHandler）。

详见 `flows-multitenant-and-review.md` 与 `boundaries.md`「撮合单审核只归总后台」。

## 7. 资金衔接摘要

- 代付下单：扣本金+手续费（`MatchmakingService.WithdrawalOrder` 与 `HxMerchantService`）。
- 代收成功：入账 + 费率相关账变（`HxMerchantFundsDetailService`）。
- 租户撮合服务费：订单完成时扣；漏扣由 `Job_TenantMatchingFeeSettle` 补偿。
- 账变异步：`hx.queue.merchant.fundsdetail`；ES 侧 CAP `MerchantFundsDetailEsSyncCapHandler`。

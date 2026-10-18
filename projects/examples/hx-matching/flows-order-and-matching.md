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

# HX.Matching 异步调度：MQ、消费者与定时任务

> 异步中枢是 `HX.Matching.Quartz.Job`。单实例风险高：主业务队列应避免在多个宿主重复注册消费者。

## 1. 发布 / 消费分工原则

| 角色 | 典型宿主 | 行为 |
| --- | --- | --- |
| 发布方 | MerchantApi、PartnerApi、Admin、AdminV2、业务服务 | 发订单生命周期、撮合、统计、导出、回调等消息 |
| 集中消费方 | Quartz.Job | 注册绝大部分 `*ConsumerHostedService` 与 CAP Worker |
| 本地 fanout | 各读商户缓存的进程 | 仅消费商户缓存失效类消息 |
| CAP | AdminV2 多只发布；Quartz 消费 | Outbox → ES / 资金明细同步等 |

证据：各宿主 `Program.cs`；`Quartz.Job/Program.cs`。

## 2. 核心队列（拓扑常量）

权威清单：`HX.Matching.Infrastructure/Messaging/RabbitMqTopology.cs`。

| 队列常量 | 用途 |
| --- | --- |
| `OrderLifeQueue` | 订单流转记录 |
| `MerchantFundsdetailQueue` | 商户账变 |
| `OrderMatchmakingQueue` / `OrderMatchmakingAgainQueue` | 代付撮合 / 再入队 |
| `MerchantNotifyCallbackQueue` | 商户 NotifyUrl HTTP 回调 |
| `ChannelOrderQueryQueue` + `ChannelOrderQueryRetryQueue` | 渠道查单 + TTL 延迟重试 |
| `CashierRechargePullQueue` | 收银台拉单（现网可能仍偏进程内 Task，改前核对） |
| `StatsOrderQueue` | 订单统计 |
| `OperLogQueue` | 操作审计（API 投递，Quartz 写 ES） |
| `OfflineDownloadQueue` | 离线导出 |
| `UserSaveQueue` / `RemoveBlacklistQueue` / `UserUpDownQueue` / `NxhErrorQueue` / `CashierAccessLogQueue` / `ApiStoreQueue` | 用户、风控、访问日志、API 存储等 |

Exchange：`hx.exchange.matching`；延迟交换机：`hx.delay.exchange.matching`；导出独立 exchange。

## 3. Quartz 宿主主要消费者 / 后台服务

注册点：`HX.Matching.Quartz.Job/Program.cs`。

| 类 | 职责 |
| --- | --- |
| `MatchmakingHostedService` | 代付撮合/再撮合 |
| `MerchantNotifyCallbackConsumerHostedService` | 商户 HTTP 回调 |
| `ChannelOrderQueryConsumerHostedService` | 渠道查单（含延迟重试） |
| `OrderCallbackBackgroundService` | 商户回调投递（MQ 优先 + 本机降级） |
| `ChannelOrderQueryQueueBackgroundService` | 渠道查单入队与本机降级 |
| `AdminOrderConsumerHostedService` | 订单生命周期等写库 |
| `StatsConsumerHostedService` | 统计 |
| `MerchantStatementHostedService` | 账变/对账相关 |
| `ExportConsumerHostedService` | 离线导出 |
| `ApiOrderConsumerHostedService` | ApiStore 相关 |
| `UserConsumerHostedService` / `ErrorConsumerHostedService` | 用户与撮合失败分析 |
| `OperLogConsumerHostedService` / `CashierAccessLogConsumerHostedService` | 审计与收银台访问日志 |
| `MerchantUpdateConsumerHostedService` | 商户缓存失效 |
| `TelegramBotHostedService` | Telegram 通知/指令 |
| `DataTimeOutService` | 超时巡检辅助 |
| CAP Worker（`AddMultiTenantCapWorkerHost`） | 多租户 CAP 事件消费 |
| `OrderEsCapBackfillOnceHostedService` | 环境变量触发的一次性 ES 回填 |

## 4. 定时任务（Tasks）

任务程序集：`HX.Matching.Tasks`；从 `sys_tasks` 按 `AssemblyName=HX.Matching.Tasks` 加载（`Quartz.Job/TasksExtension.cs`）。SQL 种子参考业务仓 `Sql/Job_V2_sys_tasks.sql`。

### 订单与撮合

| 任务类 | 职责 |
| --- | --- |
| `Job_RechargeOrderTimeout` | 充值超时 |
| `Job_WithdrawalTimeOut` | 提款超时自动推供应商 |
| `Job_WithdrawalDelayOrder` | 延迟提款到期推送 |
| `Job_WithdrawalMantissa` | 尾数自动推送 |
| `Job_MaintenanceWithdrawalDrain` | 维护模式提款排空 |
| `Job_RechargeSuccess` | 未上传凭证等后续 |
| `Job_FalseOrder` / `Job_SystemBlack` | 刷单与黑名单巡检 |

### 审单

| 任务类 | 职责 |
| --- | --- |
| `Job_OrderReview` | 分配审单客服 |
| `Job_OrderReviewAi` | AI 辅助审单 |
| `Job_PlatformReview` | 平台审核队列分单 |

### 租户与资金

| 任务类 | 职责 |
| --- | --- |
| `Job_TenantInit` | 扫描 Initializing 租户执行开户步骤 |
| `Job_TenantStatsDaily` | 租户日统计 |
| `Job_TenantCommissionDaily` | 佣金 Outbox → 日汇总 |
| `Job_TenantMatchingFeeSettle` | 撮合服务费漏扣补偿 |

### ES / Bloom / 运维

| 任务类 | 职责 |
| --- | --- |
| `Job_OrderEsSync` / `Job_OrderEsReconcile` / `Job_OrderEsCapBackfill` / `Job_EsIndexMaintenance` | 订单 ES 同步、对账、回填、索引维护 |
| `Job_BloomWarmup` / `Job_BloomLifecycleRefresh` | Bloom 重建与主备生命周期 |
| `Job_Stats` | 日统计 |
| `Job_SupplierBalanceSync` | 供应商余额 |
| `Job_DataMove` | 租户数据迁移与任务日志清理 |
| `Job_GoogleSecretEncrypt` | 谷歌密钥加密迁移 |
| `Job_SqlExecute` / `Job_HttpRequest` / `Job_SyncTest` | 通用运维任务 |

## 5. 调度衔接示意

```text
热路径写库成功
  ├─ Publish 生命周期 / 账变 / 统计 / 撮合
  ├─ 渠道回调 HTTP → 查单队列 → Handler
  └─ 终态 → Notify 队列 → HTTP 打商户

定时扫表 / 状态机
  ├─ 超时关单、自动推单、审单分单
  ├─ 租户 Init / 佣金 / 撮合费补偿
  └─ ES 同步对账、Bloom 维护
```

## 6. 改异步链路检查清单

1. 新消费者默认只加在 Quartz，不要在 MerchantApi/PartnerApi 再挂主业务消费。
2. 新队列先落 `RabbitMqTopology` 常量，避免魔法字符串。
3. 重试优先用 TTL/死信或消息计数字段，避免无限本机内存队列。
4. 涉及资金/订单终态的消费者必须幂等；验证要打真实 MQ 或集成场景，不能只测映射。
5. 单实例 Quartz 故障会影响全部异步；变更前评估就绪探针与告警，而非假设 LB 摘流。

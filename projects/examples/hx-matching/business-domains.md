# HX.Matching 业务板块总览

> 证据来自 `HX.Matching.sln` 当前源码与项目档案；行为结论改代码前必须回读源码。  
> 关联：`architecture-map.md`、`boundaries.md`、`flows-*.md`、`knowledge/code-summaries/hx-matching/cards/`。

## 1. 系统一句话

支付撮合平台：商户/合作方下单 → 验签与风控 → 撮合池或渠道拉单 → 审单/渠道查单 → 资金账变 → 商户回调；异步集中在 Quartz 任务进程。

## 2. 模块地图（按职责）

| 板块 | 主要落点 | 核心能力 | 衔接对象 |
| --- | --- | --- | --- |
| 开放下单 | MerchantApi / PartnerApi + `MerchantApiService` / `PartnerOpenApiService` | 代收、代付、查单、收银台；Partner 同步取码 | 撮合、渠道、准入闸门、商户缓存 |
| 撮合引擎 | `OrderMatchmakingServices/MatchmakingService`、`OrderMemoryStoreServices` | 代付入池、代收匹配池内代付单、再撮合 | MQ `order.matchmaking*`、审单、供应商推送 |
| 渠道适配 | `HX.Matching.Channels` + `AsyncOrderService` | 按 SupplierCode 拉代收码/推代付 | 渠道回调 → 异步查单 |
| 审单 | `OrderMatchingReviewService`、AdminV2 平台审单、`ReviewOwner` | 一审/二审/终审、转派、AI 辅助 | 终态回调、资金入账 |
| 资金账变 | `HxMerchantService`、`HxMerchantFundsDetailService`、租户钱包 | 余额/授信、费率、撮合服务费、佣金 | MQ 账变、CAP→ES |
| 回调通知 | `EnterPriseMatchingCallbackHandler`、`HxCallbackService`、`OrderCallbackBackgroundService` | 渠道 ACK+查单；商户 Notify MQ+本机降级 | Quartz 消费者 |
| 多租户 | `MultiTenant/*`、`Job_TenantInit` | 开户步进、Host/商户码定租户、审单归属 | 公共库 + 租户库 |
| 搜索与对账 | `Es/*`、`Cap/OrderEsBackfill`、`Job_OrderEs*` | 订单/资金查询索引、对账、回填 | CAP Worker（Quartz） |
| 运营后台 | Admin / AdminV2 / MerchantAdmin | 配置、报表、离线导出、平台租户与钱包 | SignalR、导出 MQ |
| 稳定性组件 | Bloom、准入闸门、限流、线程池调优 | 去重预判、热路径保护、就绪探针 | 各宿主 |

## 3. 订单主状态（业务口径）

权威枚举：`HX.Matching.Model/.../OrderStateEnum.cs`（注释即状态机说明）。

| 状态 | 典型含义 | 常见进入事件 |
| --- | --- | --- |
| `None` / `NoneCode` | 未打开收银台 / 未出码 | 代收刚创建 |
| `Processing` | 撮合交易中 | 下单进入链路；可补救驳回回此态 |
| `SupplierProcessing` | 供应商处理中（代付） | 推送供应商 |
| `SupplierFail` | 供应商失败 | 渠道侧失败 |
| `Reviewing` | 审核中（代收） | 凭证/撮合成功/供应商回调成功 |
| `Success` / `Fail` / `Cancel` | 终态 | 审过/满额成功；不可补救失败；取消 |

改状态机或回调出口前，必须回读该枚举注释与 `EnterPriseMatchingCallbackHandler`。

## 4. 板块间业务衔接（调度点）

```text
Merchant/Partner 下单
  → 准入闸门 / 验签 / 定租户
  → 写订单 + Bloom + 生命周期消息
  → 代付：扣余额 → Publish 撮合 MQ → Quartz MatchmakingHostedService
  → 代收：收银台 info / Partner PullPayee → 撮合池匹配或 Channel 拉单
  → 需审：Reviewing → 平台或租户自审（ReviewOwner）
  → 渠道回调：Admin ACK → 查单队列 → Handler 推终态
  → 终态：账变 + 商户 Notify（MQ 优先）
  → ES：同步/对账/CAP 回填（异步）
```

关键调度点清单：

1. **进程边界**：开放口与管理端主要发 MQ；Quartz 集中消费（避免双实例重复消费主业务队列）。
2. **撮合池**：Redis Hash + Pond Sorted Set（`OrderMemoryStoreServices`），多实例共享。
3. **渠道回调**：HTTP 不解析终态，只 ACK + 入查单队列。
4. **审单归属**：公共库 `ReviewOwner`；新租户默认 `Platform`。
5. **租户未 Ready**：域名/开放口解析拒绝或走 Legacy 规则（见多租户文档）。

## 5. 风险标签（按板块）

- 资金/订单：代收付、撮合、账变、回调、审单终态。
- 权限/登录：AdminV2 JWT、租户代登、`TenantSelfReviewOnly`、`PlatformOnly`。
- 异步一致性：MQ、CAP、ES 对账、Bloom 生命周期。
- 生产配置：租户 DDL 自动执行开关、AgileConfig、密钥类任务。

## 6. 文档导航

| 文档 | 内容 |
| --- | --- |
| `flows-order-and-matching.md` | 代收/代付/撮合/渠道/回调链路 |
| `flows-async-jobs.md` | MQ 拓扑、消费者、定时任务 |
| `flows-multitenant-and-review.md` | 租户开户、钱包手续费、审单边界 |
| `architecture-map.md` | 宿主与分层 |
| `boundaries.md` | 宿主与审单硬边界 |
| `knowledge/code-summaries/hx-matching/cards/` | 关键组件深挖卡 |
| `knowledge/glossary/hx-matching-domain.md` | 领域术语 |

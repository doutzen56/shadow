# HX.Matching 多租户、钱包与审单归属

> 口径以业务仓当前源码为准。本文随 2026-10-19/20 Host 定租户统一做过修订。  
> 检索说明：SimpX 向量召回尚未可用；日常靠项目档案、关键词与代码摘要定位，涉及行为必须回读源码。

## 1. 定租户入口（统一：绑 Host，不拼路径租户段）

### 1.1 总原则

| 原则 | 说明 |
| --- | --- |
| 定租户只看 Host + Purpose | 商户后台 / 商户下单 / 收银台 / 合作方开放口：先解析绑定域名 |
| 未绑定 | 商户侧用途（商户后台、商户下单、收银台）→ 降级首租户（默认 `T0001`，`TenantContextSource.HostDegraded`） |
| 管理用途未绑定 | 总后台 / 租户后台用途 → HTTP 404「当前域名尚未开通…」 |
| 不再用路径切租户 | 渠道回调路径固定 `channelcallback/Withdraw\|Recharge/{SupplierId}/{OrderId}`，**不拼** `/t/{TenantCode}/` |
| 不再用 `mc` 定租户 | 收银台 URL 不带 `?mc=`；租户只认收银台绑定域 |
| `merchantCode` | 仍是业务字段（验签、找商户）；只校验「商户所属租户 = 当前 Host 租户」，**不**再 Push 一套 MerchantCode 租户上下文 |
| `MultiTenant:Enabled` | **不定租户**。仍管：切库、缓存前缀、租户初始化、Job 分租户、CAP 租户头等 |
| 对外契约 | **不向商户/渠道暴露 TenantCode** |

### 1.2 宿主对照

| 入口 | 定租户方式 | 证据 |
| --- | --- | --- |
| MerchantApi | `MerchantApiHostTenantMiddleware`：收银台 → `DomainPurpose.Cashier`，其余（含 `/channelcallback`）→ `DomainPurpose.MerchantOpenApi`；再 `OpenApiMerchantTenantMiddleware` 只做商户归属校验 | `MerchantApi/Program.cs`；两中间件 |
| PartnerApi | 同上 Host（商户下单用途）+ OpenApi 归属校验 | `PartnerApi/Program.cs` |
| MerchantAdmin | `HostTenantResolveMiddleware(DomainPurpose.MerchantAdmin)` | `MerchantAdmin.WebApi/Program.cs` |
| AdminV2 | Host 解析（`TenantResolveMiddleware`）；路由 `/api/v2/{platform\|hx\|system\|tenant}` | AdminV2 中间件与 V2 管道 |
| Admin（V1） | **默认库 = 现网/首库（T0001 世界）**；渠道回调若仍打到 V1，与多租户新租户库无关 | `Admin.WebApi` 无商户侧 Host 中间件属预期 |
| 渠道回调主路径 | 多租户开：回调站点根 = 该租户「商户下单」绑定域 → 打到 MerchantApi，靠 Host 定租户 | `TenantBoundEndpointUrls.ResolveChannelCallbackBaseUrlAsync`；`CallbackUrlHelper` |
| 渠道回调 V1 兼容 | 关多租户或仍配置旧 `SYS.CALLBACK.ADDRESS.*` 指向 Admin 时，走 V1 默认库 | `TenantBoundEndpointUrls`；`Admin.../ChannelCallback` |

### 1.3 域名绑定类型与用途

| BindType（绑定类型） | Purpose（用途） | 典型宿主 |
| --- | --- | --- |
| 总后台 Platform | Admin | Admin / AdminV2 |
| 租户后台 Tenant | Admin | AdminV2 租户后台 |
| 商户后台 MerchantAdminPortal | MerchantAdmin | MerchantAdmin.WebApi |
| 商户下单 MerchantOrder | MerchantOpenApi | MerchantApi / PartnerApi / 渠道回调 |
| 收银台 Cashier | Cashier | MerchantApi `/api/cashier` |

证据：`HX.Matching.Model/.../HxTenant.cs` 中 `DomainBindType` / `DomainPurpose`；`DomainPurpose.AllowsLegacyDegrade` 仅商户侧三种用途。

### 1.4 回调地址拼装

- 配置/绑定只存**站点根**（禁止文档写「基址」一词，见 ADR-2026-10-20）。
- 程序拼：`{站点根}channelcallback/Withdraw|Recharge/{supplierId}/{orderId}`。
- 多租户开且未绑商户下单域：解析回调站点根可能为 null → 推单侧无回调地址（发布前必须绑域）。

证据：`CallbackUrlHelper`；`CashierUrlHelper`（不再拼 mc）；`ChannelCallbackService` + MerchantApi `ChannelCallbackEndpoints`。

启动预热：Merchant/Partner 可对 Ready 租户预热商户缓存（`TenantJobRunner.ForEachReadyTenantAsync`）。

## 2. 租户生命周期与初始化

| 项 | 说明 |
| --- | --- |
| 状态 | `HxTenant.InitStatus`：`None` → `Initializing` → `Ready` / `Failed` |
| 触发 | 平台生命周期 API 置 Initializing；定时 `Job_TenantInit` 扫描执行 |
| 步骤 | `RegisterDb` → `ApplyDdl` → `Seed` → `AdminUser` → `Complete`（步骤表 `HxTenantInitStep`） |
| 幂等 | 已成功步骤跳过；`RegisterDb` 对非空库可复用；失败可 InitRetry/requeue |
| 生产约束 | 默认禁止自动 DDL；需显式 `AllowAutoApplyDdl` 或人工灌库后再对齐步骤状态 |
| 未 Ready | `DomainHostTenantResolver` 可拒绝（初始化中/失败/不可用文案） |

证据：`MultiTenant/TenantInitWorkerService.cs`；`TenantLifecycleService.cs`；`HX.Matching.Tasks/.../Job_TenantInit.cs`；深挖卡 `knowledge/code-summaries/hx-matching/cards/tenant-init-worker.md`。

## 3. 审单归属 ReviewOwner

| 规则 | 说明 |
| --- | --- |
| 存储 | 公共库 `hx_tenant.ReviewOwner` |
| 默认 | 创建租户默认 `Platform`（`TenantLifecycleService`） |
| 解析 | `ITenantReviewOwnerResolver`；多租户关或空值时解析器可能视为 `Tenant`（过渡/兼容，改前核对） |
| 落单 | 撮合进入审单前写入归属（`MatchmakingService` 事务前解析） |

### 平台审单（推荐主路径）

- 路由：AdminV2 `/api/v2/platform/review/*`，`[PlatformOnly]`。
- 写操作要求归属为 `Platform`，否则 403；校验公共库审核队列与客服类型。
- 查询可跨租户；写时 `RunAsTenantAsync` 进入租户库复用 `IOrderMatchingReviewService`。

证据：`AdminV2.../HxPlatformReviewController.cs`；`HxPlatformReviewV2ApplicationService`；深挖卡 `cards/review-owner-boundary.md`。

### 租户自审（过渡）

- 路由：`/api/v2/hx/ordermatching/*` 上带 `[TenantSelfReviewOnly]` 的写接口。
- 条件：租户后台身份、非代登、且 `ReviewOwner=Tenant`；否则 403「由平台审核」。
- 只读详情/AI/历史凭证：**不**走该过滤器，可按权限查看。
- MerchantAdmin：**无**撮合审单写接口。
- 存量如 T0001 仍为 `Tenant` 时自审；迁平台后旧写接口可下线。

硬原则见 `boundaries.md`「撮合单审核只归总后台」。新增审单功能默认做在总后台；租户侧加审核动作须用户拍板。

## 4. 租户钱包与撮合服务费 / 佣金

| 主题 | 说明 | 证据 |
| --- | --- | --- |
| 钱包 | 公共库 `hx_tenant_wallet`；余额可负；负余额停撮合等策略 | `Model/.../HxTenantWallet.cs`；平台钱包控制器 |
| 账变类型 | 含 Fee*、MatchingFee* 等 | 钱包/账变模型与平台 adjust API |
| 撮合服务费 | 订单完成主路径实时扣；`Job_TenantMatchingFeeSettle` 扫漏补扣 | `TenantMatchingFeeSettleService`；对应 Job |
| 佣金 | `commissionMode` / 策略：`MultiTenant/Commission/*`（固定费率/阶梯）；日汇总 Job | `ICommissionCalcStrategy`；`Job_TenantCommissionDaily` |

商户侧费率（代收付手续费）与租户撮合服务费是不同账本：前者走商户余额/账变，后者走租户钱包。改计费前分别回读 `HxMerchantService.GetRechargeFess/GetWithdrawalFess` 与租户扣费服务。

## 5. 与主链路的衔接点

```text
创建租户（默认 ReviewOwner=Platform）
  → Init 步进至 Ready
  → 绑定：商户下单域 / 收银台域 / 商户后台域（按用途）
  → 开放口：Host 定租户 → merchantCode 校验归属 → 下单
  → 渠道回调：商户下单绑定域 + /channelcallback/...（MerchantApi Host）
  → Admin V1 回调：仅首库/旧配置兼容，不承担新租户切库
  → 撮合进审：按 ReviewOwner 进平台队列或租户自审
  → 订单完成：商户账变 + 可能扣租户 MatchingFee
  → 日终：佣金 Outbox 汇总、漏扣补偿任务
```

## 6. 改动检查清单

1. 新租户是否默认平台审？是否误开自审写接口？
2. Init 步骤失败后重试是否会重复建库/覆盖人工灌库？
3. 钱包扣费与商户账变是否在同一业务事件上重复/漏扣？
4. 商户下单 / 收银台 / 回调绑定域是否齐？未绑商户侧是否接受降级 T0001？
5. 是否又引入 `/t/{TenantCode}` 或收银台 `mc` 定租户？
6. `Enabled` 是否被误用来「跳过 Host 解析」？
7. 代登场景是否被 `TenantSelfReviewOnly` 正确拦截？
8. 存量渠道若仍回调旧 `/t/...` URL，是否已迁移？

## 7. 相关知识索引（SimpX）

| 类型 | 路径 |
| --- | --- |
| ADR（Host 定租户） | `knowledge/decisions/2026/ADR-2026-10-20-hx-matching-host-tenant-resolve.md` |
| 术语 | `knowledge/glossary/hx-matching-domain.md` |
| 深挖卡 | `knowledge/code-summaries/hx-matching/cards/host-tenant-resolve.md` |
| 渠道 ACK+查单 | `knowledge/code-summaries/hx-matching/cards/channel-callback-ack-query.md` |
| 租户 Init | `knowledge/code-summaries/hx-matching/cards/tenant-init-worker.md` |
| 审单边界 | `knowledge/code-summaries/hx-matching/cards/review-owner-boundary.md` |

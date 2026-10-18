# HX.Matching 多租户、钱包与审单归属

## 1. 定租户入口

| 入口 | 规则 | 证据 |
| --- | --- | --- |
| 商户/合作方开放口 | body `merchantCode` 或收银台 query `mc`；`channelOrder/query` 豁免；多租户关闭时行为同旧现网 | `OpenApiMerchantTenantMiddleware` |
| AdminV2 | Host 域名解析租户；路由 `/api/v2/{platform\|hx\|system\|tenant}` | `TenantResolveMiddleware`；`V2PipelineExtensions` |
| MerchantAdmin | Host + Purpose=`MerchantAdmin` | `HostTenantResolveMiddleware` |
| 开放契约 | **不向商户暴露 TenantCode**，仅内部切库 | 中间件 remarks |

启动预热：Merchant/Partner 可对 Ready 租户预热商户缓存（`TenantJobRunner.ForEachReadyTenantAsync`）。

## 2. 租户生命周期与初始化

| 项 | 说明 |
| --- | --- |
| 状态 | `HxTenant.InitStatus`：`None` → `Initializing` → `Ready` / `Failed` |
| 触发 | 平台生命周期 API 置 Initializing；定时 `Job_TenantInit` 扫描执行 |
| 步骤 | `RegisterDb` → `ApplyDdl` → `Seed` → `AdminUser` → `Complete`（步骤表 `HxTenantInitStep`） |
| 幂等 | 已成功步骤跳过；`RegisterDb` 对非空库可复用；失败可 InitRetry/requeue |
| 生产约束 | 默认禁止自动 DDL；需显式 `AllowAutoApplyDdl` 或人工灌库后再对齐步骤状态 |
| 未 Ready | 解析器可拒绝（如 `DomainHostTenantResolver`） |

证据：`MultiTenant/TenantInitWorkerService.cs`；`TenantLifecycleService.cs`；`HX.Matching.Tasks/.../Job_TenantInit.cs`。

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

证据：`AdminV2.../HxPlatformReviewController.cs`；`HxPlatformReviewV2ApplicationService`。

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
  → 开放口按 merchantCode 切租户库下单
  → 撮合进审：按 ReviewOwner 进平台队列或租户自审
  → 订单完成：商户账变 + 可能扣租户 MatchingFee
  → 日终：佣金 Outbox 汇总、漏扣补偿任务
```

## 6. 改动检查清单

1. 新租户是否默认平台审？是否误开自审写接口？
2. Init 步骤失败后重试是否会重复建库/覆盖人工灌库？
3. 钱包扣费与商户账变是否在同一业务事件上重复/漏扣？
4. Host 定租户与开放口 merchantCode 定租户结果是否一致？
5. 代登场景是否被 `TenantSelfReviewOnly` 正确拦截？

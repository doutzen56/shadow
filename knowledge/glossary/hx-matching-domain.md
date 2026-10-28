---
id: glossary-hx-matching-domain
title: HX.Matching 领域术语
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: true
reviewed_at: 2026-10-26
reviewed_by: user
projects:
  - hx-matching
last_verified_at: 2026-10-26
---

# HX.Matching 领域术语

| 术语 | 含义 | 易混点 |
| --- | --- | --- |
| 代收 / Payin / 充值单 | 商户侧收款订单，用户付款进商户 | 与租户钱包入金不是同一账本 |
| 代付 / Payout / 提款单 | 商户侧出款订单，需撮合或推供应商 | 下单时常先扣商户余额 |
| 撮合单 / Matching | 代收与代付候选匹配产生的撮合关系 | 不等于渠道供应商单 |
| 出码 | 给收银台返回收款账户/二维码等信息 | Partner 可同步取码；Merchant 常靠轮询 |
| 审单 / Review | 对代收凭证等的人工/AI 审核 | 归属由 `ReviewOwner` 决定 |
| ReviewOwner | 公共库字段：`Platform` 或 `Tenant` | 新租户默认 Platform |
| 渠道 / Supplier | 外部支付供应商适配器 | `SupplierCode` 常等于适配器类名 |
| 查单 | 主动向渠道查询订单终态 | 渠道回调后的权威确认手段 |
| Notify / 商户回调 | 平台把终态 POST 到商户 NotifyUrl | 与渠道回调方向相反 |
| 渠道回调 | 供应商 POST/GET 到平台 `/channelcallback/...` | 只 ACK+入队查单；租户靠 Host |
| 准入闸门 | 进程内快速失败限流 | 不是网关全局限流 |
| 撮合池 Pond | Redis 中按规则分组的候选集合 | 多实例共享 |
| 租户 Init | 开户步进直至 Ready | 未 Ready 可能拒绝解析 |
| 撮合服务费 MatchingFee | 租户钱包侧费用 | 不同于商户手续费 |
| 商户手续费 | 代收付费率账变 | 走商户资金明细 |
| CAP 回填 | 事务消息驱动的 ES 同步/回填 | 消费在 Quartz Worker |
| Bloom 订单过滤器 | Redis Bloom 预判订单是否可能存在 | 假阳性可接受，假阴性需避免业务假设错误 |
| Host 定租户 | 按请求 Host + `DomainPurpose` 查 `hx_domain_binding` | 与 `Enabled` 开关无关 |
| HostDegraded | 商户侧域名未绑时降级到首租户 | 默认 T0001；管理用途未绑是 404 |
| 站点根 | 绑定域或配置里的站点根 URL | **禁止写「基址」** |
| 商户下单绑定域 | Purpose=`MerchantOpenApi` | 下单 API + 渠道回调主路径 |
| 收银台绑定域 | Purpose=`Cashier` | 仅 `/api/cashier`；不靠 `mc` |
| 首租户 / Legacy | `MultiTenantRuntime.LegacyTenantCode` | 常为 T0001；Admin V1 默认库世界 |
| Admin V1 回调 | 打到 Admin.WebApi 的旧回调入口 | 只服务首库；多租户新租户应打 MerchantApi |
| 用户等级标签 | `HxMerchantUserLabel`；用户 `LabelId` 指向它 | 删标签不迁用户 Id → 孤儿 LabelId |
| 孤儿 LabelId | 用户仍挂已删等级标签 Id | `MatchSellRule` 直接 NotMatched，代付不进 NXH |
| NXH / SF | 内循环撮合 / 四方供应商 | 代付阶段 B 二选一；失败回写可能不落平台类型 |

权威状态枚举见业务仓 `OrderStateEnum` 注释。定租户细则见 `flows-multitenant-and-review.md` 与 ADR-2026-10-20。

代付进池与标签孤儿见 `flows-order-and-matching.md` §2.1 与 ADR-2026-10-26。

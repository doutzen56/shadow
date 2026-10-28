---
id: adr-2026-10-26-hx-matching-withdrawal-enter-pool-gate
title: HX.Matching 代付进撮合池：阶段分流、对象关系与标签孤儿坑
doc_type: decision
projects:
  - hx-matching
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: true
reviewed_at: 2026-10-26
reviewed_by: user
risk_tags:
  - order-state
  - funds
evidence_refs:
  - projects/examples/hx-matching/flows-order-and-matching.md
  - knowledge/code-summaries/hx-matching/cards/withdrawal-enter-matching-pool.md
  - knowledge/glossary/hx-matching-domain.md
  - HX.Matching.ServiceCore/Services/OrderMatchmakingServices/MatchmakingService.cs
  - HX.Matching.ServiceCore/Services/HxMatchingPondConfigService.cs
  - HX.Matching.ServiceCore/Services/HxMerchantSellRuleService.cs
  - HX.Matching.ServiceCore/Services/HxMerchantUserLabelService.cs
  - HX.Matching.ServiceCore/Services/HxLabelRiskService.cs
  - HX.Matching.Quartz.Job/Messaging/MatchmakingHostedService.cs
  - HX.Matching.ServiceCore/MultiTenant/TenantMatchingFeeGateService.cs
  - 会话 2026-10-26：GWTK2114698014701563904；池行数误判更正；现场确认等级标签已删
---

# ADR-2026-10-26：代付进撮合池阶段、关系与标签孤儿

- 日期：2026-10-26（同日多次修订，本版为收录定稿）
- 项目：`projects/examples/hx-matching`（`HX.Matching.sln`）
- 状态：生效
- 来源：代付不进池排障 + 业务关系复核 + 现场「等级标签被删」

## 背景

「不进撮合池」易被收成单点检查。真实链路是：**阶段 A 先扣款落单，阶段 B 再异步分流 NXH/SF**。  
曾误用「库表撮合池配置行数=0」定责；后确认服务会默认 `PondId=1`。现场案例进一步确认：**用户等级标签删除后未迁移 `LabelId`** 会导致出售规则必 miss。

## 决定（排障与设计口径）

1. **两阶段**：A=`WithdrawalOrder`（扣款+落库+发 MQ）；B=`WithdrawInMatchmaking`（NXH 入池或 SF 推供应商）。OpenAPI 只保证 A 成功。
2. **进 NXH 前提**：非测试商户；非双禁风控；非（骗单|ForbidMatch）；**`MatchSellRule` 命中**；非首租户过预付费门禁；再 `OrderMemoryStoreServices.AddAsync`。
3. **池配置**：缺代付配置时 `GetPondConfigListByMerchantId` 默认 `PondId=1`。**禁止**用库表行数单独断定「无池故不进池」。
4. **等级标签孤儿**：删 `HxMerchantUserLabel` **不**改写存量用户 `LabelId`；`MatchSellRule` 在 Redis 列表找不到该 Id 时 `NotMatched`（与「未找到出售单规则」同路径）。默认标签不可删；`LabelId==0` 按 1 处理。删风控标签会清 `RiskLabelIds`，一般不单独造成进不了池。
5. **SF 失败表象**：`SupplierFail` +「计算手续费失败」表示已 skip NXH 并死在供应商费率；根因看生命周期 `failReason`，优先核对用户 `LabelId` 是否仍有效。
6. **失败回写**：HostedService 在 `IsSuccess=false` 时常写 `SystemRemark` 与 `None→SupplierFail`，**不一定**回写 `OrderPlatformType`。

## 案例定案（GWTK2114698014701563904）

- 落库符合「未进 NXH → SF 费率失败」。
- 现场：相关**用户等级标签已删** → 与口径第 4 条一致，作为本单 skip 撮合主因收录。
- 修复：用户改挂有效等级标签（或重建并迁 Id）；并确认通道代付供应商可用以免再挂在 SF。

## 后果

- 档案 §2.1、深挖卡、术语表与本 ADR 对齐；Agent 排障按对象关系而非单点配置。
- 产品侧删除等级标签应视为高风险运维（需迁用户或禁删非空标签）——本 ADR 只定口径，不强制改代码。

## 回滚

- 仅文档；归档本 ADR 并改回档案表述即可。

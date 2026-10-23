---
decision_id: ADR-2026-10-21-hx-matching-v2-mt-not-break-v1
title: V2 多租户改动不得牵连 V1；共享代码优先保 V1
date: 2026-10-21
project: hx-matching
status: accepted
user_reviewed: false
confirmed_by: user
source: 用户口述「有一个原则记住」
---

# V2 多租户改动不得牵连 V1；共享代码优先保 V1

## 决策

做 V2 / 多租户相关改造时：

1. **默认不改 V1 宿主行为**（`HX.Matching.Admin.WebApi` 及首库/现网路径）。
2. 改动能放在 AdminV2 / 租户上下文 / 多租户专用分支里的，不要改共享默认路径。
3. 必须动 `ServiceCore` 等共享代码时：**优先保证 V1 行为不变**（例如仅在 `MultiTenantRuntime.Enabled` + 租户后台上下文才走新逻辑；V1/首库仍走原逻辑）。
4. 若认定 V1 也必须同步改行为：先说明影响面，**等用户拍板**后再改，不得自行「顺便统一」。

## 原因

V1 仍是现网/首库入口；V2 多租户演进不能把回归风险摊到 V1。

## 执行口径

- `@@X` / `@@BA` / `@@BE`：涉及共享模块时先标「是否影响 V1」；影响则拆 V2 专用路径或加开关/分支。
- 已误伤 V1 的改动：主动披露，按用户意见回退或补拍板。

## 证据

- 用户 2026-10-21：「v2多租户改东西不要影响v1，如果一旦有共享，优先保证v1不变动，如果确实要同步改动，请让我拍板」

## 补充：预付撮合费豁免首租户（2026-10-21）

用户拍板：预付撮合费只针对非 T0001；T0001 不管、整条豁免；现场只需保证之后新下单，不强制重推已失败单。

实现落点（业务仓）：`TenantMatchingFeeRules.AppliesToTenant`；门禁 / `SyncMatchingFeeAsync` / 对账 Job / 钱包余额副作用共用。


---
summary_id: review-owner-boundary
project_id: hx-matching
file_path: HX.Matching.ServiceCore/MultiTenant/TenantReviewOwnerResolver.cs
file_hash: cea8e56d7b5295b04b05ca35c4d34dcfa6fd451cb332c639db6d75f9f772efe9
symbol: ITenantReviewOwnerResolver / TenantSelfReviewOnly / Platform review APIs
symbol_kind: type
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@TL"
  - reviewer
task_scope:
  - 方案设计
  - 代码实现
  - 代码审查
risk_tags:
  - permission
  - order-state
  - user-data
last_seen: 2026-10-18
---

# 撮合审单归属边界深挖卡

## 职责

用公共库 `ReviewOwner` 决定撮合单由平台审还是租户自审；过滤器与平台 API 共同保证「新租户默认平台审、租户写接口可过渡关闭」。

## 契约

- 平台写：`/api/v2/platform/review/*`，归属必须为 `Platform`。
- 租户写：带 `[TenantSelfReviewOnly]`，要求租户身份、非代登、`ReviewOwner=Tenant`。
- 租户只读审单查询：不强制自审归属，按权限可读。

## 依赖

- `TenantLifecycleService` 创建默认 `Platform`
- `HxPlatformReviewV2ApplicationService` / `OrderMatchingReviewService`
- 项目档案 `boundaries.md` 硬原则

## 状态与副作用

撮合进审前写入订单/审单上的归属字段；分单任务 `Job_PlatformReview` / `Job_OrderReview*` 按队列工作。

## 验证提示

1. `ReviewOwner=Platform` 的租户调用租户终审写接口应 403。
2. 平台审单写成功后订单进入预期审态/终态。
3. 代登会话不能绕过 `TenantSelfReviewOnly`。

## 风险说明

- 解析器在多租户关闭时的默认值与创建默认值可能不一致，环境开关要核对。
- 在租户侧新增审核动作属于边界变更，需用户拍板。

## 源码回读要求

任何审单菜单/接口增删前，回读 boundaries、过滤器与平台/租户控制器特性标注。

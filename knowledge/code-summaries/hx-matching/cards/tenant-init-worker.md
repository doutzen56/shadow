---
summary_id: tenant-init-worker
project_id: hx-matching
file_path: HX.Matching.ServiceCore/MultiTenant/TenantInitWorkerService.cs
file_hash: f169c988732176ff0d72b989ec196658d178ef40805139de140e96e08848a325
symbol: TenantInitWorkerService.ProcessTenantInitAsync / RunStepAsync
symbol_kind: method
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@DEVOPS"
  - "@@QA"
task_scope:
  - 代码实现
  - 发布交付
  - 缺陷定位
risk_tags:
  - database-schema
  - production-config
  - permission
last_seen: 2026-10-18
related_pattern: pattern-reliability-saas-tenant-init-steps-v1
---

# 租户初始化步进深挖卡

## 职责

把 `Initializing` 租户按步骤表推进到 `Ready`：注册库、DDL、种子、管理员、完成；支持幂等跳过与失败重试。

## 契约

- 输入：租户编码、步骤状态。
- 输出：步骤成功/失败写入 `HxTenantInitStep`；全部完成后 `InitStatus=Ready`。
- 失败契约：单步失败标记 Failed，不假装 Ready；生产自动 DDL 受开关约束。

## 依赖

- `Job_TenantInit` 定时扫描
- `TenantLifecycleService` 置位与默认 `ReviewOwner=Platform`
- 连接串/Schema 相关基础设施（改前查 MultiTenant 目录）

## 状态与副作用

步骤顺序：`RegisterDb` → `ApplyDdl` → `Seed` → `AdminUser` → `Complete`。  
`RegisterDb` 对非空库可幂等复用；人工灌库后必须对齐步骤状态。

## 验证提示

1. 重复执行已成功步骤应跳过且不破坏数据。
2. 关闭自动 DDL 时，ApplyDdl 不得在生产静默建库。
3. Failed 后 InitRetry 能从失败步继续。

## 风险说明

- 自动建库权限过大。
- 手工灌库与步骤表不一致会导致「看似 Ready、实际缺对象」。

## 源码回读要求

执行或改 Init 前必读 `TenantInitWorkerService`、`TenantInitStepRules` 与对应 SQL 脚本。

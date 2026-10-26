---
id: pattern-reliability-saas-tenant-init-steps-v1
title: SaaS 租户开户步进状态机（可幂等、可禁自动 DDL）
domain: reliability
tags:
  - multi-tenant
  - provisioning
  - state-machine
  - ddl
projects:
  - hx-matching
maturity: validating
reuse_score: 84
last_verified_at: 2026-10-18
owner: "@@TL"
status: validating
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: false
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@DEVOPS"
  - "@@QA"
task_scope:
  - 方案设计
  - 发布交付
  - 代码实现
risk_tags:
  - database-schema
  - production-config
  - permission
---

# SaaS 租户开户步进状态机（模式卡）

## 抽象问题定义

- abstract_problem: 多租户开通步骤长（建库、DDL、种子、管理员），易半成功；生产环境不能随意自动 DDL。
- failure_of_old_way: 一次性脚本无步骤状态；失败后人工不知道走到哪。
- goal_invariant: 每步可记录、可跳过已成功、可重试失败步；生产自动 DDL 默认关闭。

## 能力组件拆解

- `TenantStatus`: None/Initializing/Ready/Failed。  
- `StepTable`: 步骤名、状态、错误信息。  
- `Worker`: 定时扫描 Initializing 并 RunStep。  
- `DdlGuard`: 环境开关控制 ApplyDdl。

## 核心不变量

- Ready 仅在全部关键步成功后出现。  
- 已成功步不重复破坏性执行。  
- 人工灌库必须能与步骤表对齐。

## 算法蓝图

1. 创建租户 → Initializing。  
2. Worker：RegisterDb → ApplyDdl → Seed → Admin → Complete。  
3. 失败标记步骤与租户 Failed，支持 Retry。  
4. Ready 后才接受该租户业务流量（按解析策略）。

## 适配点

- hx-matching: `TenantInitWorkerService`、`Job_TenantInit`、`TenantLifecycleService`。

## 权衡

- 步骤表增加运维复杂度；换来可观测与可恢复。

## 验证清单

- 重入已成功步安全。  
- 关闭自动 DDL 时不会静默建库。  
- Failed → Retry 从失败步继续。

## 反模式

- 无步骤记录的「一把梭」开通。  
- 生产默认自动 DDL。  
- Ready 与真实库对象不一致仍放行流量。

## 证据

- evidence_refs:
  - HX.Matching.ServiceCore/MultiTenant/TenantInitWorkerService.cs
  - HX.Matching.Tasks/TaskScheduler/Job_TenantInit.cs
  - knowledge/code-summaries/hx-matching/cards/tenant-init-worker.md
  - projects/examples/hx-matching/flows-multitenant-and-review.md
- reusable_assets: 步进表驱动开通器
- non_reuse_reason_when_created_from_gap: 公共库缺租户开通步进模式

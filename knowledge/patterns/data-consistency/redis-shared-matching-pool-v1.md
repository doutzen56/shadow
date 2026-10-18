---
id: pattern-data-consistency-redis-shared-matching-pool-v1
title: Redis 共享匹配池（Hash + 分组有序索引）
domain: data-consistency
tags:
  - redis
  - matching-pool
  - multi-instance
projects:
  - hx-matching
maturity: validating
reuse_score: 82
last_verified_at: 2026-10-18
owner: "@@TL"
status: validating
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: false
role_scope:
  - "@@BA"
  - "@@BE"
task_scope:
  - 方案设计
  - 代码实现
risk_tags:
  - order-state
  - funds
  - cache-consistency
---

# Redis 共享匹配池（模式卡）

## 抽象问题定义

- abstract_problem: 多实例部署时，进程内挂单/匹配池无法共享，导致漏匹配或重复匹配。
- failure_of_old_way: 每实例一份内存字典。
- goal_invariant: 任意实例入池的候选对其他实例可见；取出与业务状态变更有明确并发约定。

## 能力组件拆解

- `EntityHashStore`: 订单/挂单快照存 Hash。  
- `PondSortedIndex`: 按池/金额等维度 Sorted Set 索引。  
- `BoundedScanner`: 限制扫描量，防止热点放大。

## 核心不变量

- 池是候选索引，不是资金账本；账本仍在 DB 事务。  
- 匹配成功必须推进持久状态，防止重复完整成交。  
- Redis 失败时匹配失败可重试，不可假装成功。

## 算法蓝图

1. 入池：写 Hash + 加索引。  
2. 匹配：按 Pond 取候选 → 业务规则过滤 → 事务落撮合结果。  
3. 出池/更新剩余量。  
4. 超时任务清理脏候选。

## 适配点

- hx-matching: `OrderMemoryStoreServices` + `MatchmakingService`。  
- 可迁到：跨实例库存池、简易挂单簿（非交易所级）。

## 权衡

- 实现相对简单；Redis 成为可用性依赖；强一致撮合需额外锁/版本号。

## 验证清单

- 双实例互见候选。  
- 并发匹配不双花（结合 DB 状态）。  
- 大池扫描有上限。

## 反模式

- 只改 Redis 不改订单状态。  
- 无界 SSCAN/ZRANGE 拖死 Redis。

## 证据

- evidence_refs:
  - HX.Matching.ServiceCore/Services/OrderMatchmakingServices/OrderMemoryStoreServices.cs
  - knowledge/code-summaries/hx-matching/cards/matchmaking-redis-pool.md
- reusable_assets: Hash + Sorted Set 池模板
- non_reuse_reason_when_created_from_gap: 公共库缺共享匹配池模式

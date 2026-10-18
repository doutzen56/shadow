---
summary_id: matchmaking-redis-pool
project_id: hx-matching
file_path: HX.Matching.ServiceCore/Services/OrderMatchmakingServices/OrderMemoryStoreServices.cs
file_hash: efa716e62f9256f0bb1447d3c807383a2812ca11aebecbaefd1f923d7102911b
symbol: OrderMemoryStoreServices
symbol_kind: type
role_scope:
  - "@@BA"
  - "@@BE"
  - reviewer
task_scope:
  - 代码实现
  - 方案设计
  - 缺陷定位
risk_tags:
  - order-state
  - funds
  - cache-consistency
last_seen: 2026-10-18
related_pattern: pattern-data-consistency-redis-shared-matching-pool-v1
---

# 撮合池 Redis 化深挖卡

## 职责

跨实例共享的代付撮合候选池：Hash 存订单快照，按 Pond 的 Sorted Set 做候选索引；配合 `MatchmakingService` 做金额/通道匹配。

## 契约

- 输入：入池/出池订单标识、Pond、金额等索引字段。
- 输出：按池批量取出的候选集合（有扫描上限防热点放大）。
- 失败契约：Redis 异常时撮合应失败可重试，不能假装匹配成功。

## 依赖

- Redis（业务 CSRedis）
- MQ：`hx.queue.order.matchmaking*` → `MatchmakingHostedService`
- `MatchmakingService.RechargeInMatchmaking` / `WithdrawInMatchmaking`

## 状态与副作用

- 多实例共享同一池，避免进程内内存池漏单/重复。
- 脏数据（过期未出池）会影响匹配质量，需与超时任务协同。

## 验证提示

1. 两实例同时入池/匹配，候选应对双方可见。
2. 匹配成功后订单不应仍可被其他实例重复完整撮合（结合业务锁/状态）。
3. 大池扫描耗时与上限配置。

## 风险说明

- Redis 成为撮合 SPOF。
- 锁与过期策略错误会导致池脏或超卖式匹配。

## 源码回读要求

文件内可能并存实现段/条件编译痕迹；改池结构前通读 `OrderMemoryStoreServices` 与 `MatchmakingService` 匹配段。

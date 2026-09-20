---
id: pattern-cache-redis-bloom-dual-write-v1
title: Redis Bloom 双写生命周期组件（去重能力可持续扩容）
domain: cache
tags:
  - redis
  - bloom-filter
  - dedup
  - dual-write
  - lifecycle
projects:
  - hx-matching
maturity: validating
reuse_score: 88
last_verified_at: 2026-09-20
owner: @@BE
status: candidate
---

# Redis Bloom 双写生命周期组件（模式卡）

## 抽象问题定义

- abstract_problem: 去重过滤器长期运行后会饱和，误判率持续升高，如何在不停机条件下持续扩容并保持去重语义。
- failure_of_old_way: 单位图 Bloom 长期使用只能越写越满，直接清空会丢历史去重能力，直接替换会出现窗口期漏判。
- goal_invariant: 热路径始终保留“命中即可能存在、未命中即确定不存在”的判定语义，且扩容/切换过程不破坏业务去重闭环。

## 能力组件拆解

- component_name: `BloomQueryAndWriteGate`
  - component_role: 对外提供 `ProbablyExists` / `CheckAndAdd` / `Add` 三个统一能力
  - input_contract: 过滤器名称 + 业务唯一键
  - output_contract: 返回“可能存在/确定不存在”并按状态决定是否写主/备过滤器
  - failure_contract: Redis 异常时降级返回“可能存在”，避免重复放行业务
  - observable_signal: Bloom 执行异常日志、二次校验异常日志
- component_name: `BloomLifecycleManager`
  - component_role: 驱动 Normal -> DualWrite -> Switching -> Normal 的生命周期刷新
  - input_contract: 当前填充率、双写天数、状态键
  - output_contract: 是否开启双写、是否完成主备切换
  - failure_contract: 刷新异常只记录结果并返回状态，不中断主链路
  - observable_signal: 生命周期刷新日志（阈值、填充率、状态前后）
- component_name: `AtomicLuaStateMachine`
  - component_role: 通过 Lua 保证检查写入与状态切换原子性
  - input_contract: 主键锚点、位索引、状态码
  - output_contract: 单次操作状态一致，不出现并发脏切换
  - failure_contract: 状态不匹配时拒绝切换（返回 0）
  - observable_signal: 并发刷新测试结果、状态键值

## 核心不变量

- state_invariants:
  - `Normal`：只查主、只写主
  - `DualWrite`：查主，写主+备
  - `Switching`：查询保守返回“可能存在”
- data_invariants:
  - 主键与备键始终同槽可原子处理
  - 主备切换后旧主转备份键，备升级为新主
- timing_invariants:
  - 主过滤器填充率达阈值（0.7）才允许开启双写
  - 达到双写持续天数才允许执行切换
- security_invariants:
  - 不记录原始业务敏感值，只记录过滤器与状态信息

## 关键方法 / 逻辑节点

- key_methods:
  - method_name: `ExecuteAsync`
    - responsibility: 统一执行检查/写入，并处理二次校验与降级
    - pre_condition: item 非空，Redis 客户端可用
    - post_condition: 返回去重判定结果
    - idempotency_rule: 同键重复写入返回“可能存在”
  - method_name: `RefreshLifecycleAsync(filter)`
    - responsibility: 刷新单过滤器生命周期并返回状态变化
    - pre_condition: 过滤器配置存在
    - post_condition: 状态按阈值与双写时长推进
    - idempotency_rule: 并发刷新只允许一次真正状态推进
  - method_name: `StartDualWriteAsync / SwitchPrimaryAndSecondaryAsync`
    - responsibility: 启动双写 / 完成主备切换
    - pre_condition: 当前状态符合预期
    - post_condition: 状态与主备键一致更新
    - idempotency_rule: 状态不匹配直接拒绝重复切换
- key_decision_nodes:
  - node: 命中 Bloom 后是否二次校验
    - decision_input: 是否提供 `secondaryValidateAsync`
    - decision_output: 直接判定 / 执行二次确认
  - node: 是否写入备过滤器
    - decision_input: 当前状态是否 `DualWrite/Switching`
    - decision_output: 只写主 / 主备双写
  - node: 是否切换主备
    - decision_input: 已双写天数是否达阈值
    - decision_output: 继续双写 / 执行切换

## 核心实现蓝图（平台无关）

- algorithm_blueprint:
  1. 对业务唯一键做多哈希定位位图位置。
  2. 通过原子脚本读取位状态，必要时写位；输出存在性判定。
  3. 若命中且有二次校验委托，执行业务真值校验。
  4. 定时任务刷新生命周期：检查填充率 -> 开启双写 -> 到期后主备切换。
  5. 切换期间采用保守查询策略，避免“误判不存在”造成漏拦截。
- adapter_points:
  - Redis 客户端实现（Lua/EVAL、BITOP、BITCOUNT）
  - 业务唯一键构造规则
  - 生命周期调度器（Job/Crontab）
- non_negotiable_flow:
  - 原子检查写入 -> 状态机双写 -> 到期切换 -> 保守降级

## 参数与取舍

- key_parameters:
  - `ExpectedItems`
  - `FalsePositiveRate`
  - `DoubleWriteDays`
  - `LifecycleFillRatioThreshold`（当前实现 0.7）
- tradeoffs:
  - 双写阶段增加写放大，但避免强切换丢去重能力
  - 降级“可能存在”会提高误拦截概率，但保护核心幂等安全
- anti_patterns:
  - 不做生命周期刷新让位图长期饱和
  - 切换期间仍按普通未命中处理
  - 在热路径直接清空主过滤器

## 验收断言

- verification_checklist:
  - 填充率未达阈值不进入双写
  - 达阈值后进入双写，重复写入判定稳定
  - 并发刷新只允许一次成功进入双写
  - 双写天数满足且备填充率达标后只发生一次主备切换
- rollback_strategy:
  - 状态异常时保守回到 `Normal`，保留备份键，暂停切换任务
- performance_cost:
  - 双写阶段 Redis SETBIT 成本提升；换取持续可用的去重能力

## 来源证据（代码仅引用）

- source_projects: hx-matching
- evidence_refs:
  - HX.Matching.Infrastructure/BloomFilter/IBloomFilter.cs
  - HX.Matching.Infrastructure/BloomFilter/RedisBloomFilter.cs
  - HX.Matching.Infrastructure/BloomFilter/BloomFilterConfig.cs
  - src/TestIntegration/BloomFilter/RedisBloomFilterTests.cs
- reusable_assets:
  - asset_id: cache-redis-bloom-dualwrite-lifecycle-v1
    asset_type: cache-strategy
    scenario: 高并发幂等/去重场景的长期运行过滤器
    input_contract: filterName + item + 可选二次校验函数
    output_contract: 命中语义稳定、生命周期可推进
    failure_contract: Redis 故障时保守返回“可能存在”
    dependencies: Redis 位图 / 定时任务 / 日志系统
    observability: 生命周期日志、状态键、并发刷新结果
    source_repo: HX.MatchingApi
    file_path:
      - HX.Matching.Infrastructure/BloomFilter/RedisBloomFilter.cs
      - src/TestIntegration/BloomFilter/RedisBloomFilterTests.cs
    symbol:
      - RedisBloomFilter.ExecuteAsync
      - RedisBloomFilter.RefreshLifecycleAsync
      - RedisBloomFilter.StartDualWriteAsync
      - RedisBloomFilter.SwitchPrimaryAndSecondaryAsync
    snippet_or_ref:
      - 摘要：Lua 原子检查+按状态写主备位图
      - 摘要：填充率阈值触发双写，双写天数到期切换

## 不可直接复用部分

- coupling_to_project:
  - 当前过滤器名称、Redis Key 前缀、阈值策略来自项目内固定配置
- required_replacements:
  - 替换业务唯一键构造规则
  - 按目标系统调整 expectedItems / FPR / 双写天数

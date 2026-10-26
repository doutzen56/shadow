---
id: pattern-reliability-hotpath-admission-gate-v1
title: 热路径进程内准入闸门（快速失败）
domain: reliability
tags:
  - admission-control
  - backpressure
  - hot-path
  - semaphore
projects:
  - hx-matching
maturity: validating
reuse_score: 85
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
  - 代码实现
risk_tags:
  - funds
  - gateway
  - order-state
---

# 热路径进程内准入闸门（模式卡）

## 抽象问题定义

- abstract_problem: 开放 API 热路径在下游（DB/Redis/外部 HTTP）变慢时，请求在线程/连接池里排队，直到调用方超时，成功率与可观测性双崩。
- failure_of_old_way: 无限接受请求或与后台拉单共用同一配额，导致 submit 被拖死。
- goal_invariant: 满载时立刻拒绝并返回可识别的繁忙语义，保护调用方超时预算与进程稳定性。

## 能力组件拆解

- component_name: `ProcessLocalAdmissionGate`
  - component_role: 按业务类型提供 TryEnter/Exit
  - input_contract: 无业务体；可选环境变量覆盖上限
  - output_contract: 占用成功或立即失败
  - failure_contract: 失败不进入写单/发消息
  - observable_signal: 满载 Warn 日志、拒绝计数

- component_name: `IsolatedGateByWorkload`
  - component_role: 不同工作负载使用独立闸门（如下单 vs 拉单）
  - input_contract: 工作负载类型
  - output_contract: 互不挤占的配额
  - failure_contract: 某一类打满不影响另一类的进入判定逻辑本身

## 核心不变量

- state_invariants: 未 Exit 不得泄漏名额；异常路径必须 finally 释放。
- data_invariants: 闸门不代替业务幂等与资金一致性。
- timing_invariants: Wait(0) 或等价非阻塞获取；禁止长时间阻塞等待名额。
- security_invariants: 拒绝响应不泄露内部配额细节给不可信客户端（可统一繁忙码）。

## 关键方法 / 决策节点

- key_methods: `TryEnter*` / `Exit*`；配置解析 `Resolve(env, default)`。
- key_decision_nodes: 进入重逻辑前；与下游限流/熔断的分工（闸门管进程接纳，熔断管依赖失败）。

## 算法蓝图

1. 请求进入热路径。  
2. TryEnter：成功则执行业务；失败立即返回繁忙。  
3. finally Exit。  
4. 按实例数与共享资源容量校准上限。

## 适配点

- adapter_points: 限额来源（环境变量/配置中心）；拒绝响应映射到对外错误码。
- source_projects: hx-matching → `MerchantSubmitAdmissionGate`、`CashierRechargePullCoordinator`、`GateLimitOptions`。

## 不可妥协流程

- non_negotiable_flow: 拒绝路径不得写订单终态或扣款；必须释放已占用名额。

## 关键参数

- key_parameters: 每工作负载 MaxInFlight；多实例时的全局换算。

## 权衡

- tradeoffs: 实现简单、延迟低；非全局精确限流。需要全局精确时要改分布式令牌/网关限流。

## 失败与回滚

- failure_contracts: 配置错误导致上限过小 → 可用性下降；过大 → 保护失效。
- rollback_strategy: 环境变量回调默认值；功能开关绕过闸门仅作紧急手段并监控。

## 验证清单

- verification_checklist:
  - 打满后 P99 拒绝延迟应远小于调用方超时。
  - 异常路径不泄漏信号量。
  - 两类闸门互不挤占的压测。

## 性能 / 安全 / 反模式

- performance_cost: 原子增减，可忽略；收益是避免超时雪崩。
- security_invariants: 见上。
- anti_patterns: 阻塞等待名额；与慢拉单共用一个闸门；把闸门当幂等。

## 证据与复用资产

- evidence_refs:
  - HX.Matching.ServiceCore/Services/MerchantSubmitAdmissionGate.cs
  - HX.Matching.ServiceCore/Services/OrderMatchmakingServices/CashierRechargePullCoordinator.cs
  - knowledge/code-summaries/hx-matching/cards/merchant-submit-admission-gate.md
- reusable_assets: 进程内 Semaphore 闸门 + 环境变量校准模板
- non_reuse_reason_when_created_from_gap: 公共库尚无「热路径快速失败闸门」抽象卡

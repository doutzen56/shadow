---
id: pattern-reliability-callback-ack-then-async-query-v1
title: 上游回调 ACK 后异步查单确认终态
domain: reliability
tags:
  - callback
  - async-query
  - payment-gateway
  - retry
projects:
  - hx-matching
maturity: validating
reuse_score: 90
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
  - reviewer
task_scope:
  - 方案设计
  - 代码实现
  - 事故处理
risk_tags:
  - callback
  - funds
  - order-state
  - idempotency
  - mq
---

# 上游回调 ACK 后异步查单确认终态（模式卡）

## 抽象问题定义

- abstract_problem: 支付渠道回调报文异构、到达不可靠；在 HTTP 回调里同步解析并改资金/订单终态，极易超时丢 ACK、重复回调或状态错乱。
- failure_of_old_way: 回调线程内直接改单；渠道因未收到 ACK 疯狂重放。
- goal_invariant: HTTP 阶段快速合法 ACK；权威终态以查单（或等价对账查询）结果为准，并可有限重试。

## 能力组件拆解

- component_name: `CallbackAckEndpoint`
  - component_role: 鉴权（如 IP 白名单）+ 返回渠道 ACK
  - failure_contract: 鉴权失败不入队；鉴权成功尽量先 ACK 再异步

- component_name: `QueryWorkQueue`
  - component_role: 持久化查单任务；支持延迟重试与去重窗
  - failure_contract: 超限告警/丢弃策略明确，不无限重试打爆渠道

- component_name: `QueryToStateMachine`
  - component_role: 查单结果映射到订单状态机与资金动作
  - failure_contract: 非法迁移拒绝；幂等处理重复查单

## 核心不变量

- state_invariants: 回调 HTTP 成功 ≠ 业务成功；业务成功只来自查单/状态机。
- timing_invariants: ACK 时延远小于渠道超时；重试有上限。
- security_invariants: 校验来源；日志脱敏。

## 关键方法 / 决策节点

- key_decision_nodes: 是否信任回调字段直接改终态（默认否）；重试退避用 TTL 死信回主队列还是延迟插件。

## 算法蓝图

1. 收回调 → 验来源 → ACK。  
2. 入查单队列（短窗去重）。  
3. Worker 调渠道查单 API。  
4. 状态机推进 + 下游通知。  
5. 失败 → 有限次延迟重试 → 告警。

## 适配点

- hx-matching: `ChannelCallback` + `ChannelOrderQueryQueueBackgroundService` + Channels 查询方法。
- 可迁到：任意「上游回调语义弱、以查询为准」的网关对接。

## 不可妥协流程

- 不得因业务处理慢而不 ACK（在已鉴权前提下）。  
- 不得跳过状态机直接按回调字段入账。

## 权衡

- 增加查单 QPS 与最终一致性窗口；换来 ACK 稳定与协议隔离。

## 失败与回滚

- failure_contracts: 重试耗尽卡单；查单与本地状态竞态。
- rollback_strategy: 暂停消费者；人工按查单结果补单；修正状态机后再放量。

## 验证清单

- 回调接口在查单完成前返回。  
- 查单失败进入重试并有上限。  
- 重复回调不导致重复入账。

## 反模式

- anti_patterns: 回调里同步调多级下游；无去重的无限重试；把 ACK 失败当业务失败回滚资金。

## 证据与复用资产

- evidence_refs:
  - HX.Matching.Admin.WebApi/Controllers/System/CallBack/ChannelCallback.cs
  - HX.Matching.ServiceCore/Services/ExternalChannels/ChannelOrderQueryQueueBackgroundService.cs
  - HX.Matching.Infrastructure/Messaging/RabbitMqTopology.cs（ChannelOrderQuery*）
  - knowledge/code-summaries/hx-matching/cards/channel-callback-ack-query.md
- reusable_assets: ACK 端点模板 + 查单重试队列拓扑
- non_reuse_reason_when_created_from_gap: 与 betwin「回调幂等清单」互补——本卡强调 ACK/查单分离，而非仅幂等清单

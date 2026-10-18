---
id: pattern-messaging-webhook-mq-local-fallback-v1
title: 出站 Webhook：MQ 优先 + 本机降级 + 计数重试
domain: messaging
tags:
  - webhook
  - rabbitmq
  - fallback
  - retry
projects:
  - hx-matching
maturity: validating
reuse_score: 86
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
task_scope:
  - 方案设计
  - 代码实现
  - 事故处理
risk_tags:
  - callback
  - mq
  - retry
  - funds
---

# 出站 Webhook：MQ 优先 + 本机降级 + 计数重试（模式卡）

## 抽象问题定义

- abstract_problem: 业务终态已提交后必须通知外部商户；MQ 或消费进程抖动时不能丢通知，也不能把通知失败回滚成业务失败。
- failure_of_old_way: 仅本机内存队列（重启丢）或仅同步 HTTP（拖垮热路径）。
- goal_invariant: 热路径只负责“投递意图”；可靠投递靠持久队列；短暂 MQ 故障有降级；重试可跨实例。

## 能力组件拆解

- `IntentPublisher`: 优先 Publish 持久队列；失败写入本机降级队列。
- `CompetingConsumer`: 多实例竞争消费并 HTTP 回调。
- `RetryCounter`: 失败再次入队并递增计数，超限告警。

## 核心不变量

- 业务终态与 Webhook 成功解耦。  
- 重试不得依赖单机内存作为唯一真相。  
- 签名与脱敏在出站层统一处理。

## 算法蓝图

1. 终态事务提交成功。  
2. 组装回调报文并签名。  
3. TryPublish MQ → 否则 LocalEnqueue。  
4. Consumer HTTP；失败则带 CallbackCount 再 Publish。

## 适配点

- hx-matching: `OrderCallbackBackgroundService`、`HxCallbackService`、`MerchantNotifyCallback*`。

## 权衡

- 本机降级提高可用性，但重启仍可能丢；应用层需监控 MQ 健康，降级只是缓冲。

## 失败与回滚

- 超限：人工补发工具/运营重推。  
- 回滚业务终态：禁止因 Webhook 失败自动回滚资金。

## 验证清单

- MQ 正常路径收到回调。  
- Publish 失败走降级并有日志。  
- 商户错误响应触发计数重试。

## 反模式

- 在开放 API 进程同步打商户 HTTP。  
- 重试只活在单机内存。  
- 回调失败自动把成功订单改失败。

## 证据

- evidence_refs:
  - HX.Matching.ServiceCore/Services/OrderCallbackBackgroundService.cs
  - knowledge/code-summaries/hx-matching/cards/merchant-notify-callback.md
- reusable_assets: 出站意图发布器 + 竞争消费重试
- non_reuse_reason_when_created_from_gap: 公共库缺「出站 Webhook 投递拓扑」模式卡

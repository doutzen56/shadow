---
summary_id: channel-callback-ack-query
project_id: hx-matching
file_path: HX.Matching.Admin.WebApi/Controllers/System/CallBack/ChannelCallback.cs
file_hash: 57cb9c753fd6e4c3cb60378cd1e6809bf8e66461d59c0516d34bd7d343d49105
symbol: ChannelCallback.NotifyCoreAsync / VerifySessionRechargeCoreAsync
symbol_kind: method
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@DEVOPS"
  - reviewer
task_scope:
  - 代码实现
  - 缺陷定位
  - 事故处理
risk_tags:
  - callback
  - funds
  - order-state
  - idempotency
  - mq
last_seen: 2026-10-18
related_pattern: pattern-reliability-callback-ack-then-async-query-v1
---

# 渠道回调 ACK + 异步查单深挖卡

## 职责

渠道 HTTP 回调只做 IP 校验与 ACK，把「以查单为准」的终态判定丢给异步队列，避免同步解析异构报文导致超时丢 ACK。

## 契约

- 输入：渠道回调请求（各供应商字段不一）。
- 输出：渠道要求的成功/失败 ACK 字符串（适配器 `GetCallbackSuccessValue/FailValue`）。
- 失败契约：入队失败要有日志/降级；不得在未 ACK 时长时间阻塞。

## 依赖

- `ChannelOrderQueryQueueBackgroundService`（入队、短窗去重、本机降级）
- MQ：`hx.queue.channel.order.query` + TTL 重试队列
- 适配器查询：`RechargeOrderQuery` / `WithdrawOrderQuery`
- 终态：`EnterPriseMatchingCallbackHandler`

## 状态与副作用

1. HTTP：白名单 → ACK → Enqueue。
2. 消费：查单 → 按状态机推进；代付覆盖回写有竞态注释，改前必读 Handler。
3. 重试：RetryCount 超限后丢弃并打日志（无人工死信捞取队列）。

## 验证提示

1. 回调接口应在查单完成前就返回 ACK。
2. 故意让查单失败，确认会进入重试队列并最终有上限。
3. 同单一短窗重复回调不应放大为多次危险写。

## 风险说明

- 查单与回调竞态依赖订单状态机严谨性。
- 重试耗尽后需人工/告警兜底，否则会卡单。

## 源码回读要求

新增渠道或改回调语义前，回读 `ChannelCallback`、查单后台服务与对应 Channels 适配器。

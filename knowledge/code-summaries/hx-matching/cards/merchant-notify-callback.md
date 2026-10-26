---
summary_id: merchant-notify-callback
project_id: hx-matching
file_path: HX.Matching.ServiceCore/Services/OrderCallbackBackgroundService.cs
file_hash: c1aa33d2077696666298982e3657c3500191e7f49a3e4cb320b46d2b13c12ff4
symbol: OrderCallbackBackgroundService / HxCallbackService
symbol_kind: type
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@DEVOPS"
  - "@@QA"
task_scope:
  - 代码实现
  - 缺陷定位
  - 事故处理
risk_tags:
  - callback
  - funds
  - order-state
  - mq
  - retry
last_seen: 2026-10-18
related_pattern: pattern-messaging-webhook-mq-local-fallback-v1
---

# 商户 Notify 回调投递深挖卡

## 职责

业务终态写完后，可靠通知商户 NotifyUrl：优先持久化 MQ，MQ 不可用时降级本机队列；消费侧按计数重试。

## 契约

- 输入：订单终态上下文、回调 URL、签名字段（`HxCallbackService` MD5）。
- 输出：商户 HTTP 接受或进入重试。
- 失败契约：不得因回调失败回滚已成功的资金/订单终态；靠重试与运营补偿。

## 依赖

- `IMerchantNotifyCallbackBus` / 队列 `hx.queue.merchant.notify.callback`
- Quartz：`MerchantNotifyCallbackConsumerHostedService`
- 开放口不注册主消费者（只发）

## 状态与副作用

- Publish 成功：跨实例竞争消费。
- Publish 失败：入本机内存/本地队列 → 进程重启可能丢失。
- 重试：再次 Publish 并递增 `CallbackCount`。

## 验证提示

1. MQ 正常时，终态后商户应收到回调。
2. 模拟 MQ 发布失败，确认走本机降级且有日志。
3. 商户返回非成功时，确认会按计数重试而非静默丢弃。

## 风险说明

- 本机降级不是跨机可靠投递。
- 幂等依赖商户侧与回调报文设计；重复通知要可接受。

## 源码回读要求

改回调签名、重试上限或宿主注册前，回读 `HxCallbackService` 与 Quartz 消费者注册。

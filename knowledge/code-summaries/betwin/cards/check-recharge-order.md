---
summary_id: check-recharge-order
project_id: betwin
file_path: Library/BetWin/Agent/User/UserAgent-Money.cs
file_hash: e1ee9931d768eb4bcc7dff6b68af6a6db3e06776fd5842c978ff73502fdcfc5b
symbol: UserAgent.CheckRechargeOrder
symbol_kind: method
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@DEVOPS"
  - reviewer
task_scope:
  - 缺陷定位
  - 事故处理
  - 代码审查
  - 方案设计
risk_tags:
  - funds
  - callback
  - idempotency
  - order-state
last_seen: 2026-10-18
---

# CheckRechargeOrder 深挖卡

## 职责

充值订单到账：把 `usr_RechargeOrder` 标为已支付，调用 `MoneyAgent.AddMoneyLog` 加钱，并触发首充/活动等副作用。

## 契约

- 输入：订单号、网关单号、实际到账金额、可选手续费/成本、备注、支付账号。
- 输出：外层方法成功返回 true 并 Commit；失败 Rollback 返回 false。
- 失败契约：金额 ≤ 0、订单不存在、已入账、网关单号冲突时应返回 false。当前实现中 catch 后仍返回 true，与“失败不得当成功”冲突。

## 依赖

- DB：`usr_RechargeOrder`、资金存储过程 `usr_AddMoneyLog`
- Redis：`MoneyCaching`（事务回调刷新）
- ES：无直接依赖
- MQ：无直接依赖
- 第三方：支付回调链经 `PaymentAgent` / Gateway Payment 进入本方法

## 状态与副作用

- 订单：`IsPay=true`、`Paid`/`Fee`/`Cost`/`SystemID`/`PayAt`/`Count` 更新。
- 资金：写入充值流水并增加余额。
- 活动：首充、充值活动、Telegram/Socket 通知等可能在事务回调或后续逻辑中触发。
- `IsDeleted` 在内存置 false，但 Update 字段列表未包含，删除标记可能不落库。

## 验证提示

1. 同一订单回调两次：第二次应失败且余额不变。
2. 回调金额大于订单金额：当前代码不会拒绝，需对账场景覆盖。
3. 订单更新成功后人为抛错：观察是否仍 Commit、第三方是否收到成功。
4. 已删除订单回调：检查 `IsDeleted` 最终库值。

## 风险说明

- 金额校验被注释（约 374-377 行）。
- catch 吞异常后 `return true`（约 632-637 行），外层照样 Commit。
- 更新不带 `WHERE IsPay = 0` 条件，并发双回调主要靠业务判断与流水去重。
- 关联事故：`inc-2026-10-18-betwin-recharge-callback-swallow`。

## 源码回读要求

改充值到账、回调应答或活动发放前，必须回读本文件当前实现；本卡只做定位。

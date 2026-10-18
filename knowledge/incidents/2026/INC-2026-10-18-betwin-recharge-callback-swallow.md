---
id: inc-2026-10-18-betwin-recharge-callback-swallow
title: BetWin 充值回调吞异常可能导致已到账未加钱
doc_type: incident
projects:
  - betwin
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: false
risk_tags:
  - funds
  - callback
  - idempotency
---

# INC-2026-10-18：BetWin 充值回调吞异常可能导致已到账未加钱

## 现象

支付回调处理过程中，如果订单已更新为已支付，但后续加钱或活动逻辑抛错，方法仍返回成功；外层事务提交，第三方也收到成功应答，不会再重试。

## 证据

- `Library/BetWin/Agent/User/UserAgent-Money.cs`
  - `CheckRechargeOrder` 外层事务：失败才 Rollback，成功路径 `Commit`（约 308-321 行）。
  - 金额与订单比对被注释（约 374-377 行）。
  - `IsDeleted` 在内存清掉，但不在 Update 字段列表中，删除标记可不落库（约 397、409 行）。
  - catch 记录错误后仍 `return true`（约 632-637 行）。

## 影响

- 影响对象：会员余额、充值订单 `usr_RechargeOrder`、第三方支付对账。
- 触发条件：回调入账过程中，订单更新成功后任意未处理异常。
- 失败后果：订单显示已到账，余额未增加；支付通道不再重推。

## 相关风险（同链路）

1. 回调金额不与订单金额比对。
2. 部分签名类型 `None`、个别 Special 插件弱验签。
3. 入账失败时 `ShowCallback` 仍可能给第三方成功应答（见 `PaymentAgent.ShowCallback`）。
4. 回调日志写入原始报文和全部请求头，可能含签名与敏感头。

## 临时缓解

- 对账：按“已支付但无充值流水”扫描补偿。
- 新对接渠道禁止使用 `None` 验签。
- 修复前不要把“回调返回成功”当成入账成功的唯一依据。

## 建议修复方向

1. catch 后必须返回 false，并 Rollback。
2. 恢复金额校验，明确大于订单金额时的处理规则。
3. 条件更新：`WHERE IsPay = 0`。
4. 只有订单更新和 `AddMoneyLog` 都成功，才给第三方成功应答。

## 沉淀

- 模式：`pattern-payment-callback-idempotency-checklist-v1`
- 模式：`pattern-money-agent-addmoneylog-v1`

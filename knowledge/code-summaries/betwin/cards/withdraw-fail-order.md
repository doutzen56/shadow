---
summary_id: withdraw-fail-order
project_id: betwin
file_path: Library/BetWin/Agent/Users/WithdrawOrderAgent.cs
file_hash: c5fccf1e85581945ed30807a3849f8f34b24e88f11a7126c5b79a0f06bc2af80
symbol: WithdrawOrderAgent.CheckFailOrder / CheckWithdrawOrder
symbol_kind: method
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@DEVOPS"
  - reviewer
task_scope:
  - 缺陷定位
  - 事故处理
  - 方案设计
  - 代码审查
risk_tags:
  - funds
  - callback
  - order-state
  - idempotency
last_seen: 2026-10-18
---

# 提现失败回等待与成功核对深挖卡

## 职责

代付失败时把订单从付款中打回等待；成功核对时解锁、扣本金/手续费并置完成。

## 契约

- 输入：订单号、渠道号（失败路径）；或已加载的 `WithdrawOrder`（成功路径）。
- 输出：失败路径最终 `FaildMessage("订单回调失败")` 但仍可能已把状态改为 Wait；成功路径完成资金动作。
- 失败契约：渠道不一致或非 InPayment 应拒绝；成功路径已 Finish 应拒绝。当前失败路径不解锁（注释掉，1012 需求）。

## 依赖

- DB：提现订单、提现日志、资金锁定表
- Redis：钱包缓存（经 MoneyAgent）
- ES：无直接依赖
- MQ：无直接依赖
- 第三方：Gateway Withdraw 回调 / 查单

## 状态与副作用

- `CheckFailOrder`：校验渠道与 InPayment、120 秒保护；解锁代码注释；`Status=Wait`。
- `CheckWithdrawOrder`：仅挡 Finish，更新不强制当前必须是 InPayment。
- 同文件其他失败入口（核对失败、查单失败）也有“回 Wait 且不解锁”的相同模式。

## 验证提示

1. 失败回调后订单是否可被再次提交代付。
2. 失败回 Wait 后晚到成功回调能否再次扣款。
3. 伪造失败码且签名错误时，上游 `ICallback` 是否仍改状态。
4. 成功回调渠道号与订单不一致时是否被拒绝。

## 风险说明

- 1012“失败回 Wait、保留锁定”若仍有效，必须配套失败验签、成功校渠道、条件更新。
- 关联事故：`inc-2026-10-18-betwin-withdraw-fail-back-to-wait`。
- 上游失败码先于验签：`Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/ICallback.cs`。

## 源码回读要求

改提现状态机或代付回调前必须回读本文件与 Withdraw `ICallback`。

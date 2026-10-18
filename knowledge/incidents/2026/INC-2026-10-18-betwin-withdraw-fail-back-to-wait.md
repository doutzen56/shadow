---
id: inc-2026-10-18-betwin-withdraw-fail-back-to-wait
title: BetWin 提现失败回调回等待且不释放锁定资金
doc_type: incident
projects:
  - betwin
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: false
risk_tags:
  - funds
  - callback
  - order-state
  - idempotency
---

# INC-2026-10-18：BetWin 提现失败回调回等待且不释放锁定资金

## 现象

代付失败回调、手工查单失败或后台核对失败时，订单从“付款中”回到“等待出款”，但锁定资金释放代码被注释。同时失败回调验签弱于成功路径，成功回调也不强制校验渠道。

## 证据

- `Library/GateWay/BetWin.Gateway.Withdraw/CallBackSign/ICallback.cs`
  - `CheckFailCode()` 命中后直接 `CallbackFail`，发生在验签之前（约 221-228 行）。
- `Library/BetWin/Agent/Users/WithdrawOrderAgent.cs`
  - `CheckFailOrder`：要求渠道一致且状态为 InPayment；解锁代码注释；状态改为 Wait（约 740-774 行，注释写明 1012 需求）。
  - `CheckWithdrawOrder`：仅拒绝已经是 Finish 的订单，更新不带“当前必须是 InPayment”条件（约 782-788 行）。
- `Library/BetWin/Agent/Users/MoneyAgent.cs`
  - `UnlockMoney` 在解锁后无条件执行 `UPDATE game_TransferOrder SET Status=4 WHERE OrderID=@SourceID`（约 814-818 行）。

## 影响

- 影响对象：提现订单、用户锁定资金、代付渠道、可能波及游戏转账单。
- 触发条件：失败回调、查单失败、后台 CheckWithdraw；或提现解锁 SourceID 与转账单号冲突。
- 失败后果：
  - 订单可再次出款，原渠道若实际已成功则双出款。
  - 失败后拒绝并解锁，晚到的成功回调可能无法扣款，造成通道已付、用户未扣。
  - 解锁误改游戏转账单状态，导致转账卡单难查。

## 业务待确认

“1012：失败回 Wait、保留锁定以便换渠道重试”是否仍是现行需求。若是，必须同时满足：

1. 失败回调也验签。
2. 成功回调校验渠道号。
3. 状态更新带期望状态条件。
4. 重复提交检测不能注释掉。

## 建议修复方向

1. 失败回调验签后再改状态。
2. 明确“换渠道重试”状态机，禁止无条件倒退。
3. 去掉或收窄 `UnlockMoney` 对 `game_TransferOrder` 的无条件更新。
4. 恢复代付重复提交检测。

## 沉淀

- 模式：`pattern-payment-callback-idempotency-checklist-v1`

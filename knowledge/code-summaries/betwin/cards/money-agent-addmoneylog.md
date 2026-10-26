---
summary_id: money-agent-addmoneylog
project_id: betwin
file_path: Library/BetWin/Agent/Users/MoneyAgent.cs
file_hash: 5e4f6a993c2132492adae2afa53bb153be242a96d8f857447e7360cbe8b1b9a2
symbol: MoneyAgent.AddMoneyLog / UnlockMoney
symbol_kind: method
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@DEVOPS"
  - "@@QA"
  - reviewer
task_scope:
  - 代码实现
  - 缺陷定位
  - 事故处理
risk_tags:
  - funds
  - idempotency
  - cache-consistency
  - order-state
last_seen: 2026-10-18
---

# MoneyAgent 加减钱与解锁深挖卡

## 职责

统一加减钱入口：查可用余额、按业务键查重流水、调用 `usr_AddMoneyLog`，提交后刷新 Redis 钱包；同时提供锁定/解锁。

## 契约

- 输入：用户、金额、币种、`MoneyType`、`SourceID`、`SubType`、描述、流水倍数等。
- 输出：成功则余额与流水一致；余额不足或流水已存在返回 false。
- 失败契约：存储过程 `exec == 0` 视为余额不足；不得把 false 当成功继续推进订单终态。

## 依赖

- DB：`usr_Money`、`usr_MoneyLog`（经 SP）、`usr_MoneyLock`、`game_TransferOrder`
- Redis：`MoneyCaching.SaveUserMoney` / `AddUserMoneyQueue` / 锁定金额缓存
- ES：无直接依赖
- MQ：报表队列 `ReportCaching.AddMoneyLogQueue`
- 第三方：无直接调用

## 状态与副作用

- `AddMoneyLog`：先 `ExistsMoneyLog(userId, type, sourceId, subType)`，再执行 SP；回调刷新钱包与流水队列；充值类可能清洗码并加洗码流水。
- `UnlockMoney`：解锁本地锁并减 `LockMoney`；随后无条件 `UPDATE game_TransferOrder SET Status=4 WHERE OrderID=@SourceID`，SourceID 冲突时会误改转账单。

## 验证提示

1. 同一 SourceID 连续加钱两次，第二次应返回“已存在资金记录”。
2. 余额不足扣款返回 false，且订单不得标成功。
3. 提现解锁与游戏转账 SourceID 碰撞时，检查转账单是否被改成 Status=4。
4. 事务回滚后 Redis 钱包不应被回调刷新。

## 风险说明

- 应用层先查后写，并发双写依赖 DB/SP 唯一约束兜底；约束缺失时仍可能双入账。
- `UnlockMoney` 与转账单耦合是跨域副作用。
- 关联模式：`pattern-money-agent-addmoneylog-v1`。

## 源码回读要求

任何动钱、锁定、解锁改动前必须回读本文件；存储过程本体不在仓库，行为以 SP 与本入口联调结果为准。

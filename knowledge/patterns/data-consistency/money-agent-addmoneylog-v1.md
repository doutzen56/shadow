---
id: pattern-money-agent-addmoneylog-v1
title: 统一加减钱入口（先查流水再写存储过程）
domain: data-consistency
tags:
  - funds
  - money-log
  - idempotency
  - redis-wallet
projects:
  - betwin
maturity: validating
reuse_score: 85
last_verified_at: 2026-10-18
owner: "@@BE"
status: validating
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: false
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
  - funds
  - idempotency
  - cache-consistency
---

# 统一加减钱入口（模式卡）

## 抽象问题定义

- abstract_problem: 充值、提现、游戏转账、返水、活动等多条链路都要改余额和写流水，分散入口容易重复加钱或漏记流水。
- failure_of_old_way: 各业务直接 UPDATE 余额或各写各的流水表，幂等口径不一致，复盘时对不上。
- goal_invariant: 所有动钱都必须经过同一个加减钱入口；同一 (用户, 类型, 业务单号, 子类型) 只能成功记一笔。

## 能力组件拆解

- component_name: `MoneyChangeGate`
  - component_role: 对外提供统一加钱/扣钱能力
  - input_contract: 用户、金额、币种、资金类型、业务单号 SourceID、子类型
  - output_contract: 成功则余额与流水一致；失败返回原因且不改余额
  - failure_contract: 余额不足、流水已存在、存储过程失败时返回 false，不抛给调用方当成功
  - observable_signal: 资金流水表、余额表、Redis 钱包缓存
- component_name: `MoneyLogDedup`
  - component_role: 写流水前按业务键查重
  - input_contract: TableID + UserID + Type + SourceID + SubType
  - output_contract: 已存在则拒绝再次入账
  - failure_contract: 仅靠先查后写时，并发下仍可能双写，需要 DB 唯一约束或存储过程内去重兜底
- component_name: `WalletCacheSync`
  - component_role: 事务提交后再刷新缓存钱包
  - input_contract: 最新余额快照
  - output_contract: Redis 钱包更新；失败不影响已提交的 DB 余额
  - failure_contract: 缓存与 DB 短暂不一致时，扣款判断仍以 DB 为准

## 状态不变量

1. DB 余额表是真值；缓存只作读加速。
2. 可用余额 = 总余额 − 锁定余额。
3. 锁定金额必须带锁定类型和业务单号，解锁时同一把锁只能解一次。
4. 流水 SourceID 必须能回溯到业务订单或活动日志。

## 不可妥协流程

1. 开事务。
2. 读 DB 余额并判断可用余额。
3. 按业务键查流水是否已存在。
4. 调用唯一的加减钱存储过程或等价原子更新。
5. 提交事务。
6. 提交后再写缓存、队列、通知。

## 关键参数

- SourceID：业务幂等键，规则必须稳定，补发与首次发放不能换键格式。
- MoneyType：资金类型枚举，决定报表和审计口径。
- SubType：同一订单多笔费用（本金/手续费）时使用。
- Currency：多币种站点必须显式传入，不能取“第一条钱包”。

## 取舍

- 好处：所有动钱可审计、可按 SourceID 追查。
- 代价：业务方必须遵守统一入口，不能为了方便直接 UPDATE。
- 并发：先查后写有窗口；生产要有唯一索引或存储过程内去重，否则只能靠行锁碰运气。

## 失败契约

- 流水已存在：返回失败或“已处理”，不能再加一次钱。
- 余额不足：事务回滚，订单状态不能标成功。
- 缓存写失败：可重试刷新，不能回滚已成功的 DB 入账。

## 回滚策略

- 业务冲正必须再记一笔反向流水，不能物理删除原流水。
- 锁失败或解锁失败时，订单不得进入“已完成”。

## 验证清单

1. 同一 SourceID 连发两次，余额只变一次。
2. 余额不足时订单与流水都不成功。
3. 提交后 Redis 钱包最终与 DB 一致，或明确以 DB 为准。
4. 锁定与解锁成对出现，解锁后可用余额恢复。

## 安全不变量

- 日志不得明文记录完整卡号、证件号、支付密码、签名串。
- 管理端手工加减钱必须有权限和操作日志。

## 反模式

- 业务代码直接 UPDATE 余额表。
- 补发时改掉 SourceID 格式，导致防重失效。
- 把“查缓存余额”当成扣款依据。
- 解锁时顺带无条件改其他业务表状态。

## 证据引用（BetWin）

- `Library/BetWin/Agent/Users/MoneyAgent.cs`：`AddMoneyLog`、`ExistsMoneyLog`、`LockMoney`、`UnlockMoney`
- `Library/BetWin.Caching/Users/MoneyCaching.cs`：`USERMONEY_{uid}_{currency}`
- `Library/BetWin.Common/Users/part/MoneyLog-enum.cs`：资金类型

## 适配点

- 新项目接入时：找到统一加减钱入口；没有的话先建，再迁各业务。
- BetWin 现状：入口已统一到 `MoneyAgent.AddMoneyLog`，但 `UnlockMoney` 会无条件更新 `game_TransferOrder`，属于项目特有风险。

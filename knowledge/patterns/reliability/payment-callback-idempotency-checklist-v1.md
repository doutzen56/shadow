---
id: pattern-payment-callback-idempotency-checklist-v1
title: 支付与代付回调幂等检查清单
domain: reliability
tags:
  - callback
  - idempotency
  - funds
  - gateway
projects:
  - betwin
maturity: validating
reuse_score: 90
last_verified_at: 2026-10-18
owner: "@@BA"
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
  - 方案设计
  - 代码实现
  - 缺陷定位
  - 事故处理
  - 代码审查
risk_tags:
  - funds
  - callback
  - idempotency
  - order-state
---

# 支付与代付回调幂等检查清单（模式卡）

## 抽象问题定义

- abstract_problem: 第三方会重复推送、乱序推送成功/失败回调；本地既要防重复入账/出款，又不能在异常时给对方假成功。
- failure_of_old_way: 只判断“订单已完成”就返回成功，或 catch 后一律当成功；失败回调不验签、不校验渠道。
- goal_invariant: 同一业务单在同一终态上只完成一次资金动作；未完成资金动作时不得向第三方返回成功。

## 能力组件拆解

- component_name: `CallbackAuthenticator`
  - component_role: 验签、校验成功/失败码、识别订单号和金额
  - input_contract: 原始回调报文、渠道配置
  - output_contract: 通过/失败/未验证；解析出订单号、网关单号、金额
  - failure_contract: 失败回调也要验签；`None` 签名类型不得用于生产资金渠道
- component_name: `OrderStateGuard`
  - component_role: 用当前状态做条件更新，不允许状态倒退
  - input_contract: 期望当前状态 + 目标状态
  - output_contract: 更新行数；0 表示已被别人处理或状态不符
  - failure_contract: 只按主键 Update、不带状态条件，等于没有并发保护
- component_name: `FundsOnceGate`
  - component_role: 入账/扣款只发生一次
  - input_contract: 订单号作为 SourceID
  - output_contract: 流水存在则停止；不存在才动钱
  - failure_contract: 订单已标成功但流水未写入时，必须能补偿或告警，不能继续回成功

## 不可妥协流程

1. 识别渠道与订单。
2. 成功和失败回调都验签。
3. 校验金额、币种、渠道号与订单一致。
4. 条件更新订单到处理中或终态。
5. 在同一事务内完成资金动作。
6. 只有资金与订单都成功，才给第三方成功应答。
7. 异常时返回失败或明确“处理中”，保留重试空间。

## 关键检查项

1. 金额是否与订单比对（允许手续费差时要写清规则）。
2. 网关单号是否按站点唯一。
3. 失败回调是否会把订单打回可再次出款的状态。
4. 失败后资金锁定是否释放，释放后晚到的成功回调如何处理。
5. 已删除/已拒绝订单是否仍能被回调入账。
6. 日志是否记录完整签名串、卡号、证件号。

## 取舍

- 严格验签与金额校验会增加对接成本，但比掉单、重复出款便宜。
- “失败回等待、保留锁定以便换渠道重试”可以是业务选择，但必须同时禁止：失败回调不验签、成功回调不校验渠道、状态无条件回退。

## 反模式

- catch 后返回 true，外层照样 Commit。
- 金额校验代码被注释仍上线。
- 失败回调先匹配失败码再跳过验签。
- 向第三方返回成功，但本地资金动作失败。

## 验证清单

1. 同一成功回调打两次，余额只变一次。
2. 伪造失败回调不能改变订单状态。
3. 成功回调金额被改大时被拒绝。
4. 事务中途抛错后，第三方收不到成功，或本地有明确补偿任务。

## 证据引用（BetWin）

- 充值：`UserAgent-Money.CheckRechargeOrder` 金额校验被注释；外层 catch 返回 true。
- 提现：`ICallback.Check` 失败码先于验签；`CheckFailOrder` 回 Wait 且不解锁。
- 入账防重：`MoneyAgent.ExistsMoneyLog`。

## 适配点

- 新支付对接时用本清单做审查门禁。
- BetWin 现有充值/提现链路有多处违反本清单，见同日事故卡。

---
summary_id: merchant-submit-admission-gate
project_id: hx-matching
file_path: HX.Matching.ServiceCore/Services/MerchantSubmitAdmissionGate.cs
file_hash: f557d8acb464dafffed18a456dbd0ade1bfb38b19b8d924c24af32064f170d98
symbol: MerchantSubmitAdmissionGate.TryEnterPayin / TryEnterPayout
symbol_kind: type
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@DEVOPS"
task_scope:
  - 代码实现
  - 缺陷定位
  - 方案设计
risk_tags:
  - funds
  - order-state
  - gateway
last_seen: 2026-10-18
related_pattern: pattern-reliability-hotpath-admission-gate-v1
---

# 商户下单准入闸门深挖卡

## 职责

代收/代付 submit 的进程内快速失败闸门：满载立刻拒绝，避免排队拖到商户 HTTP 超时；与收银台拉单闸门分立。

## 契约

- 输入：无业务参数；调用方在进入重逻辑前 `TryEnter*`，`finally` 里 `Exit*`。
- 输出：`true` 表示占到名额；`false` 表示应立即返回繁忙/失败。
- 失败契约：不得在 `false` 后继续写单或发 MQ。

## 依赖

- `GateLimitOptions.Resolve`（环境变量 `HX_GATE_PAYIN_INFLIGHT` / `HX_GATE_PAYOUT_INFLIGHT`）
- 进程内 `SemaphoreSlim`（非 Redis 全局锁）

## 状态与副作用

- 默认上限：代收 100、代付 60（单进程）；多实例全局容量≈单值×实例数，须按共享 Redis/SQL 容量校核。
- 仅打 Warn 日志，不抛异常。

## 验证提示

1. 打满并发后 submit 应快速失败，耗时远小于商户侧超时。
2. 正常结束后信号量可再次进入。
3. 与收银台拉单高压并行时，submit 仍应有独立配额。

## 风险说明

- 按进程计数，水平扩展后全局并发线性放大。
- 过严会压低峰值成功率；过松仍会拖垮连接池。

## 源码回读要求

调整限额或把闸门改成分布式前，必须回读本文件与 `CashierRechargePullCoordinator`。

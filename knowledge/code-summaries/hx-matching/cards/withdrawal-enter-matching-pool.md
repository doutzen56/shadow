---
summary_id: withdrawal-enter-matching-pool
project_id: hx-matching
file_path: HX.Matching.ServiceCore/Services/OrderMatchmakingServices/MatchmakingService.cs
symbol: MatchmakingService.WithdrawInMatchmaking
symbol_kind: method
role_scope:
  - "@@BA"
  - "@@BE"
  - "@@QA"
task_scope:
  - 缺陷定位
  - 方案设计
risk_tags:
  - order-state
  - funds
last_seen: 2026-10-26
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: true
reviewed_at: 2026-10-26
reviewed_by: user
related_decision: adr-2026-10-26-hx-matching-withdrawal-enter-pool-gate
related_pattern: pattern-data-consistency-redis-shared-matching-pool-v1
---

# 代付进撮合池准入深挖卡

## 职责

阶段 B：商户余额已扣后，决定进 Redis 共享池（NXH）还是推四方（SF）。

## 对象关系（必读）

| 对象 | 作用 |
| --- | --- |
| `HxMerchantUser.LabelId` | 解析等级 → 出售规则 LevelStr；**标签删了人未改挂 = 孤儿 Id → MatchSellRule 必失败** |
| `HxMerchantInfo.SalesRules` | 1 系统规则 / 2 商户规则 |
| 骗单 / 风控 Forbid* | skip 或拉空 |
| `GetPondConfigListByMerchantId` | 无代付配置时默认 PondId=1 |
| 预付费门禁 | 仅非 T0001；失败不落 SF |
| HostedService 失败回写 | SupplierFail + SystemRemark；常不写 OrderPlatformType |

## 排障顺序

1. 读单：MatchingPondId / RemainMatchingAmount / State / SystemRemark  
2. 生命周期 failReason（「出售单规则」优先）  
3. 用户 LabelId 是否仍在等级标签表/Redis  
4. 再查骗单、ForbidMatch、供应商费率  
5. **不要**用池配置表 Count=0 定责  

## 案例

GWTK2114698014701563904：等级标签删除 → miss 出售规则 → SF → 计算手续费失败。

## 源码回读

`WithdrawInMatchmaking`；`MatchSellRule`（userLabel==null）；`DeleteUserLabel`；`GetPondConfigListByMerchantId`；`MatchmakingHostedService` 失败分支。

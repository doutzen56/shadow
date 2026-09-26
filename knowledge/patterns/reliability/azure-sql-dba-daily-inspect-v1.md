---
id: pattern-reliability-azure-sql-dba-daily-inspect-v1
title: Azure SQL DBA 日巡检（Query Store 检出慢 SQL）
domain: reliability
tags:
  - azure-sql
  - query-store
  - dba
  - performance
  - hx-matching
projects:
  - hx-matching
maturity: validated
reuse_score: 90
last_verified_at: 2026-09-25
owner: @@X
status: active
---

# Azure SQL DBA 日巡检（模式卡）

## 抽象问题定义

- abstract_problem: 业务单量不大但 Azure SQL CPU/Log IO 经常顶满，需要可重复的日清手段，把「谁慢」变成可交给开发的 query_id 证据，而不是先加核。
- failure_of_old_way: 只看门户 Metrics 尖峰，无法落到具体 SQL / 表 / 代码调用点。
- goal_invariant: 日清输出必须带时段 + query_id + SQL 预览 + 等待提示；不替代应用侧定位，但能为开发开单。

## 资产位置（下次直接用）

| 资产 | 路径 |
|------|------|
| 可执行脚本 | `F:\shadow\projects\examples\hx-matching\ops\azure-sql-daily-inspect.sql` |
| 本模式卡 | `F:\shadow\knowledge\patterns\reliability\azure-sql-dba-daily-inspect-v1.md` |

**禁止**把脚本提交进业务仓 `HX.MatchingApi`（SimpX 边界：业务仓不托管运维脚本）。

## 脚本覆盖范围

1. 等待类型 Top20（业务重点看 `WRITELOG` / `SOS_SCHEDULER_YIELD` / `ASYNC_NETWORK_IO`；系统空闲类可忽略）
2. Query Store 近 24h Top CPU
3. Query Store 近 24h Top Duration（「谁比较慢」主榜）
4. Query Store 近 24h Top 执行次数
5. 缺索引建议（需人工裁剪，勿全加）
6. 表体积 Top30
7. 死锁 best effort

**刻意不含：** Running Requests / Blocking（不当下抓现行）。

## 使用步骤

1. SSMS / Azure Data Studio 连目标库（如 HxMatching）。
2. 确认 Query Store = Read Write。
3. 高峰后或凌晨 Job 后执行脚本；避开 CPU/Log 已 >80% 时段。
4. 优先把 **Top Duration** 的 `query_id + sql_preview` 开给开发。
5. 预览不够时用脚本文末「取完整 SQL」片段。
6. 用表名/条件在业务仓搜 C#，落到具体 Service（脚本本身不给调用栈）。

## 给开发的工单最小字段

```
时段 / query_id / total_duration_ms / exec_count / sql_preview
涉及表（从 SQL 猜）
请开发：对代码调用点 + 索引/降频/改写法
```

## 2026-09-25 一次真实检出摘要（hx-matching）

- 机器 32 vCore 对「十几万单/天」规格偏大；尖峰主因是写放大 + 重管理查询，不是单量本身。
- Top1 CPU：`query_id=26260234` → `hx_merchant_user_name_log` 的 `AnyAsync`  
  代码：`OrderMatchingReviewService.GetReviewInfoAsync`（审核详情，充值/代付各查一次是否改名）。  
  表缺 `(MerchantCode, MerchantUserId)` 类索引。
- 其它重头：贴群代付列表、审核列表、MatchingOrderIds 反查、偶发 60s 统计。
- 缺索引建议最高分常指向 `hx_order_withdrawal_202609(CallbackState+时间…)`、`hx_order_matching_review(FirstReviewBy)`。

## 能力组件拆解

- component_name: `DailyInspectSql`
  - component_role: 一次性产出多结果集巡检证据
  - input_contract: Azure SQL + Query Store ON
  - output_contract: wait / top cpu / top duration / top exec / missing index / table size / deadlock
  - failure_contract: QS 未开则给出 warning 行；死锁段失败可忽略改门户
  - observable_signal: 执行时长；QS 段过慢则缩窗口到 6h 或 TOP 10

## 核心不变量

- 日清看历史（Query Store），不把「当下会话」当主证据。
- 系统等待类型不作为升配依据。
- 缺索引建议必须与 Top 查询对得上才落地。

## 反模式

- 尖峰正中反复跑完整脚本。
- 把 SOS_WORK_DISPATCHER 等空闲等待当故障。
- 缺索引建议 30 条全加。
- 只升配不对照 query_id。

## 验证方式

- 跑通脚本；Top Duration 有行。
- 任选一 query_id 能拉出完整 SQL 并在代码中定位到调用点。

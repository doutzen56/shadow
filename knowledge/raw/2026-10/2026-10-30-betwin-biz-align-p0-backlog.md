---
id: raw-2026-10-30-betwin-biz-align-p0
title: BetWin 已迁接口业务对齐 P0 待办（先记住，后于命名重构）
doc_type: raw
projects:
  - simpx-net
  - betwin
lifecycle_status: raw
confirmed_by: "@@TL"
user_reviewed: false
evidence_refs:
  - upgrade docs/07-业务对齐审查-已迁接口.md
  - 会话 2026-10-30：用户要求按旧版业务逻辑实现，并先记住；优先改命名问题
---

# BetWin 业务对齐 P0（挂起，命名改完再做）

证据见 `upgrade docs/07-业务对齐审查-已迁接口.md`。当前已迁接口是 V1 子集，**不是**旧站全量等价。

## P0（必须按旧逻辑补）

1. 注册邀请链：InviteCode → 域名邀请 → Setting.InviteCode → AgentID / `AddUser` 语义。
2. 会员登录：失败次数与错密累计、用户名密码互含、单钱包币种、黑名单手机等。
3. 站点后台登录：错密累计与自动锁定（旧 FaildAgent / 锁定语义）。

## 顺序

用户 2026-10-30：先记住；优先命名 → 已完成。同日「按 P0 开工」并要求总后台/管理后台 Vue 登录页同步。

## 实现进度（会话）

- 后端：会员登录 P0、站点后台错密锁定、注册邀请主链 — 已改代码；宿主冒烟 11/11（`orchestration/runs/2026-10-30-betwin-p0p1/`）。
- 前端：Admin `legacyAccount` + SuperAdmin 兼容登录；登录后 `/admin/info/menu` → 动态路由（占位页）。
- P1：Setting 兼容 querystring；列表/详情列扩；Level/InitLevel；marklist 合并 UserLog。
- 未闭合：代理域限制、邀请活动事件细分支、业务 Vue 页替换占位、有权限账号实改 Level。

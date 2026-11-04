---
id: adr-2026-10-30-betwin-migrate-io-parity
title: BetWin 迁移：业务逻辑与输入输出必须与旧版一致
doc_type: decision
projects:
  - simpx-net
  - betwin
lifecycle_status: confirmed
confirmed_by: user
user_reviewed: false
risk_tags:
  - public-contract
  - compatibility
evidence_refs:
  - 会话 2026-10-30：用户 @@X「迁移业务的时候，业务逻辑，输入输出要跟旧版一模一样，迁移过程中这是硬性规定」
  - 会话 2026-10-31：用户 @@X「都完善一下，记住规则，业务上要对其旧版」（对齐旧版）
  - 会话 2026-10-31：用户 @@X「记住：后端业务和前台管理界面操作以及功能，都要对齐旧版，admin对应 web.admin superadmin对应web.system」
  - 会话 2026-11-02：用户 @@X「接口路径完全按照旧版来，1比1还原」「不要搞什么 wavegaps.cs」「旧项目是线上项目，所以业务要1比1还原，这是个硬性规则」
  - upgrade docs/AGENT-迁移硬约束.md A0/A3/B20/B23/B24
  - upgrade docs/拍板-2026-11-02-路径1比1与禁WaveGaps.md
  - knowledge/decisions/2026/ADR-2026-10-31-betwin-admin-ui-ops-parity.md
---

# ADR-2026-10-30：迁移期业务逻辑与 I/O 对齐旧版

- 日期：2026-10-30（2026-11-02 修订：路径 1:1、禁 WaveGaps、线上升级硬还原）
- 范围：SimpX.Net 上 BetWin 业务迁移（`feature/betwin1.0`）
- 状态：生效
- 来源：用户 `@@X` 硬性规定

## 决定

1. **本仓是线上旧项目升级**。迁移过程中每一刀的 **路径、动作/方法名、业务逻辑、入参、出参必须与旧版 1:1**（字段名/含义、默认值、校验、分支结果、错误业务文案）。禁止另造「差不多」的接口。
2. **实现前**必须对照旧 Handler 写方法级清单：路径/动作名、Form/Query 键、成功/失败 info 形状；对不齐 **停，问用户**。
3. 与 **AGENT A3**（2026-11-02）：
   - **Request**：`/request/...` 路径 + 方法名 + Form + `success/msg/info` **全对齐**。
   - **Admin / System**：对外 URL **分别 1:1** `/admin/...`、`/system/...`；动作名对齐；**不得**用 `/api/system/*`、`/api/platform/*` 顶替对外契约（内部实现仍可用框架鉴权/菜单）。
4. **第一版禁止以 `*.WaveGaps.cs` 占位充数**；按旧接口/方法名正经迁移。
5. 新框架内部实现可以换（SqlSugar、Shared 等），但 **对外业务契约不得「优化掉」旧行为**。确需变更语义：停，用户拍板后再改。
6. **界面操作与功能**另见 ADR-2026-10-31；本阶段可暂缓页面，接口不得偷工。

## 不做

- 不借本条把 Admin 退回 Layui 菜单皮肤为目标态。
- 不借本条改资金 SP 语义或对业务表 CodeFirst。
- 不借 WaveGaps / 新壳路径宣称「接口已迁完」。

## 后果

- 对照与联调以旧路径为键（例如 `/admin/site/info/get`），新旧并排排查。
- 已有按 `/api/system/*` 暴露的业务 API：第一版验收前须补 **旧路径 1:1 入口**（或等价路由映射），业务体仍 1:1。

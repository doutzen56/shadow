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
  - upgrade docs/AGENT-迁移硬约束.md B20
  - knowledge/decisions/2026/ADR-2026-10-31-betwin-admin-ui-ops-parity.md
---

# ADR-2026-10-30：迁移期业务逻辑与 I/O 对齐旧版

- 日期：2026-10-30
- 范围：SimpX.Net 上 BetWin 业务迁移（`feature/betwin1.0`）
- 状态：生效
- 来源：用户 `@@X` 硬性规定

## 决定

1. **迁移过程中**，每一刀业务的 **业务逻辑、入参、出参必须与旧版一致**（字段名/含义、默认值、校验、分支结果、错误业务文案）。禁止另造一套「看起来差不多」的接口。
2. **实现前**必须对照旧 Handler（或等价入口）写出方法级清单：路径/动作、Form/Query 键、成功/失败 info 形状；对不齐 **停，问用户**。
3. 与 **AGENT A3** 的关系：
   - **会员 / 代理 WebApi**：路径 + Form/Query + 旧版返回 `success/msg/info` **全对齐**。
   - **Admin / SuperAdmin**：鉴权与菜单按 A3（Vue + JWT + SysMenu）；HTTP 可走 `/api/system/*` 或 `/api/platform/*` 与统一 `code/result` 外壳；**外壳里面的业务字段与读写语义仍须对齐旧 Admin Handler**，禁止擅自增减字段、改默认值、合并/拆开旧动作却不声明兼容。
4. 新框架内部实现可以换（SqlSugar、Shared 服务等），但 **对外业务契约不得「优化掉」旧行为**。确需变更语义：停，用户拍板后再改契约版本。
5. **界面操作与功能**另见 ADR-2026-10-31：Admin 对照 Web.Admin，SuperAdmin 对照 Web.System；皮肤可不复刻 Layui，但筛选项、列、按钮、弹层动作不得擅自砍掉。

## 不做

- 不借本条把 Admin 退回 Layui 菜单 / 旧 TOKEN 为目标态（仍以 A3/B5 为准）。
- 不借本条改资金 SP 语义或对业务表 CodeFirst。
- 不借「壳是 Vue」省略旧页上的运营操作（见 ADR-2026-10-31）。

## 后果

- 波次 A1「站点设置」须对齐旧 `/admin/site/info/get|save`（Site + Setting），不得用自创的整表 `site_Config` 扁平 CRUD 顶替。

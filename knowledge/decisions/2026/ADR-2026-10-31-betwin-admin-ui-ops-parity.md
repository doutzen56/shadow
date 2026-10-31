---
id: adr-2026-10-31-betwin-admin-ui-ops-parity
title: BetWin 迁移：后台界面操作与功能对齐旧版 Web.Admin / Web.System
doc_type: decision
projects:
  - simpx-net
  - betwin
lifecycle_status: confirmed
confirmed_by: user
user_reviewed: false
related:
  - adr-2026-10-30-betwin-migrate-io-parity
risk_tags:
  - compatibility
  - public-contract
evidence_refs:
  - 会话 2026-10-31：用户 @@X「有一个问题，admin后台和superadmin后台vue页面，是根据旧版页面做的吗？我看着咋不像呢」
  - 会话 2026-10-31：用户「记住：后端业务和前台管理界面操作以及功能，都要对齐旧版，admin对应 web.admin   superadmin对应web.system」
---

# ADR-2026-10-31：后台界面操作与功能对齐旧版

- 日期：2026-10-31
- 范围：SimpX.Net 上 BetWin 后台迁移（`feature/betwin1.0`）
- 状态：生效
- 来源：用户 `@@X`「记住：」

## 决定

1. **后端业务**继续遵守 ADR-2026-10-30：逻辑、入参、出参对齐旧 Handler。
2. **前台管理界面的操作和功能**也要对齐旧版，不是只把接口迁过去、页面随便做一层：
   - **Admin**（`Web/apps/admin`）对照旧版 **Web.Admin**
   - **SuperAdmin**（`Web/apps/super-admin`）对照旧版 **Web.System**
3. **对齐范围**（要）：
   - 页面职责与菜单对应关系（一页干什么、从哪进）
   - 查询条件、列表列、详情字段、按钮与弹层动作
   - 操作顺序、必填/可选、成功失败后的页面反馈语义
   - 与旧页绑定的后台动作一一对应（少按钮、少筛选项、少关键列视为未对齐）
4. **不对齐外观皮肤**（不要误解成）：
   - 不要求像素级复刻 Layui 皮肤
   - 壳仍是 **Vue + Element Plus + JWT + SysMenu**（见迁移硬约束 A3 / B5）
   - 换 UI 组件库不等于可以砍掉旧页上的操作和功能
5. 旧前端源码位置（用户工作区已打开）：`Web.Admin`、`Web.System`（相对业务前端仓根；不写盘符进档案）。BetWin 解决方案仓里的同名目录仍忽略，以这两处为准。
6. 实现业务 Vue 页前：对照旧 HTML 列出筛选项 / 列 / 按钮 / 弹层 / 调用的 admin|system 动作；缺项不得标「页面对齐完成」。

## 不做

- 不把 Layui 菜单 JSON / 旧 TOKEN 重新定为终态。
- 不借「壳是新框架」省略旧页上的运营操作。

## 后果

- 现有大量业务 Vue 页若只有精简表格或占位，一律视为**未对齐**，后续按模块对照 Web.Admin / Web.System 补齐操作与功能。
- 与 ADR-2026-10-30 一起作为后台迁移验收口径：后端 I/O + 界面操作功能，两边都要对旧版。

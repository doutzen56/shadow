---
id: raw-2026-10-28-simpx-net-tenant-host-superadmin-kickoff
title: SimpX.Net 域名定租与 SuperAdmin 开工落地摘要
doc_type: raw
projects: []
lifecycle_status: raw
user_reviewed: false
---

# 2026-10-28 开工摘要

用户确认方案后「按此开工」。已落 ADR-2026-10-28；部分废止 ADR-2026-10-27 的「一律独立库」。

后端：SysTenant 迁 Data；ITenantHostResolver + 中间件；HostTenantMode；Admin/WebApi 按域名定租失败即拒；SuperAdmin Host:5004 + Application.SuperAdmin；租户增删改仅平台进程。

前端：Web pnpm 双应用 apps/admin、apps/super-admin、packages/shared。

未闭合：现有库 Host 仍可能是旧值（如 gitee.com），需改成实际访问域名才能本地登录；Admin 宿主若在跑会锁文件导致无法覆盖编译；租户服务仍主要在 Application.Admin，SuperAdmin 暂引用 Admin。

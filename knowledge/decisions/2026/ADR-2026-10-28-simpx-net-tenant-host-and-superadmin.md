---
id: adr-2026-10-28-simpx-net-tenant-host-and-superadmin
title: SimpX.Net 域名定租、隔离配置化、SuperAdmin 独立应用
doc_type: decision
projects:
  - simpx-net
lifecycle_status: confirmed
confirmed_by: user
user_reviewed: false
supersedes:
  - adr-2026-10-27-simpx-net-tenant-db-and-furion-first#data-topology-tenant-db-default
risk_tags:
  - permission
  - login
  - public-contract
evidence_refs:
  - 会话 2026-10-28：用户确认隔离配置化、SuperAdmin 方案 C、域名绑定、B5 前端双应用一次到位
---

# ADR-2026-10-28：域名定租 + 隔离配置化 + SuperAdmin C

- 日期：2026-10-28
- 范围：SimpX.Net（多宿主框架）
- 状态：生效
- 来源：用户 `@@X` 拍板并「按此开工」

## 决定

### 1. 租户隔离方式

1. **完全配置驱动**：`TenantTypeEnum.Id`（共库 + TenantId）与 `TenantTypeEnum.Db`（独立库）都是正式支持模式。
2. **不再**把「凡租户一律独立库」当作唯一目标态或代码默认强制项。
3. 新建租户的隔离方式跟配置/入参走；独立库时连接串必填。
4. **库拓扑只有两类**：中心库 + 租户库（`TenantType=Id` 时租户数据仍在中心库用 TenantId 隔离；`TenantType=Db` 时才有独立租户库文件/实例）。不做按宿主、按支付/WebApi 进程再拆库。
5. **只有 Admin 宿主初始化中心库**（CodeFirst / 种子）。SuperAdmin、WebApi、Schedule、Channels 以及后续支付等进程：只连中心库（及按租户配置切到的租户库），**禁止**对本机再开一份空库或抢初始化。WebApi、支付挂在具体租户下面，定租后读当前租户上下文办事。
6. Furion-first 拆分原则继续有效（见 ADR-2026-10-27 第 2 节，未废止）。

### 2. 定租方式（租户面向站点）

1. **按域名绑定租户**；一租户至少一个域名；`SysTenant.Host` 可存多个域名（逗号分隔）。
2. 入口：`Request.Host` → 查绑定 → 得到 `TenantId`。
3. 会话：登录后 JWT 仍带 `TenantId`；过滤、切库认 Claim。
4. 租户站域名**匹配失败：直接失败**，不静默落到默认租户。
5. 登录页不靠选手动租户列表定身份（由域名锁定）。
6. **定租只发生在宿主/基础设施**（管道中间件 + `ITenantHostResolver` / `ICurrentTenantAccessor`）。登录、租户管理等业务**禁止**再按 Host 查租户；只读当前租户上下文或 Claim。

### 3. 总后台 SuperAdmin（方案 C）

1. 独立 `Application.SuperAdmin` + `Host.SuperAdmin`。
2. 独立平台域名；**不按租户域认身份**。
3. 代管某租户时显式选择/传入 `TenantId`。
4. 职责：租户注册、连接与隔离配置、全局运维；租户 Admin 只管本租户业务。

### 4. 前端（B5）

1. 一次到位：`apps/admin` + `apps/super-admin` + `packages/shared`。
2. 各自构建、各自站点、各自 API 基址。
3. 不做单 SPA 靠角色藏菜单长期混用；不做「先整仓复制再拆」的过渡态。

## 不采用

- 「凡租户一律独立库」作为产品默认。
- 租户站以客户端自选 `TenantId` 为权威定租方式。
- 总后台与租户后台长期共一个前端 SPA。
- SuperAdmin 与 Admin 共一个 Application 靠菜单权限区分。

## 后果

- 废止 ADR-2026-10-27 中与「一律独立库 / 不以 Id 为默认」冲突的数据拓扑表述；该 ADR 的 Furion-first 与「不做按域分库」仍然有效。
- Admin / WebApi 等租户面向宿主启用 Host→TenantId；SuperAdmin / Worker 不定租户域。
- 默认种子与 `DbConnectionConfig.TenantType` 不得再写死偏向必须 Db。

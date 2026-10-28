---
id: adr-2026-10-27-simpx-net-tenant-db-and-furion-first
title: SimpX.Net 中心库+租户一律独立库；拆分优先用 Furion 自带能力
doc_type: decision
projects:
  - simpx-net
lifecycle_status: superseded_partial
confirmed_by: user
user_reviewed: false
superseded_by: adr-2026-10-28-simpx-net-tenant-host-and-superadmin
---

# ADR-2026-10-27：SimpX.Net 数据拓扑与拆分原则

- 日期：2026-10-27
- 来源：用户 `@@X` 架构讨论——否定按域/按宿主分库；当时选择「凡租户一律独立库」；拆分过程「能用 Furion 自带的就用自带的」
- 范围：SimpX.Net（多宿主框架）
- 状态：**部分废止**——「凡租户一律独立库」已被 [ADR-2026-10-28](./ADR-2026-10-28-simpx-net-tenant-host-and-superadmin.md) 取代；下文第 2 节 Furion-first 与「不做按域/按宿主分库」仍有效

## 决定

### 1. 数据拓扑（租户库默认部分已废止）

1. **不做**按业务域或按宿主分库（不为 WebApi / Schedule / Channels 单独建「会员库」「作业库」「支付库」）。**仍有效。**
2. 业务库认两层中心库 + 租户数据：~~凡租户一律独立库~~ → **改由配置选择 Id/Db，见 ADR-2026-10-28。**
3. 日志库等技术库可另设，不算业务分域库。**仍有效。**
4. 多宿主共用同一套库拓扑；差别在进程职责与认证，不在「一宿主一库」。**仍有效。**
5. 中心库的结构初始化与种子：总后台侧以 **SuperAdmin**（迁出前暂由 Admin）为准；其它宿主连库读写，不抢中心库的 CodeFirst/种子。**原则仍有效，宿主名随 10-28 调整。**

### 2. 拆分与实现原则（仍有效）

1. 多宿主拆分、Hosting 点选、Contracts、Core 消肿等改造中：**Furion 已有的能力优先用框架自带**（动态 API、事件总线、组件装配、鉴权扩展、远程请求、任务队列等），不平行造一套同名轮子。
2. 只有 Furion 没有、或明确不够用时，才自研 Feature / 扩展。

## 不采用

- 一宿主一库 / 按域分库作为目标架构。**仍有效。**
- ~~以 `TenantTypeEnum.Id` 不当作产品默认~~ → **已取消**；Id/Db 均正式支持，见 ADR-2026-10-28。

## 后果

- Furion-first 与不做按域分库继续指导拆分。
- 租户隔离、定租、SuperAdmin 边界以 ADR-2026-10-28 为准。

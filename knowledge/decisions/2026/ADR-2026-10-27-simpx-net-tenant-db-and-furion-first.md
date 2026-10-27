---
id: adr-2026-10-27-simpx-net-tenant-db-and-furion-first
title: SimpX.Net 中心库+租户一律独立库；拆分优先用 Furion 自带能力
doc_type: decision
projects: []
lifecycle_status: confirmed
confirmed_by: user
user_reviewed: false
---

# ADR-2026-10-27：SimpX.Net 数据拓扑与拆分原则

- 日期：2026-10-27
- 来源：用户 `@@X` 架构讨论——否定按域/按宿主分库；选择「凡租户一律独立库」；拆分过程「能用 Furion 自带的就用自带的」
- 范围：SimpX.Net（多宿主框架）
- 状态：生效

## 决定

### 1. 数据拓扑

1. **不做**按业务域或按宿主分库（不为 WebApi / Schedule / Channels 单独建「会员库」「作业库」「支付库」）。
2. 业务库只认两层：
   - **中心库（总后台库）**：租户注册与连接、全局模板与运维配置、总后台账号等。
   - **租户库**：**凡租户一律独立库**（对应实现上的 `TenantTypeEnum.Db` 路径；不再把「仅 TenantId 共库」当作目标态默认）。
3. 日志库等技术库可另设，不算业务分域库。
4. 多宿主共用同一套库拓扑；差别在进程职责与认证，不在「一宿主一库」。
5. 中心库的结构初始化与种子以 **Admin** 为准；其它宿主连库读写，不抢中心库的 CodeFirst/种子。

### 2. 拆分与实现原则

1. 多宿主拆分、Hosting 点选、Contracts、Core 消肿等改造中：**Furion 已有的能力优先用框架自带**（动态 API、事件总线、组件装配、鉴权扩展、远程请求、任务队列等），不平行造一套同名轮子。
2. 只有 Furion 没有、或明确不够用时，才自研 Feature / 扩展。

## 不采用

- 一宿主一库 / 按域分库作为目标架构。
- 以 `TenantTypeEnum.Id`（共库 + TenantId）作为产品默认租户模式（实现里可短暂保留兼容，但架构目标是 B：租户独立库）。

## 后果

- Contracts、Hosting 能力点选、Kernel/Feature 拆分可继续推进，且不依赖按域分库。
- 新建租户、连库、缓存连接串等实现与文档，按「中心库 + 每租户一库」叙述和验收。
- 代码评审：出现与 Furion 重复的自研总线/动态 API/组件体系时，先质疑是否该删掉改用 Furion。

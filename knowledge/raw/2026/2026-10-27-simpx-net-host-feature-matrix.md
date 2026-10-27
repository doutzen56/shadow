---
id: raw-2026-10-27-simpx-net-host-feature-matrix
title: SimpX.Net 每宿主默认 Feature 开关表
doc_type: raw
projects: []
---

# SimpX.Net 每宿主默认 Feature 开关表

- 日期：2026-10-27
- 来源：用户 `@@X` 批准架构方案后安排落地
- 约定：● 默认开；○ 默认关（配置打开再用）；— 不引用 / 不注册
- 原则：Furion 已有能力优先；能关的不进 Hosting 全局默认

| Feature / 能力 | Admin | WebApi | Schedule | Channels | 说明 |
| --- | --- | --- | --- | --- | --- |
| Data / SqlSugar | ● | ● | ● | ● | 共用中心库拓扑；种子/建表仅 Admin |
| Caching | ● | ● | ● | ● | |
| Auth.Jwt（后台用户） | ● | — | — | — | |
| Auth.Jwt（会员） | — | ● | — | — | |
| Auth.InternalCall | ● | ○ | ● | ● | WebApi 按需 |
| Auth.Signature（开放接口） | ● | ○ | — | ○ | Channels 看渠道校验方式 |
| Logging（文件/控制台） | ● | ● | ● | ● | |
| Logging → 写库 | ● | — | — | — | 审计表在后台 |
| Logging → ElasticSearch | ○ | — | — | — | |
| Email（含错误告警订阅） | ● | ○ | ○ | ○ | |
| Sms | ○ | ○ | — | — | |
| Storage / 上传 | ● | ○ | — | — | |
| Captcha | ● | ○ | — | — | |
| RateLimit | ○ | ○ | — | ○ | 有 Limit.json 才注册 |
| SignalR | ● | ○ | — | — | |
| ImageSharp | ● | — | — | — | |
| ViewEngine | ● | — | — | — | |
| IP 归属地库进内存 | ● | ○ | — | — | 避免 Schedule/Channels 白吃 60MB+ |
| Localization | ● | ○ | — | — | |
| ExternalLogin | ○ | ○ | — | — | |
| Translation | ○ | — | — | — | |
| EventBus（Furion 内存） | ● | ● | ● | ● | |
| EventBus → Redis Storer | ○ | ○ | ○ | ○ | 随 Cache=Redis |
| EventBus → RabbitMQ | ○ | ○ | ○ | ○ | 引用 Feature 后替换 |
| APIJSON | — | — | — | — | 已整块删除（2026-10-27） |
| Cluster / DataProtection.Redis | ○ | ○ | ○ | ○ | |
| Contracts.Jobs 客户端 | ● | — | — | — | Admin 调 Schedule |
| Contracts.Jobs 服务端 | — | — | ● | — | |
| 调度看板登录 | — | — | ● | — | `IScheduleDashboardLogin` |
| MongoDB 提供程序 | — | — | — | — | 默认不注册 |

## Core 消肿验收（配套）

Core/Kernel 禁止长期留下：`UserManager`、`AdminJwtHandler`、业务 Enum（Notice/Nation…）、`PrintTypeEnum`、APIJSON、硬件监控 Utils、作业实体与 `IScheduleDashboardLogin`。

## 本轮已落地范围

- 写出本表。
- 抽出 `SimpX.Net.Contracts.Jobs`（作业协议 + 看板登录），Admin 不再项目引用 `Application.Schedule`。

## 落地进度

- 2026-10-27：Hosting 点选已落地。HostLauncher.Run<认证>(HostCapabilities.Admin|WebApi|Worker())；HostServicesComponent / MVC / 管道按 HostCapabilities.Current 注册 Captcha、ImageSharp、ViewEngine、SignalR、FileStorage、IP 内存库、Localization、VirtualFileServer。验证：编译 0 错误；四宿主起来；Admin 登录与作业转发 200。

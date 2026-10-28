# SimpX.Net 系统边界

## 边界总览

SimpX.Net 是单仓多宿主框架。总后台、租户后台、会员 API、调度与渠道进程共享 Data / Feature / Core，但进程职责、定租方式和前端入口不同。边界目标是：定租不进业务、总后台与租户后台不混 API、共库/独立库只跟租户配置走。

## 宿主边界

### 总后台 SuperAdmin

路径：`SimpX.Net.Host.SuperAdmin` / `SimpX.Net.Application.SuperAdmin`

职责：

- 租户注册、域名绑定、隔离方式与连接配置、全局运维。
- 登录只接受总后台账号（`AccountType=SuperAdmin`）。
- 不定租户域（`HostTenantMode.IsPlatformHost`）。

边界：

- 不承接单租户日常业务菜单。
- 代操作某租户时显式传/选 `TenantId`，不靠请求域名认租户。
- 对外约定：`/api/platform/host`、`/api/platform/auth`、`/api/platform/tenant`；实现在 `Application.Shared`，总后台进程不再加载 `Application.Admin`。

### 租户后台 Admin

路径：`SimpX.Net.Host.Admin` / `SimpX.Net.Application.Admin`

职责：

- 当前租户内的用户、权限、配置与业务后台。
- 管道按绑定域名定租；匹配失败直接失败。

边界：

- 超管账号不得在此登录（`Z1006`）。
- 租户增删改仅允许总后台进程调用；Admin 进程调用应拒绝。
- 业务服务禁止再按 `Request.Host` 查租户，只读当前租户上下文。

### 会员 WebApi

路径：`SimpX.Net.Host.WebApi` / `SimpX.Net.Application.WebApi`

职责：

- 会员等对外 HTTP API。

边界：

- 租户面向时同样走域名定租能力开关。
- 不承担总后台租户注册。

### Schedule / Channels

路径：`Host.Schedule` / `Host.Channels` 及对应 Application

职责：

- 定时任务、渠道与内部调用。

边界：

- 默认不定租户域（Worker 预设）。
- 不抢中心库 CodeFirst / 种子（中心库初始化以 Admin / 后续 SuperAdmin 为准）。

## 定租边界

| 层 | 允许 | 禁止 |
| --- | --- | --- |
| Hosting 管道 | Host→TenantId，写入上下文 | 业务规则、账号类型判断堆在业务里重复解析域名 |
| Data | `ITenantHostResolver`、`ICurrentTenantAccessor`、`SysTenant` | 登录/CRUD 流程 |
| Application.* | 读当前租户 Id / Claim 办事 | 再读 Host 查租户表 |

## 数据边界

- 只有两类库：中心库、租户库（Id 隔离时租户行仍在中心库）。
- 中心库：租户注册、连接、总后台账号、全局模板；**仅 Host.Admin 初始化**。
- 租户数据：`TenantType=Id` 共库 + TenantId 过滤；`TenantType=Db` 独立库切连接。
- WebApi、支付、Schedule、Channels、SuperAdmin：连中心库（及租户库）但不初始化；支付与 WebApi 挂在具体租户下。
- 不做按业务域 / 按宿主分库。

## 前端边界

| 应用 | 目录 | 对接 |
| --- | --- | --- |
| 租户后台 | `Web/apps/admin` | Admin :5005 |
| 总后台 | `Web/apps/super-admin` | SuperAdmin :5004 |
| 共享 | `Web/packages/shared` | 请求封装等 |

禁止长期单 SPA 靠角色藏菜单混用总后台与租户后台。

## 改动升级条件

涉及资金、订单、权限、登录、租户隔离、对外接口契约、生产配置或删数据时，按高风险链路处理，需用户拍板后再推进。

# SimpX.Net 项目档案

## 项目定位

SimpX.Net 是基于 Furion + SqlSugar 的多宿主通用权限与多租户框架：中心库登记租户与连接，租户数据按配置做 Id 共库或 Db 独立库隔离；Admin / WebApi 按绑定域名定租；总后台 SuperAdmin 与租户后台 Admin 分宿主。

## 项目风格

- 主风格：企业稳定型
- 次风格：业务交付型
- 补充风格：遗留系统改造型（从单体后台拆多宿主 / 双前端）
- 治理标签：演进中的框架拆分

## 工程标识

- 解决方案：`SimpX.Net.sln`（相对仓库根：`SimpX.Net/SimpX.Net.sln`）
- SimpX 档案：`projects/examples/simpx-net`
- 前端：`Web/`（pnpm monorepo）

## 主要宿主

| 宿主 | 端口（开发默认） | 职责 |
| --- | --- | --- |
| Host.SuperAdmin | 5004 | 总后台：租户注册、隔离配置、全局运维；不定租户域 |
| Host.Admin | 5005 | 租户后台：按域名定租，只服务当前租户 |
| Host.WebApi | 5006 | 会员等对外 API；按域名定租 |
| Host.Schedule | 5007 | 定时任务 / 内部调用 |
| Host.Channels | 5008 | 渠道等内部宿主 |

## 应用与分层（相对 `SimpX.Net/`）

| 项目 | 职责 |
| --- | --- |
| Application.Shared | 登录、Jwt、租户平台能力（无 DynamicApi）；总后台与租户后台共用 |
| Application.SuperAdmin | 总后台应用（只暴露 /api/platform/*） |
| Application.Admin | 租户后台应用（/api/system/*） |
| Application.WebApi / Schedule / Channels | 对应宿主业务 |
| Hosting | 启动入口、能力点选、请求处理链（含域名定租中间件） |
| Data | SqlSugar、仓储、SysTenant、定租解析与当前租户访问器 |
| Feature.* | 认证、验证码、存储、短信、邮件等可点选能力 |
| Core | 常量、枚举、错误码、宿主定租模式快照 |

## 定租与总后台（摘要）

权威决定见 `knowledge/decisions/2026/ADR-2026-10-28-simpx-net-tenant-host-and-superadmin.md`：

- Admin / WebApi：请求处理链按 Host 定租；业务只读 `ICurrentTenantAccessor` / Claim。
- 会员等业务实体：Data + Shared 领域服务；各宿主只保留带身份约束的 HTTP 入口。
- Schedule 独立进程时保留 `Contracts.Jobs`（内部 HTTP）；不与会员领域服务混用一种模式。
- 总后台：独立域名与宿主；代管租户时显式 TenantId。
- 隔离：`TenantType` Id/Db 配置驱动，不强制一律独立库。
- 前端：`apps/admin` + `apps/super-admin` + `packages/shared`。

## 本地开发注意

- 默认租户种子 Host 为 `localhost`；`TenantHostResolver` 把 `127.0.0.1` 与 `localhost` 互相认作同一绑定。空 Host 不能定租。
- 总后台前端约定只调 `/api/platform/*`（auth / tenant / host）；Admin 进程上租户增删改会 `SA001`。
- 库只有中心库与租户库两类；开发上 SuperAdmin / WebApi / Schedule / Channels 都指向 Admin 的 Sqlite，并关闭本机 CodeFirst/种子。
- WebApi、支付挂在具体租户下，定租后办事，不各自起空库；Channels 按域名定租，支付表由 Admin 初始化。
- 租户增删改/建库/授权只走总后台 `/api/platform/tenant`；Admin 进程不再暴露这套运维 API。
- `SysTenant.Host` 可存多个域名（逗号分隔）；`localhost` 与 `127.0.0.1` 互相认。
- 业务仓禁止写 Agent 规则目录；禁止 Agent git push / 开 PR。

## HTTP 约定（摘要）

- 内部互调：Declarative API；外部出站：`IHttpCaller`（Feature.Http）。
- 详见 `boundaries.md`「HTTP 出站约定」。

## 工作约束

- 只做本地修改，不 git push、不提 PR。
- 提交前由人审。
- 知识只写 SimpX，不写业务仓。


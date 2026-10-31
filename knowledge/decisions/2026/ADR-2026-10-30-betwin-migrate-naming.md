---
id: adr-2026-10-30-betwin-migrate-naming
title: BetWin 迁移命名：禁止 BetWin 目录/类名前缀；实体进 Entity；ES 文档 EsXxx
doc_type: decision
projects:
  - simpx-net
  - betwin
lifecycle_status: confirmed
confirmed_by: user
user_reviewed: false
risk_tags:
  - public-contract
evidence_refs:
  - 会话 2026-10-30：用户 @@X「不要 betwin 文件夹」「类名不要加 betwin」「es 文档统一 esXxx」「实体放 entity/…」
  - 会话 2026-10-30：用户澄清举例只指目录结构；旧版实体类名不改，原来叫 User 仍叫 User
  - 会话 2026-10-31：用户 @@X「类名根据职责来命名，不要 queryservice writeservice」
  - 会话 2026-10-31：用户 @@X「dto也要规范一下按照职责」
  - 会话 2026-10-31：用户 @@X 纠正——业务能力叫 SiteDomain 就应叫 SiteDomainService，不要 XxxAdminService
---

# ADR-2026-10-30：迁移代码命名与实体目录

- 日期：2026-10-30
- 范围：SimpX.Net 上 BetWin 业务迁移代码（`feature/betwin1.0`）
- 状态：生效
- 来源：用户 `@@X` 拍板（含同日澄清）

## 决定

1. **禁止**在 SimpX.Net 业务迁移代码里再建 `BetWin/` 目录（含 `Data/BetWin`、`Service/BetWin`、`Test/BetWin` 等）。
2. **禁止**为迁移层服务/特性/种子类加 `BetWin` 前缀。兼容层用职责名（如 `LegacyResult`、`MemberAuthService`）。
3. **业务实体**放在 `SimpX.Net.Data/Entity/<域>/`，命名空间 `SimpX.Net.Data.Entity.<域>`。目录按旧站域拆，例如：
   - `Entity/Users/User.cs`
   - `Entity/Sites/Site.cs`
   - `Entity/Systems/...`
   - `Entity/Es/...`（ES 文档模型）
4. **旧版实体类名不改**：原来叫什么还叫什么（`User`、`Site`、`SiteAdmin`、`UserGroup` 等）。用户举例里的 `member` / `siteinfo` **只说明目录形态，不是改名要求**。
5. **ES 文档模型**类名统一 `Es` 前缀（如 `EsAdminLog`），放 `Entity/Es/`。与 SQL 表实体同名并存时，ES 侧用 `Es` 前缀区分；已有 SQL 映射实体不因举例被改名。
6. **定站基础设施**（解析器、访问器）不是实体，放 `SimpX.Net.Data/SiteContext/`，不进 `Entity/`，也不挂 `BetWin` 目录。
7. **应用服务按业务能力命名，禁止按读写拆、禁止加宿主标签**：不要 `XxxQueryService` / `XxxWriteService` / `XxxCommandService`；也不要 `XxxAdminService`（`Admin` 不是业务职责，只是宿主）。同一业务能力一个服务，能力叫什么服务就叫什么，例如 `SiteDomainService`、`SiteNewsService`、`ExchangeRateService`。与框架已有 `MemberService`（租户 Member 表）撞名时，BetWin 会员后台用 `UserMemberService`（对齐 `userMember` API）。只读报表可用 `TipReportService` 这类职责名。租户后台 HTTP 入口仍是 Furion 的 `XxxAppService`，用这一层区分宿主。AppService 注入字段统一 `_svc`。存量已于 2026-10-31 批量去掉 `Admin` 后缀。
8. **DTO 文件按业务能力命名，禁止按读写拆、禁止加宿主标签**：不要 `XxxQueryDtos.cs` / `XxxWriteDtos.cs` / `XxxAdminDtos.cs`。同一业务能力一个文件（如 `SiteDomainDtos.cs`、`UserMemberDtos.cs`、`SiteNewsDtos.cs`），列表/详情/保存入参放一起。类型名仍用 `*PageInput` / `*ListItemDto` / `*DetailDto` / `*SaveInput` 等职责后缀，不叫 QueryDto/WriteDto/AdminDto。

## 不做

- 不借这次调整去改对外 HTTP 路径、入参、返回包络。
- 不对业务表 CodeFirst 改结构；`SugarTable` / 列名保持旧库。
- 不把旧实体改成 `Member` / `SiteInfo` 等新名字。

## 废止

- `01-实体盘点清单.md` / `AGENT-迁移硬约束.md` 里「实体放 `Data/BetWin/**`」的路径约定，以本 ADR 为准。

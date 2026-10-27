# SimpX.Net Core → Data / Feature.* 拆分落地（2026-10-27）

## 结论

已把胖 Core 拆成：

- `SimpX.Net.Data`：SqlSugar 装配、仓储、过滤器、`DbConnectionOptions`（默认 `TenantType=Db`）
- `SimpX.Net.Feature.*`：Auth / Email / Sms / Storage / Logging / Captcha / ExternalLogin / Translation / EventBus
- `SimpX.Net.Core`：常量、框架枚举、扩展、工具、SignalR 等薄内核
- 后台专用：`AdminJwtHandler` / `UserManager` 在 `Application.Admin`；业务枚举迁入 `Application.Admin/Enum`
- 统一返回：`AdminResult<T>` 在 Core；管道 `AdminResultProvider` 在 Hosting；应用层静态构造用 `AdminResults`

租户拓扑仍按 ADR：中心库 + 凡租户独立库（`TenantTypeEnum.Db`）；种子默认库类型已是 Db。

## 冒烟证据（本会话）

- `dotnet build SimpX.Net.sln -c Debug` → 0 错误
- 四宿主监听 5005–5008，swagger All Groups 均 200
- Admin swagger 登录成功；`GET /api/system/menu/loginMenuTree` → code 200
- `POST /api/system/job/pageJobDetail`（转 Schedule）→ code 200

## 修复点（拆分过程踩到的）

- Feature/Data 需 `Microsoft.AspNetCore.App` + 完整 GlobalUsings；Data 需 `System.Collections`（非泛型 `IList`/`IEnumerable`）
- `Retry` 在 `Furion.FriendlyException`
- Worker 认证用 `Feature.Auth.DefaultJwtHandler`，不再引用 Admin 专用 Handler
- `IFileStorage` 必须在 `AddSqlSugar` 之前注册（种子会解析应用服务）
- `ICacheReadThroughScope` 改为 Transient（`SimpXCache` 从根容器取，不能 Scoped）

## 刻意未做 / 后续可选项

- Core `Option` 里其余配置项是否再拆到对应 Feature（未阻塞）
- APIJSON 全局选项收口、IP 库仅随 Logging/Admin、Test 工程矩阵、docker 租户库故事
- 未提交 git（用户未要求 commit）

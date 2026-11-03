# SimpX.Net.Test 引用矩阵

按测试类型引用项目，避免 Test 工程无差别挂上全部 Feature。

| 测试类型 | 目录约定 | 允许引用 | 说明 |
| --- | --- | --- | --- |
| 工具单测 | `Utils/` | `SimpX.Net.Core` | 纯函数、扩展、安全计算等，不启宿主 |
| 数据层单测（若新增） | `Data/` | `Core` + `Data` + `Caching` | 测仓储/过滤器时再加，默认不要挂 |
| 功能包单测（若新增） | `Feature.<名>/` | `Core` + 对应 `Feature.*` | 只引被测包，不引 Application |
| 应用服务单测（若新增） | `Application.Admin/` 等 | 对应 `Application.*` + 其依赖 | 用 Furion 测试宿主时再扩 |
| UI / Selenium | `User/` 等 | **不引**后端业务项目 | 打已启动的前端/Admin 地址 |

当前工程已挂：`SimpX.Net.Core`（覆盖 `Utils/`）、`SimpX.Net.Data`（覆盖 `Data/`，如 `TenantHostBindingTests`）。  
Selenium 用例依赖本机前端（默认 `http://localhost:8888`），不通过项目引用拉宿主。

运行单测（.NET 10 + Microsoft.Testing.Platform）：

```bash
dotnet run --project SimpX.Net.Test -- --filter-class SimpX.Net.Test.Data.TenantHostBindingTests
```

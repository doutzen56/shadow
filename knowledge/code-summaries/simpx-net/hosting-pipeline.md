# 宿主装配与管道

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `simpx-net`
- summary_group: `hosting-pipeline`
- host_or_layer: `infrastructure`
- solution_file: `SimpX.Net/SimpX.Net.sln`
- last_ingestion_run: `20261028T041009Z-project-simpx-net`
- file_count: 11
- project_count: 1
- risk_tags: `login`, `permission`

## 职责摘要

HostLauncher、能力点选、公共管道与域名定租中间件。

## 扫描根目录

- `SimpX.Net/SimpX.Net.Hosting`

## 项目文件

- `SimpX.Net/SimpX.Net.Hosting/SimpX.Net.Hosting.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `SimpX.Net/SimpX.Net.Hosting/AdminResultProvider.cs`：符号 `AdminResultProvider, OnAuthorizeException, OnException, OnSucceeded, OnValidateFailed, OnResponseStatusCodes, Ok, Error`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/Components/HostMvcComponent.cs`：符号 `HostMvcComponent, Load, RemoveExcludedAssemblies`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/Components/HostPipelineComponent.cs`：符号 `HostPipelineComponent, Load`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/Components/HostServicesComponent.cs`：符号 `HostServicesComponent, Load`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/DefaultSecretChecker.cs`：符号 `DefaultSecretChecker, readonly, EnsureChangedInProduction, Sha256Hex`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/GlobalUsings.cs`：符号 `未提取到符号`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/HostCapabilityOptions.cs`：符号 `HostCapabilityOptions, HostCapabilities, Admin, SuperAdmin, WebApi, Worker`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/HostLauncher.cs`：符号 `HostLauncher, HostWebComponent, Load`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/Middleware/TenantHostResolveMiddleware.cs`：符号 `TenantHostResolveMiddleware, InvokeAsync`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/ProjectOptions.cs`：符号 `ProjectOptions, AddProjectOptions`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Hosting/SimpX.Net.Hosting.csproj`：符号 `未提取到符号`；风险 `login, permission`

## 跳过项

- pruned_directory 2 项；明细只写入 `rag/init-reports/20261028T041009Z-project-simpx-net.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

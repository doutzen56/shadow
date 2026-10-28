# 会员 API 与内部宿主

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `simpx-net`
- summary_group: `api-worker-hosts`
- host_or_layer: `web-host`
- solution_file: `SimpX.Net/SimpX.Net.sln`
- last_ingestion_run: `20261028T041009Z-project-simpx-net`
- file_count: 41
- project_count: 6
- risk_tags: `async`, `callback`, `funds`, `gateway`, `login`, `order-state`, `permission`, `user-data`

## 职责摘要

会员 WebApi、调度与渠道宿主，内部调用与对外会员面。

## 扫描根目录

- `SimpX.Net/SimpX.Net.Host.WebApi`
- `SimpX.Net/SimpX.Net.Host.Schedule`
- `SimpX.Net/SimpX.Net.Host.Channels`
- `SimpX.Net/SimpX.Net.Application.WebApi`
- `SimpX.Net/SimpX.Net.Application.Schedule`
- `SimpX.Net/SimpX.Net.Application.Channels`

## 项目文件

- `SimpX.Net/SimpX.Net.Host.WebApi/SimpX.Net.Host.WebApi.csproj`
- `SimpX.Net/SimpX.Net.Host.Schedule/SimpX.Net.Host.Schedule.csproj`
- `SimpX.Net/SimpX.Net.Host.Channels/SimpX.Net.Host.Channels.csproj`
- `SimpX.Net/SimpX.Net.Application.WebApi/SimpX.Net.Application.WebApi.csproj`
- `SimpX.Net/SimpX.Net.Application.Schedule/SimpX.Net.Application.Schedule.csproj`
- `SimpX.Net/SimpX.Net.Application.Channels/SimpX.Net.Application.Channels.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `SimpX.Net/SimpX.Net.Application.Channels/Abstractions/IPaymentChannel.cs`：符号 `IPaymentChannel`；风险 `async, callback, funds, gateway, login`
- `SimpX.Net/SimpX.Net.Application.Channels/Abstractions/PaymentCreateRequest.cs`：符号 `PaymentCreateRequest`；风险 `async, callback, funds, gateway, login`
- `SimpX.Net/SimpX.Net.Application.Channels/Abstractions/PaymentCreateResult.cs`：符号 `PaymentCreateResult`；风险 `async, callback, funds, gateway, login`
- `SimpX.Net/SimpX.Net.Application.Channels/Abstractions/PaymentNotifyContext.cs`：符号 `PaymentNotifyContext`；风险 `async, callback, funds, gateway, login`
- `SimpX.Net/SimpX.Net.Application.Channels/Abstractions/PaymentStatus.cs`：符号 `PaymentStatus`；风险 `async, callback, funds, gateway, login, order-state`
- `SimpX.Net/SimpX.Net.Application.Channels/Channels/DemoPaymentChannel.cs`：符号 `DemoPaymentChannel, ParseNotifyAsync`；风险 `async, callback, funds, gateway, login`
- `SimpX.Net/SimpX.Net.Application.Channels/Entity/PaymentOrder.cs`：符号 `PaymentOrder`；风险 `async, callback, funds, gateway, login, order-state`
- `SimpX.Net/SimpX.Net.Application.Channels/GlobalUsings.cs`：符号 `未提取到符号`；风险 `async, callback, gateway, login`
- `SimpX.Net/SimpX.Net.Application.Channels/Service/HostInfoAppService.cs`：符号 `HostInfoAppService, Info`；风险 `async, callback, gateway, login`
- `SimpX.Net/SimpX.Net.Application.Channels/Service/PaymentOrderAppService.cs`：符号 `PaymentOrderAppService, Create, Get, Notify, ResolveChannel`；风险 `async, callback, funds, gateway, login, order-state`
- `SimpX.Net/SimpX.Net.Application.Channels/SimpX.Net.Application.Channels.csproj`：符号 `未提取到符号`；风险 `async, callback, gateway, login`
- `SimpX.Net/SimpX.Net.Application.Channels/Startup.cs`：符号 `Startup, ConfigureServices, Configure`；风险 `async, callback, gateway, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Engine/ConfigScheduleDashboardLogin.cs`：符号 `ConfigScheduleDashboardLogin, ValidateAsync, FixedEquals`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Engine/DynamicJobCompiler.cs`：符号 `DynamicJobCompiler, BuildJob`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Engine/ScheduleHostOptions.cs`：符号 `ScheduleHostOptions`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/GlobalUsings.cs`：符号 `未提取到符号`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Jobs/SystemLogCleanupJob.cs`：符号 `SystemLogCleanupJob, ExecuteAsync`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Service/HostInfoAppService.cs`：符号 `HostInfoAppService, Info`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Service/JobManagement/DbJobPersistence.cs`：符号 `DbJobPersistence, PreloadAsync, OnLoadingAsync, OnChangedAsync, OnTriggerChangedAsync, OnExecutionRecordAsync`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Service/JobManagement/Http/HttpJob.cs`：符号 `HttpJob, ExecuteAsync`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Service/JobManagement/Http/HttpJobMessage.cs`：符号 `HttpJobMessage`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Service/JobManagement/JobClusterServer.cs`：符号 `JobClusterServer, Start, WaitingForAsync, Stop, Crash, WorkNowAsync, StartHeartbeat`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Service/JobManagement/JobMonitor.cs`：符号 `JobMonitor, OnExecutingAsync, OnExecutedAsync`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Service/JobManagement/JobSchedulerAppService.cs`：符号 `JobSchedulerAppService, PageJobDetail, ListJobGroup, AddJobDetail, UpdateJobDetail, DeleteJobDetail, GetJobTriggerList, AddJobTrigger`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/SimpX.Net.Application.Schedule.csproj`：符号 `未提取到符号`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.Schedule/Startup.cs`：符号 `Startup, ConfigureServices, Configure`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.WebApi/Dto/MemberDto.cs`：符号 `MemberRegisterInput, MemberLoginInput, MemberLoginOutput, MemberProfileOutput`；风险 `async, callback, login, user-data`
- `SimpX.Net/SimpX.Net.Application.WebApi/Entity/Member.cs`：符号 `Member`；风险 `async, callback, login, user-data`
- `SimpX.Net/SimpX.Net.Application.WebApi/GlobalUsings.cs`：符号 `未提取到符号`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.WebApi/Security/MemberAuthenticationComponent.cs`：符号 `MemberAuthenticationComponent, Load`；风险 `async, callback, login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.WebApi/Security/MemberJwtHandler.cs`：符号 `MemberJwtHandler, HandleAsync, PipelineAsync`；风险 `async, callback, login, user-data`
- `SimpX.Net/SimpX.Net.Application.WebApi/Service/HostInfoAppService.cs`：符号 `HostInfoAppService, Info`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.WebApi/Service/MemberAppService.cs`：符号 `MemberAppService, Register, Login, Profile`；风险 `async, callback, login, user-data`
- `SimpX.Net/SimpX.Net.Application.WebApi/SimpX.Net.Application.WebApi.csproj`：符号 `未提取到符号`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Application.WebApi/Startup.cs`：符号 `Startup, ConfigureServices, Configure`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Host.Channels/Program.cs`：符号 `未提取到符号`；风险 `async, callback, gateway, login`
- `SimpX.Net/SimpX.Net.Host.Channels/SimpX.Net.Host.Channels.csproj`：符号 `未提取到符号`；风险 `async, callback, gateway, login`
- `SimpX.Net/SimpX.Net.Host.Schedule/Program.cs`：符号 `未提取到符号`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Host.Schedule/SimpX.Net.Host.Schedule.csproj`：符号 `未提取到符号`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Host.WebApi/Program.cs`：符号 `未提取到符号`；风险 `async, callback, login`
- `SimpX.Net/SimpX.Net.Host.WebApi/SimpX.Net.Host.WebApi.csproj`：符号 `未提取到符号`；风险 `async, callback, login`

## 跳过项

- pruned_directory 12 项、sensitive_file 3 项；明细只写入 `rag/init-reports/20261028T041009Z-project-simpx-net.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。

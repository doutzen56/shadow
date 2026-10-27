# Core 消肿 / IP 能力 / 租户建库 / 前端 api 生成（2026-10-27）

## 已做

1. **HardwareInfo** 从 Core 迁到 `Application.Admin/Utils/HardwareInfo`（仅服务器监控用）。
2. **SignalR**：`HubInvocationBus` 仍留 Core（Admin 应用引用，Hosting 按能力注册）；本轮不硬拆 Feature，避免 Application→Hosting 反向依赖。
3. **IP**：新增 `HostCapabilityOptions.IpLocation`；仅 Admin 预设开启；Worker/WebApi 不初始化 IPTools；`IpLocationInMemory` 仍只在 Admin 开。
4. **租户独立库实操**：`AddTenant(TenantType=Db)` + `CreateDb` 冒烟通过；修 Sqlite `CreateDatabase` 后 0 字节问题（建库后执行 `pragma user_version`）；`CreateDb` 连接串兼容明文/缓存密文。
5. **前端**：`Web/api_build/build.bat` 重生 `api-services`；确认无 `api-json-api`；`build.ps1` 改为 UTF-8 BOM 以免 PS5 解析失败。

## 验证

- `dotnet build` 0 错误
- 租户库文件 size>0（本轮 4096），可 sqlite 打开（业务表 0 张属 SysTable 过滤预期）
- swagger 登录 + hardwareInfo 200
- api-services 已重生且无 APIJSON 客户端

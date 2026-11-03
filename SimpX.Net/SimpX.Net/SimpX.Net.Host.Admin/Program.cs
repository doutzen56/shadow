using Microsoft.Extensions.Hosting;
using SimpX.Net.Application.Admin;
using SimpX.Net.Hosting;

// 后台宿主：后台用户令牌 + 内部签名 + 开放接口签名；可选能力按 Admin 预设全开
var capabilities = HostCapabilities.Admin();
// 本地 Sqlite 通常还没有 site_Host：Development 下不因认不到站点直接失败，连上业务库后再去掉这层软关。
if (IsDevelopmentHost())
    capabilities.FailIfSiteHostUnresolved = false;

HostLauncher.Run<AdminAuthenticationComponent>(capabilities);

/// <summary>
/// 是否按 Development 环境起宿主（读 ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT）。
/// </summary>
/// <returns>是 Development 返回 true</returns>
static bool IsDevelopmentHost()
{
    var name = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    return string.Equals(name, Environments.Development, StringComparison.OrdinalIgnoreCase);
}

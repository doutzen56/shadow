using Microsoft.Extensions.Hosting;
using SimpX.Net.Application.WebApi;
using SimpX.Net.Hosting;

// WebApi 宿主：会员令牌；验证码/SignalR/文件等默认关，要开在 HostCapabilities.WebApi() 上改或覆盖
var capabilities = HostCapabilities.WebApi();
// 本地 Sqlite 常无租户域名 / site_Host：Development 下软关，业务库联调后再收紧。
if (IsDevelopmentHost())
{
    capabilities.FailIfTenantHostUnresolved = false;
    capabilities.FailIfSiteHostUnresolved = false;
}

HostLauncher.Run<MemberAuthenticationComponent>(capabilities);

/// <summary>
/// 是否按 Development 环境起宿主。
/// </summary>
static bool IsDevelopmentHost()
{
    var name = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    return string.Equals(name, Environments.Development, StringComparison.OrdinalIgnoreCase);
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// WebApi 应用启动。认证由宿主选择的 <see cref="MemberAuthenticationComponent"/> 注册。
/// </summary>
[AppStartup(100)]
public class Startup : AppStartup
{
    /// <summary>
    /// 注册 WebApi 应用服务（目前无额外服务，会员服务按约定自动注册）。
    /// </summary>
    /// <param name="services">服务集合</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // 游戏推送 Mock 可入账（依赖宿主已配 BetWin30Biz）
        services.AddSharedChannelsMoneyServices();
    }

    /// <summary>
    /// 请求处理链由 Hosting 统一配置，这里不追加。
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="env">宿主环境</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}

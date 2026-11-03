using SimpX.Net.Contracts.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台应用启动。登录、租户平台、作业远程管理；不引用 Application.Admin。
/// </summary>
[AppStartup(100)]
public class Startup : AppStartup
{
    /// <summary>
    /// 注册总后台专属服务。
    /// </summary>
    /// <param name="services">服务集合</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // Shared 登录/租户平台服务不自动注入，总后台显式挂上
        services.AddSharedSuperAdminServices();

        // 作业远程管理：转发给 Schedule 宿主（Contracts.Jobs + 内部签名）
        services.AddInternalCallClient(JobManagementRoutes.ScheduleService);
        services.AddHttpRemote(builder => builder.AddHttpDeclarative<IJobManagementApi>());
    }

    /// <summary>
    /// 请求处理链由 Hosting 统一配置。
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="env">宿主环境</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}

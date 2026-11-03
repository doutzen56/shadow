using SimpX.Net.Application.Shared;
using SimpX.Net.Contracts.Jobs;
using SimpX.Net.ElasticSearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// Job 应用启动：调度（持久化、监视器、集群选举）、看板登录、调 Admin 的客户端。认证由宿主选择的认证组件注册。
/// 引用了本应用的宿主就有调度能力，不再靠角色开关。
/// </summary>
[AppStartup(100)]
public class Startup : AppStartup
{
    /// <summary>
    /// 注册 Job 应用的服务。
    /// </summary>
    /// <param name="services">服务集合</param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConfigurableOptions<ScheduleHostOptions>();
        services.AddSingleton<IScheduleDashboardLogin, ConfigScheduleDashboardLogin>();
        services.AddSharedScheduleQueueServices();

        // 用户游戏日结：业务 ES 聚合（与 Admin 注单明细同集群配置；容器可缺，工厂容忍）
        services.AddElasticSearch();
        services.AddTransient<IUserGameDateEsAggregate>(sp =>
            new UserGameDateEsAggregate(
                sp.GetService<ElasticSearchClientContainer>(),
                sp.GetService<ILogger<UserGameDateEsAggregate>>()));
        services.AddTransient<IJackpotEsTopQuery>(sp =>
            new JackpotEsTopQuery(
                sp.GetService<ElasticSearchClientContainer>(),
                sp.GetService<ILogger<JackpotEsTopQuery>>()));

        // 调 Admin 的系统维护接口：命名 HttpClient 负责签名和拆统一结果，Furion 声明式接口负责请求定义
        services.AddInternalCallClient(JobManagementRoutes.AdminService);
        services.AddHttpRemote(builder => builder.AddHttpDeclarative<IAdminMaintenanceApi>());

        services.AddSchedule(options =>
        {
            options.AddPersistence<DbJobPersistence>();
            options.AddMonitor<JobMonitor>();
            options.AddClusterServer<JobClusterServer>();
        });
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

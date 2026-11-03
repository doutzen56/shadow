using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpX.Net.Core;
using System;
using System.Linq;

namespace SimpX.Net.Hosting;

/// <summary>
/// 所有宿主共用的启动入口。宿主的组成在 Program.cs 里一眼可见：
/// <c>HostLauncher.Run&lt;认证组件&gt;(HostCapabilities.Admin)</c>，可选能力按预设点选。
/// </summary>
public static class HostLauncher
{
    /// <summary>
    /// 用 Furion 组件装配并启动宿主：Web 公共设置 → 认证（宿主自选）→ 公共服务 → MVC → 请求处理链。
    /// 认证一定要排在公共服务和 MVC 前面：SignalR、MVC 注册时会顺带注册 ASP.NET 默认授权，
    /// Furion 看到已注册就跳过自己的，权限检查就不生效。注册错了启动时会直接报错（见 <see cref="SimpX.Net.Feature.Auth.JwtAuthenticationExtensions"/>）。
    /// 中间件顺序只由请求处理链组件决定。
    /// </summary>
    /// <typeparam name="TAuthenticationComponent">宿主的认证组件，如后台用户、会员、仅内部调用</typeparam>
    /// <param name="capabilities">可选能力预设；不传则全部可选能力关闭（只保留内核公共服务）</param>
    public static void Run<TAuthenticationComponent>(HostCapabilityOptions capabilities = null)
        where TAuthenticationComponent : class, IServiceComponent, new()
    {
        HostCapabilities.Current = capabilities ?? new HostCapabilityOptions();
        HostTenantMode.ResolveByHost = HostCapabilities.Current.ResolveTenantByHost;
        HostTenantMode.IsPlatformHost = HostCapabilities.Current.IsPlatformHost;

        Serve.Run(RunOptions.Default
            .ConfigureBuilder(builder => builder.Host.UseWindowsService())
            .AddWebComponent<HostWebComponent>()
            .AddComponent<TAuthenticationComponent>()
            .AddComponent<HostServicesComponent>()
            .AddComponent<HostMvcComponent>()
            .UseComponent<HostPipelineComponent>());
    }
}

/// <summary>
/// 宿主公共 Web 组件：日志过滤和 Kestrel 超时、上传大小设置。
/// </summary>
public sealed class HostWebComponent : IWebComponent
{
    private static readonly string[] MutedCategories = { "Microsoft.Hosting", "Microsoft.AspNetCore" };

    /// <summary>
    /// 在构建 WebApplication 前应用公共设置。
    /// </summary>
    /// <param name="builder">WebApplication 构建器</param>
    /// <param name="componentContext">组件上下文</param>
    public void Load(WebApplicationBuilder builder, ComponentContext componentContext)
    {
        builder.Logging.AddFilter((provider, category, logLevel) =>
            !MutedCategories.Any(u => category.StartsWith(u)) && logLevel >= LogLevel.Information);

        // 长连接、大文件上传场景：放宽超时，不限请求体大小
        builder.WebHost.ConfigureKestrel(u =>
        {
            u.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
            u.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
            u.Limits.MaxRequestBodySize = null;
        });
    }
}

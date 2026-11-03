using Furion.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SimpX.Net.Feature.Auth;

/// <summary>
/// JWT 认证注册。各宿主通过认证组件选用对应的授权处理器，公共层不关心是哪个业务。
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// 注册 Furion JWT 认证并开启全局授权；支持从查询串 token 读令牌（SignalR、文件下载等场景用不了请求头）。
    /// Furion 内部用“已注册就跳过”的方式加授权服务。在它之前如果有代码调过 AddMvcCore、AddSignalR、AddAuthorization，
    /// 授权处理器和策略提供方会被悄悄跳过，按钮权限检查就不执行。所以宿主要先加载认证组件，应用层不要自己注册 MVC 或授权，
    /// 启动时由 <see cref="EnsureAuthorizationRegistered"/> 检查，注册不对直接启动失败。
    /// </summary>
    /// <typeparam name="THandler">授权处理器，如 <see cref="AdminJwtHandler"/></typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>身份验证构建器，可继续追加签名等方案</returns>
    public static AuthenticationBuilder AddJwtAuthentication<THandler>(this IServiceCollection services)
        where THandler : AppAuthorizeHandler
    {
        return services.AddJwt<THandler>(enableGlobalAuthorize: true, jwtBearerConfigure: options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // SignalR accessTokenFactory 带 access_token；旧前端/下载链接仍用 token
                    var query = context.HttpContext.Request.Query;
                    if (query.TryGetValue("access_token", out var accessToken) && !string.IsNullOrWhiteSpace(accessToken))
                        context.Token = accessToken;
                    else if (query.TryGetValue("token", out var token) && !string.IsNullOrWhiteSpace(token))
                        context.Token = token;
                    return Task.CompletedTask;
                }
            };
        });
    }

    /// <summary>
    /// 启动时检查 Furion 的授权是否真的生效：策略提供方要是 Furion 的，授权处理器里要有 <see cref="AppAuthorizeHandler"/>。
    /// 注册顺序被打乱时，ASP.NET 默认实现会顶替 Furion，所有接口都不查按钮权限，所以这里宁可启动失败也不带病运行。
    /// </summary>
    /// <param name="services">已构建好的根服务容器</param>
    /// <exception cref="InvalidOperationException">授权服务被默认实现顶替</exception>
    public static void EnsureAuthorizationRegistered(IServiceProvider services)
    {
        var furionAssembly = typeof(AppAuthorizeHandler).Assembly;

        var policyProvider = services.GetService<IAuthorizationPolicyProvider>();
        if (policyProvider?.GetType().Assembly != furionAssembly)
            throw new InvalidOperationException(
                $"授权策略提供方是 {policyProvider?.GetType().FullName ?? "空"}，不是 Furion 的，权限检查不会生效。" +
                "检查宿主是否先加载了认证组件，以及有没有代码在 AddJwtAuthentication 之前调了 AddMvcCore、AddSignalR、AddAuthorization 等。");

        var handlers = services.GetServices<IAuthorizationHandler>();
        if (!handlers.Any(h => h is AppAuthorizeHandler))
            throw new InvalidOperationException(
                "没有注册 Furion 授权处理器（AppAuthorizeHandler），权限检查不会生效。" +
                "检查宿主是否先加载了认证组件，以及有没有代码在 AddJwtAuthentication 之前调了 AddMvcCore、AddSignalR、AddAuthorization 等。");
    }
}

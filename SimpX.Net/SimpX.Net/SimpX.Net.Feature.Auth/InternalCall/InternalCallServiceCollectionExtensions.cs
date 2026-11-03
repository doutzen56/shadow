using Furion.HttpRemote;

namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 注册调用其它宿主用的命名 HttpClient。配合 Furion 声明式请求：接口上写 [HttpClientName(宿主名)] 即可。
/// </summary>
public static class InternalCallServiceCollectionExtensions
{
    /// <summary>
    /// 为目标宿主注册命名 HttpClient：站点根取 InternalCall:Services:{serviceName}，自动签名，
    /// 并让 Furion 按统一结果（AdminResult 的 Result 字段）自动拆包。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="serviceName">目标宿主名，同时作为 HttpClient 名称</param>
    /// <returns>同一个服务集合</returns>
    /// <exception cref="AppFriendlyException">未配置目标宿主地址</exception>
    public static IServiceCollection AddInternalCallClient(this IServiceCollection services, string serviceName)
    {
        services.TryAddInternalCallHandler();

        services.AddHttpClient(serviceName, (sp, client) =>
            {
                var opt = sp.GetRequiredService<IOptionsMonitor<InternalCallOptions>>().CurrentValue;
                if (opt.Services == null || !opt.Services.TryGetValue(serviceName, out var baseUrl) || string.IsNullOrWhiteSpace(baseUrl))
                    throw Oops.Oh($"内部调用未配置目标宿主地址：InternalCall:Services:{serviceName}");

                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds <= 0 ? 30 : opt.TimeoutSeconds);
            })
            .AddHttpMessageHandler<InternalCallHandler>();

        services.Configure<HttpClientOptions>(serviceName, options =>
        {
            options.UseJsonResponseWrapper = true;
            options.JsonResponseWrapper = new JsonResponseWrapper(typeof(AdminResult<>), nameof(AdminResult<object>.Result));
        });

        return services;
    }

    /// <summary>
    /// 处理器只注册一次（瞬时，每个 HttpClient 的请求处理链各自一份）。
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void TryAddInternalCallHandler(this IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(InternalCallHandler))) return;
        services.AddTransient<InternalCallHandler>();
    }
}

namespace SimpX.Net.Feature.Http;

/// <summary>
/// 注册 <see cref="IHttpCaller"/>。需在宿主已调用 <c>AddHttpRemote()</c> 之后使用。
/// </summary>
public static class HttpCallerServiceCollectionExtensions
{
    /// <summary>
    /// 注册外部 HTTP 调用封装（Furion HttpRemote 薄封装）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>同一服务集合</returns>
    public static IServiceCollection AddHttpCaller(this IServiceCollection services)
    {
        services.AddTransient<IHttpCaller, HttpCaller>();
        return services;
    }
}

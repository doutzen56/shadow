namespace SimpX.Net.Data;

/// <summary>
/// 按请求域名解析租户。Admin、WebApi 等按域名确定租户的宿主用；总后台不按域名确定租户。
/// </summary>
public interface ITenantHostResolver
{
    /// <summary>
    /// 用 Host 匹配已启用租户的绑定域名；匹配不到返回 null。
    /// </summary>
    /// <param name="host">请求主机名（不含端口），大小写不敏感</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>租户 Id；未绑定或未启用时为 null</returns>
    Task<long?> ResolveTenantIdAsync(string host, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用 Host 取已启用租户整行；匹配不到返回 null。不要把连接串回给前端。
    /// </summary>
    /// <param name="host">请求主机名（不含端口），大小写不敏感</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>租户；未绑定或未启用时为 null</returns>
    Task<SysTenant> ResolveTenantAsync(string host, CancellationToken cancellationToken = default);
}

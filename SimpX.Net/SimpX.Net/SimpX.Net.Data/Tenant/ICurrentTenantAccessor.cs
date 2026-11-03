namespace SimpX.Net.Data;

/// <summary>
/// 当前请求的租户上下文。只读；按域名确定租户由宿主请求处理链完成，业务不要自己按域名解析。
/// </summary>
public interface ICurrentTenantAccessor
{
    /// <summary>
    /// 当前请求已解析的租户 Id；还没按域名确定租户时为 null。
    /// </summary>
    long? TenantId { get; }

    /// <summary>
    /// 确定租户时用的规范化 Host；未按域名确定租户时为 null。
    /// </summary>
    string Host { get; }

    /// <summary>
    /// 租户站进程中取已确定的租户 Id；没有则抛业务异常（域名未绑定）。
    /// </summary>
    /// <returns>租户 Id</returns>
    long RequireTenantId();
}

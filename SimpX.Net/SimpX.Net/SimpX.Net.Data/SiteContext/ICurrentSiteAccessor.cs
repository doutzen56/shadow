namespace SimpX.Net.Data;

/// <summary>
/// 当前请求的站点上下文。只读；按域名解析站点由请求处理链完成，业务不要自己再解析域名。
/// </summary>
public interface ICurrentSiteAccessor
{
    /// <summary>
    /// 当前请求已解析的 SiteID；还没解析到站点时为 null。
    /// </summary>
    int? SiteId { get; }

    /// <summary>
    /// 用来确定当前站点的主机名；还没解析到站点时为 null。
    /// </summary>
    string Host { get; }

    /// <summary>
    /// 需要当前站点时取 SiteID；没有则抛业务异常。
    /// </summary>
    /// <returns>站点编号</returns>
    int RequireSiteId();
}

namespace SimpX.Net.Data;

/// <summary>
/// 按请求主机名解析站点编号。只解析站点，不解析租户。
/// </summary>
public interface ISiteHostResolver
{
    /// <summary>
    /// 根据主机名解析 SiteID；无法解析时返回 null。
    /// </summary>
    /// <param name="host">主机名或带端口的 Authority</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>站点编号；解析失败为 null</returns>
    Task<int?> ResolveSiteIdAsync(string host, CancellationToken cancellationToken = default);
}

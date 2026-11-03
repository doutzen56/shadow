namespace SimpX.Net.Application.Shared;

/// <summary>
/// 从会员 Cookie 解析当前站用户编号（兼容会话）。
/// </summary>
public interface IMemberCookieUserIdResolver
{
    /// <summary>
    /// 解析 Cookie 中的会员编号；无效返回 null。
    /// </summary>
    /// <param name="siteId">站点编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会员编号</returns>
    Task<int?> TryResolveUserIdFromCookieAsync(int siteId, CancellationToken cancellationToken = default);
}

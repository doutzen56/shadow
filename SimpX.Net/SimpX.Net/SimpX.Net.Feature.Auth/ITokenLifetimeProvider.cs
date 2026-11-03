namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 令牌有效期来源。后台可以从系统配置里读，没有实现时按 JWT 配置文件里的值。
/// </summary>
public interface ITokenLifetimeProvider
{
    /// <summary>
    /// 访问令牌有效期（分钟）。
    /// </summary>
    /// <returns>分钟数</returns>
    Task<int> GetAccessTokenExpireAsync();

    /// <summary>
    /// 刷新令牌有效期（分钟）。
    /// </summary>
    /// <returns>分钟数</returns>
    Task<int> GetRefreshTokenExpireAsync();
}

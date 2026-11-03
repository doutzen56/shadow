namespace SimpX.Net.Application.Shared;

/// <summary>
/// 用户登录结果
/// </summary>
public class LoginOutput
{
    /// <summary>
    /// 令牌Token
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// 刷新Token
    /// </summary>
    public string RefreshToken { get; set; }

    /// <summary>
    /// 个性化首页
    /// </summary>
    public string? Homepage { get; set; }
}
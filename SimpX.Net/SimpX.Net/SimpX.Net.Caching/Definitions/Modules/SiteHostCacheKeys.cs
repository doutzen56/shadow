namespace SimpX.Net.Caching;

/// <summary>
/// 兼容登录会话用的缓存键。新加缓存只在本文件加常量即可。
/// </summary>
public static class SiteHostCacheKeys
{
    /// <summary>模块名。</summary>
    public const string Module = "betwin";

    /// <summary>会员会话：Token → UserId。</summary>
    public const string UserToken = "BetWin:UserToken";

    /// <summary>站点后台会话：Token → AdminId。</summary>
    public const string AdminToken = "BetWin:AdminToken";

    /// <summary>站点后台会话所属站点：Token → SiteId。</summary>
    public const string AdminTokenSite = "BetWin:AdminTokenSite";

    /// <summary>总后台会话：Token → SystemAdminId。</summary>
    public const string SystemAdminToken = "BetWin:SystemAdminToken";

    /// <summary>登录失败计数：类型_主体 → 次数与窗口。</summary>
    public const string LoginFail = "Auth:LoginFail";

    /// <summary>站点后台错密次数：AdminId → 次数（跟旧版 ADMIN_LOGIN_TIME 对应）。</summary>
    public const string SiteAdminLoginFail = "Auth:SiteAdminLoginFail";

    /// <summary>未知账号错密次数（按 IP，当天清零）：IP → 次数。</summary>
    public const string SiteAdminUnknownFail = "Auth:SiteAdminUnknownFail";
}

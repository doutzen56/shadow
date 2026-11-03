namespace SimpX.Net.Data;

/// <summary>
/// 把按域名解析到的站点写入 HttpContext.Items 时用的键。跟租户解析用的键分开，禁止混用。
/// </summary>
public static class SiteHostKeys
{
    /// <summary>用来确定当前站点的主机名（可能来自 X-Forwarded-HostX）。</summary>
    public const string ResolvedHost = "BetWin.ResolvedSiteHost";

    /// <summary>已解析的站点编号 SiteID。</summary>
    public const string ResolvedSiteId = "BetWin.ResolvedSiteId";
}

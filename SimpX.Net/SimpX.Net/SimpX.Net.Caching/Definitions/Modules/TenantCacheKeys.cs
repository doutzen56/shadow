namespace SimpX.Net.Caching;

/// <summary>
/// 租户与开放接口缓存键。新加缓存只在本文件加常量即可。
/// </summary>
public static class TenantCacheKeys
{
    /// <summary>模块名。</summary>
    public const string Module = "tenant";

    /// <summary>租户信息。</summary>
    public const string Tenant = "tenant";

    /// <summary>开放接口身份。</summary>
    public const string OpenAccess = "open-access";
}

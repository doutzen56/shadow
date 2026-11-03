namespace SimpX.Net.Caching;

/// <summary>
/// 配置相关缓存键。新加缓存只在本文件加常量即可。
/// </summary>
public static class ConfigCacheKeys
{
    /// <summary>模块名。</summary>
    public const string Module = "config";

    /// <summary>系统配置。</summary>
    public const string Config = "config";

    /// <summary>租户配置。</summary>
    public const string TenantConfig = "tenant-config";

    /// <summary>用户配置。</summary>
    public const string UserConfig = "user-config";
}

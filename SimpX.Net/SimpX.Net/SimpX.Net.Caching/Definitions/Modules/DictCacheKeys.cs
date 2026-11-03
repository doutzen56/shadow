namespace SimpX.Net.Caching;

/// <summary>
/// 字典与常量缓存键。新加缓存只在本文件加常量即可。
/// </summary>
public static class DictCacheKeys
{
    /// <summary>模块名。</summary>
    public const string Module = "dict";

    /// <summary>平台字典。</summary>
    public const string Dict = "dict";

    /// <summary>租户字典。</summary>
    public const string TenantDict = "tenant-dict";

    /// <summary>常量下拉。</summary>
    public const string Const = "const";
}

namespace SimpX.Net.Caching;

/// <summary>
/// 权限相关缓存键。新加缓存只在本文件加常量即可。
/// </summary>
public static class PermissionCacheKeys
{
    /// <summary>模块名。</summary>
    public const string Module = "permission";

    /// <summary>用户按钮权限。</summary>
    public const string UserButton = "user-button";

    /// <summary>系统全部按钮权限。</summary>
    public const string AllButton = "all-button";

    /// <summary>用户数据范围（机构 + 最大范围）。</summary>
    public const string UserDataScope = "user-data-scope";
}

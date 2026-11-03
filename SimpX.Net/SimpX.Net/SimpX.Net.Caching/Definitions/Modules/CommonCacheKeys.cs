namespace SimpX.Net.Caching;

/// <summary>
/// 通用状态缓存键。新加缓存只在本文件加常量即可。
/// </summary>
public static class CommonCacheKeys
{
    /// <summary>模块名。</summary>
    public const string Module = "common";

    /// <summary>分布式锁。</summary>
    public const string Lock = "lock";

    /// <summary>幂等。</summary>
    public const string Idempotent = "idempotent";

    /// <summary>在线用户哈希。</summary>
    public const string OnlineUser = "online-user";

    /// <summary>在线用户哈希共用的标识，所有连接都放在这一条里（不是缓存名）。</summary>
    [CacheKeyIgnore]
    public const string OnlineUserId = "all";

    /// <summary>Excel 临时文件。</summary>
    public const string ExcelTemp = "excel-temp";

    /// <summary>用户表格列自定义（按用户+表格）。</summary>
    public const string ColumnCustom = "column-custom";
}

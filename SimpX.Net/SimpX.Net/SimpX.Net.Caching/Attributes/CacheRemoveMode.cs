namespace SimpX.Net.Caching;

/// <summary>
/// 缓存失效方式。
/// </summary>
public enum CacheRemoveMode
{
    /// <summary>
    /// 按完整 Key 删一条。
    /// </summary>
    ByKey = 0,

    /// <summary>
    /// 按缓存名删该定义下全部 Key（前缀扫描）。
    /// </summary>
    ByName = 1
}

namespace SimpX.Net.Caching;

/// <summary>
/// 缓存类别：由走数据接口还是状态接口在首次使用时打上，不在 key 文件里手写。
/// 数据类可读可失效、查库兜底；状态类 Redis 不可用时直接失败。
/// </summary>
public enum CacheKind
{
    /// <summary>
    /// 业务数据缓存。
    /// </summary>
    Data = 0,

    /// <summary>
    /// 状态数据（锁、计数、一次性值、黑名单等）。
    /// </summary>
    State = 1
}

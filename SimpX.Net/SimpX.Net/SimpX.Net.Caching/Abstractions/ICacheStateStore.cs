namespace SimpX.Net.Caching;

/// <summary>
/// 状态数据：锁、计数、一次性值、哈希。走本接口即约定为状态类；Redis 出错时直接失败。
/// 过期时间由调用方传入；不过期则传 null。
/// </summary>
public interface ICacheStateStore
{
    /// <summary>
    /// 申请锁；用完释放返回值。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">锁标识</param>
    /// <param name="waitMs">最多等待毫秒</param>
    /// <param name="expireMs">锁过期毫秒</param>
    /// <returns>锁</returns>
    IDisposable AcquireLock(string cacheName, object id, int waitMs = 500, int expireMs = 10000);

    /// <summary>
    /// 原子自增；第一次写入时按 expire 设过期（null 表示不过期）。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识</param>
    /// <param name="expire">首次自增时的过期时间；null 不过期</param>
    /// <returns>自增后的值</returns>
    long Increment(string cacheName, object id, TimeSpan? expire = null);

    /// <summary>
    /// 不存在才写入。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识</param>
    /// <param name="value">值</param>
    /// <param name="expire">过期时间；null 不过期</param>
    /// <returns>写成了返回 true</returns>
    bool TryAdd<T>(string cacheName, object id, T value, TimeSpan? expire = null);

    /// <summary>
    /// 读出后删除；并发时只有一个调用方能拿到。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识</param>
    /// <param name="value">读到的值</param>
    /// <returns>拿到返回 true</returns>
    bool TryTake<T>(string cacheName, object id, out T? value);

    /// <summary>
    /// 写入（覆盖）。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识</param>
    /// <param name="value">值</param>
    /// <param name="expire">过期时间；null 不过期</param>
    void Add<T>(string cacheName, object id, T value, TimeSpan? expire = null);

    /// <summary>
    /// 给已有键改过期时间。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识</param>
    /// <param name="expire">过期时间，必须大于 0</param>
    void SetExpire(string cacheName, object id, TimeSpan expire);

    /// <summary>
    /// 读取。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识</param>
    /// <returns>值</returns>
    T? Get<T>(string cacheName, object id);

    /// <summary>
    /// 是否存在。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识</param>
    /// <returns>存在返回 true</returns>
    bool Exists(string cacheName, object id);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识</param>
    void Remove(string cacheName, object id);

    /// <summary>
    /// 哈希写字段。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">哈希 Key 的业务标识</param>
    /// <param name="field">字段名</param>
    /// <param name="value">值</param>
    void HashSet<T>(string cacheName, object id, string field, T value);

    /// <summary>
    /// 哈希删字段。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">哈希标识</param>
    /// <param name="fields">字段名</param>
    void HashRemove(string cacheName, object id, params string[] fields);

    /// <summary>
    /// 哈希全部值。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">哈希标识</param>
    /// <returns>全部值</returns>
    ICollection<T> HashValues<T>(string cacheName, object id);
}

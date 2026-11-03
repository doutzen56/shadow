namespace SimpX.Net.Caching;

/// <summary>
/// 业务数据缓存。过期时间由调用方传入；类别由走本接口约定为数据类。
/// </summary>
public interface ISimpXCache
{
    /// <summary>
    /// 写入：从实体上取 Id 拼 Key（约定有 Id 属性或实现 <see cref="ICacheIdentifiable"/>）。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名或「模块:名称」</param>
    /// <param name="value">要缓存的实体</param>
    /// <param name="expire">过期时间</param>
    void Add<T>(string cacheName, T value, TimeSpan expire);

    /// <summary>
    /// 写入：标识与值分开传（值是字符串、列表等抽不出 Id 时用）。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">业务标识</param>
    /// <param name="value">值；null 不写</param>
    /// <param name="expire">过期时间</param>
    void Add<T>(string cacheName, object id, T? value, TimeSpan expire);

    /// <summary>
    /// 没有则查库并写入。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">业务标识</param>
    /// <param name="factory">查库</param>
    /// <param name="expire">写入时的过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存或查库结果</returns>
    Task<T?> GetOrAddAsync<T>(
        string cacheName,
        object? id,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan expire,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步版 GetOrAdd，给过滤器等同步场景用。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">业务标识</param>
    /// <param name="factory">查库</param>
    /// <param name="expire">写入时的过期时间</param>
    /// <returns>缓存或查库结果</returns>
    T? GetOrAdd<T>(string cacheName, object? id, Func<T?> factory, TimeSpan expire);

    /// <summary>
    /// 只读，不存在返回默认值。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">业务标识</param>
    /// <returns>缓存值</returns>
    T? Get<T>(string cacheName, object? id);

    /// <summary>
    /// 按标识删一条。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">业务标识</param>
    void Remove(string cacheName, object? id);

    /// <summary>
    /// 从实体取 Id 后删除。
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="cacheName">缓存名</param>
    /// <param name="value">实体</param>
    void RemoveEntity<T>(string cacheName, T value);

    /// <summary>
    /// 删掉该缓存名下全部 Key。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    void RemoveAll(string cacheName);
}

namespace SimpX.Net.Caching;

/// <summary>
/// 执行一条缓存失效。
/// </summary>
public interface ICacheInvalidationExecutor
{
    /// <summary>
    /// 按 Attribute 和参数值失效；失败应记录并由调用方决定是否抛出。提交后的统一执行不向外抛。
    /// </summary>
    /// <param name="attribute">失效声明</param>
    /// <param name="argumentValue">解析出的参数值；ByName 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExecuteAsync(
        CacheRemoveAttribute attribute,
        object? argumentValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按缓存名和标识失效一条。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识；整组删时传 null</param>
    /// <param name="mode">方式</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExecuteAsync(
        string cacheName,
        object? id,
        CacheRemoveMode mode,
        CancellationToken cancellationToken = default);
}

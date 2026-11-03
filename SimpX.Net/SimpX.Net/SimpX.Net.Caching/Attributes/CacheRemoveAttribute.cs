namespace SimpX.Net.Caching;

/// <summary>
/// 声明业务方法成功后要失效的缓存。本身不执行，由过滤器和待失效清单在事务提交后处理。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class CacheRemoveAttribute : Attribute
{
    /// <summary>
    /// 按 Key 失效：需要参数路径；按名称整组失效时参数路径可空。
    /// </summary>
    /// <param name="cacheName">缓存名或「模块:名称」</param>
    /// <param name="argumentPath">方法参数名，或「参数名.属性名」一层；ByName 时可空</param>
    /// <param name="mode">失效方式</param>
    public CacheRemoveAttribute(
        string cacheName,
        string? argumentPath = null,
        CacheRemoveMode mode = CacheRemoveMode.ByKey)
    {
        if (string.IsNullOrWhiteSpace(cacheName))
            throw new ArgumentException("缓存名不能为空。", nameof(cacheName));

        if (mode == CacheRemoveMode.ByKey && string.IsNullOrWhiteSpace(argumentPath))
            throw new ArgumentException("按 Key 失效时必须指定参数路径。", nameof(argumentPath));

        CacheName = cacheName;
        ArgumentPath = argumentPath;
        Mode = mode;
    }

    /// <summary>
    /// 缓存名或「模块:名称」。
    /// </summary>
    public string CacheName { get; }

    /// <summary>
    /// 参数路径，例如 userId 或 input.UserId。
    /// </summary>
    public string? ArgumentPath { get; }

    /// <summary>
    /// 失效方式。
    /// </summary>
    public CacheRemoveMode Mode { get; }
}

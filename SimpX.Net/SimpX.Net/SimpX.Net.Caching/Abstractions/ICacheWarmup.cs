namespace SimpX.Net.Caching;

/// <summary>
/// 某一类缓存的预热：核心库不查库，具体实现放在业务项目。
/// </summary>
public interface ICacheWarmup
{
    /// <summary>
    /// 要预热的缓存「模块:名称」或名称。
    /// </summary>
    string CacheName { get; }

    /// <summary>
    /// 是否必须在启动时成功；失败则宿主不启动。
    /// </summary>
    bool RequiredOnStartup { get; }

    /// <summary>
    /// 执行预热。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task WarmupAsync(CancellationToken cancellationToken = default);
}

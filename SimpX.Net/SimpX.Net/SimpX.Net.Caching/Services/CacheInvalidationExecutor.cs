using Microsoft.Extensions.Logging;

namespace SimpX.Net.Caching;

/// <summary>
/// 统一失效执行器：按 Key 或按缓存名扫描删除。异常向上抛，由 Batch 决定是否吞掉。
/// </summary>
public sealed class CacheInvalidationExecutor : ICacheInvalidationExecutor
{
    private readonly ISimpXCache _cache;
    private readonly ICacheStateStore _state;
    private readonly CacheRegistry _registry;
    private readonly ILogger<CacheInvalidationExecutor> _logger;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    public CacheInvalidationExecutor(
        ISimpXCache cache,
        ICacheStateStore state,
        CacheRegistry registry,
        ILogger<CacheInvalidationExecutor> logger)
    {
        _cache = cache;
        _state = state;
        _registry = registry;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(
        CacheRemoveAttribute attribute,
        object? argumentValue,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(attribute.CacheName, argumentValue, attribute.Mode, cancellationToken);
    }

    /// <inheritdoc />
    public Task ExecuteAsync(
        string cacheName,
        object? id,
        CacheRemoveMode mode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var definition = _registry.GetRequired(cacheName);

        switch (mode)
        {
            case CacheRemoveMode.ByKey:
                if (definition.Kind == CacheKind.State)
                    _state.Remove(cacheName, id!);
                else
                    _cache.Remove(cacheName, id);
                break;

            case CacheRemoveMode.ByName:
                if (definition.Kind == CacheKind.State)
                {
                    throw new InvalidOperationException(
                        $"状态类缓存 '{definition.RegistryKey}' 不支持按名称整组失效（避免误清锁和黑名单）。");
                }

                _cache.RemoveAll(cacheName);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        _logger.LogDebug(
            "缓存失效完成。Cache={Cache}, Mode={Mode}",
            definition.RegistryKey,
            mode);
        return Task.CompletedTask;
    }
}

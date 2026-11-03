using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewLife.Caching;

namespace SimpX.Net.Caching;

/// <summary>
/// 数据缓存实现：走 NewLife ICache；过期由调用方传入。
/// </summary>
public sealed class SimpXCache : ISimpXCache
{
    private readonly ICacheProvider _cacheProvider;
    private readonly CacheRegistry _registry;
    private readonly CacheKeyBuilder _keys;
    private readonly IServiceProvider _services;
    private readonly ILogger<SimpXCache> _logger;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="cacheProvider">底层缓存</param>
    /// <param name="registry">缓存名表</param>
    /// <param name="keys">拼 Key</param>
    /// <param name="services">用于读主库范围</param>
    /// <param name="logger">日志</param>
    public SimpXCache(
        ICacheProvider cacheProvider,
        CacheRegistry registry,
        CacheKeyBuilder keys,
        IServiceProvider services,
        ILogger<SimpXCache> logger)
    {
        _cacheProvider = cacheProvider;
        _registry = registry;
        _keys = keys;
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Add<T>(string cacheName, T value, TimeSpan expire)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsurePositive(expire);
        Add(cacheName, CacheEntityId.Get(value), value, expire);
    }

    /// <inheritdoc />
    public void Add<T>(string cacheName, object id, T? value, TimeSpan expire)
    {
        EnsurePositive(expire);
        var definition = RequireData(cacheName);
        if (value is null) return;
        var key = _keys.Build(definition, id);
        try
        {
            _cacheProvider.Cache.Set(key, value, expire);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "写数据缓存失败。Key={Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetOrAddAsync<T>(
        string cacheName,
        object? id,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan expire,
        CancellationToken cancellationToken = default)
    {
        EnsurePositive(expire);
        var definition = RequireData(cacheName);
        var key = _keys.Build(definition, id);

        try
        {
            var cached = _cacheProvider.Cache.Get<T>(key);
            if (cached is not null)
                return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读数据缓存失败，改为查库。Key={Key}", key);
        }

        using var cacheLock = TryLock(key);
        try
        {
            var again = _cacheProvider.Cache.Get<T>(key);
            if (again is not null)
                return again;
        }
        catch
        {
            // 第二次读失败继续查库
        }

        T? value;
        using (BeginReadThrough())
            value = await factory(cancellationToken).ConfigureAwait(false);

        if (value is null)
            return value;

        try
        {
            _cacheProvider.Cache.Set(key, value, expire);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "写数据缓存失败，仍返回查库结果。Key={Key}", key);
        }

        return value;
    }

    /// <inheritdoc />
    public T? GetOrAdd<T>(string cacheName, object? id, Func<T?> factory, TimeSpan expire)
    {
        EnsurePositive(expire);
        var definition = RequireData(cacheName);
        var key = _keys.Build(definition, id);

        try
        {
            var cached = _cacheProvider.Cache.Get<T>(key);
            if (cached is not null)
                return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读数据缓存失败，改为查库。Key={Key}", key);
        }

        using var cacheLock = TryLock(key);
        try
        {
            var again = _cacheProvider.Cache.Get<T>(key);
            if (again is not null)
                return again;
        }
        catch
        {
            // ignore
        }

        T? value;
        using (BeginReadThrough())
            value = factory();

        if (value is null)
            return value;

        try
        {
            _cacheProvider.Cache.Set(key, value, expire);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "写数据缓存失败，仍返回查库结果。Key={Key}", key);
        }

        return value;
    }

    /// <inheritdoc />
    public T? Get<T>(string cacheName, object? id)
    {
        var definition = RequireData(cacheName);
        var key = _keys.Build(definition, id);
        try
        {
            return _cacheProvider.Cache.Get<T>(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读数据缓存失败。Key={Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public void Remove(string cacheName, object? id)
    {
        var definition = RequireData(cacheName);
        var key = _keys.Build(definition, id);
        _cacheProvider.Cache.Remove(key);
    }

    /// <inheritdoc />
    public void RemoveEntity<T>(string cacheName, T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Remove(cacheName, CacheEntityId.Get(value));
    }

    /// <inheritdoc />
    public void RemoveAll(string cacheName)
    {
        var definition = RequireData(cacheName);
        var namePrefix = _keys.BuildNamePrefix(definition);
        var keys = SearchKeys(namePrefix);
        if (keys.Length > 0)
            _cacheProvider.Cache.Remove(keys);
    }

    private CacheDefinition RequireData(string cacheName)
    {
        var definition = _registry.GetRequired(cacheName);
        _registry.BindKind(definition, CacheKind.Data);
        return definition;
    }

    private IDisposable? TryLock(string key)
    {
        try
        {
            return _cacheProvider.Cache.AcquireLock($"lock:simpx-cache:{key}", 1000);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "缓存加载锁未拿到，直接查库。Key={Key}", key);
            return null;
        }
    }

    private IDisposable? BeginReadThrough()
    {
        var scope = _services.GetService<ICacheReadThroughScope>();
        return scope?.Begin();
    }

    private string[] SearchKeys(string fullPrefix)
    {
        if (_cacheProvider.Cache == Cache.Default)
        {
            return _cacheProvider.Cache.Keys
                .Where(u => u.StartsWith(fullPrefix, StringComparison.Ordinal))
                .ToArray();
        }

        if (_cacheProvider.Cache is FullRedis redis)
            return redis.Search($"{fullPrefix}*", 0, int.MaxValue).ToArray();

        return Array.Empty<string>();
    }

    private static void EnsurePositive(TimeSpan expire)
    {
        if (expire <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expire), "过期时间必须大于 0。");
    }
}

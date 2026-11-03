using NewLife.Caching;

namespace SimpX.Net.Caching;

/// <summary>
/// 状态数据实现：直接用 NewLife；出错向外抛；过期由调用方传入。
/// </summary>
public sealed class CacheStateStore : ICacheStateStore
{
    private readonly ICacheProvider _cacheProvider;
    private readonly CacheRegistry _registry;
    private readonly CacheKeyBuilder _keys;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="cacheProvider">底层缓存</param>
    /// <param name="registry">缓存名表</param>
    /// <param name="keys">拼 Key</param>
    public CacheStateStore(
        ICacheProvider cacheProvider,
        CacheRegistry registry,
        CacheKeyBuilder keys)
    {
        _cacheProvider = cacheProvider;
        _registry = registry;
        _keys = keys;
    }

    /// <inheritdoc />
    public IDisposable AcquireLock(string cacheName, object id, int waitMs = 500, int expireMs = 10000)
    {
        var key = BuildKey(cacheName, id);
        return _cacheProvider.Cache.AcquireLock(key, waitMs, expireMs, throwOnFailure: true)
               ?? throw new InvalidOperationException($"申请锁失败：{key}");
    }

    /// <inheritdoc />
    public long Increment(string cacheName, object id, TimeSpan? expire = null)
    {
        var definition = RequireState(cacheName);
        var key = _keys.Build(definition, id);
        var value = _cacheProvider.Cache.Increment(key, 1L);
        if (value == 1 && expire is { } ttl && ttl > TimeSpan.Zero)
            _cacheProvider.Cache.SetExpire(key, ttl);
        return value;
    }

    /// <inheritdoc />
    public bool TryAdd<T>(string cacheName, object id, T value, TimeSpan? expire = null)
    {
        var definition = RequireState(cacheName);
        var key = _keys.Build(definition, id);
        var expireSeconds = expire is { } ttl && ttl > TimeSpan.Zero
            ? (int)ttl.TotalSeconds
            : -1;
        return _cacheProvider.Cache.Add(key, value, expireSeconds);
    }

    /// <inheritdoc />
    public bool TryTake<T>(string cacheName, object id, out T? value)
    {
        var key = BuildKey(cacheName, id);
        if (!_cacheProvider.Cache.ContainsKey(key))
        {
            value = default;
            return false;
        }

        value = _cacheProvider.Cache.Get<T>(key);
        if (_cacheProvider.Cache.Remove(key) <= 0)
        {
            value = default;
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void Add<T>(string cacheName, object id, T value, TimeSpan? expire = null)
    {
        var definition = RequireState(cacheName);
        var key = _keys.Build(definition, id);
        if (expire is { } ttl && ttl > TimeSpan.Zero)
            _cacheProvider.Cache.Set(key, value, ttl);
        else
            _cacheProvider.Cache.Set(key, value);
    }

    /// <inheritdoc />
    public void SetExpire(string cacheName, object id, TimeSpan expire)
    {
        if (expire <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expire), "过期时间必须大于 0。");
        var key = BuildKey(cacheName, id);
        _cacheProvider.Cache.SetExpire(key, expire);
    }

    /// <inheritdoc />
    public T? Get<T>(string cacheName, object id)
    {
        var key = BuildKey(cacheName, id);
        return _cacheProvider.Cache.Get<T>(key);
    }

    /// <inheritdoc />
    public bool Exists(string cacheName, object id)
    {
        var key = BuildKey(cacheName, id);
        return _cacheProvider.Cache.ContainsKey(key);
    }

    /// <inheritdoc />
    public void Remove(string cacheName, object id)
    {
        var key = BuildKey(cacheName, id);
        _cacheProvider.Cache.Remove(key);
    }

    /// <inheritdoc />
    public void HashSet<T>(string cacheName, object id, string field, T value)
    {
        var key = BuildKey(cacheName, id);
        _cacheProvider.Cache.GetDictionary<T>(key)[field] = value;
    }

    /// <inheritdoc />
    public void HashRemove(string cacheName, object id, params string[] fields)
    {
        var key = BuildKey(cacheName, id);
        var hash = _cacheProvider.Cache.GetDictionary<object>(key);
        foreach (var field in fields)
            hash.Remove(field);
    }

    /// <inheritdoc />
    public ICollection<T> HashValues<T>(string cacheName, object id)
    {
        var key = BuildKey(cacheName, id);
        return _cacheProvider.Cache.GetDictionary<T>(key).Values;
    }

    private string BuildKey(string cacheName, object? id)
    {
        var definition = RequireState(cacheName);
        return _keys.Build(definition, id);
    }

    private CacheDefinition RequireState(string cacheName)
    {
        var definition = _registry.GetRequired(cacheName);
        _registry.BindKind(definition, CacheKind.State);
        return definition;
    }
}

using SimpX.Net.Application.Shared;
using SimpX.Net.Caching;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 登录失败计数纯逻辑（内存假缓存）。
/// </summary>
public class LoginFailCounterTests
{
    /// <summary>
    /// 满 5 次后 CheckLockedSeconds 大于 0；Clear 后可再试。
    /// </summary>
    [Fact]
    public void SaveFail_LocksAfterMax_ThenClear()
    {
        var cache = new MemorySimpXCache();
        var counter = new LoginFailCounter(cache);
        const string subject = "1001";

        for (var i = 0; i < LoginFailCounter.MaxCount; i++)
            counter.SaveFail(subject);

        Assert.True(counter.CheckLockedSeconds(subject) > 0);
        counter.Clear(subject);
        Assert.Equal(0, counter.CheckLockedSeconds(subject));
        Assert.Equal(0, counter.GetCount(subject));
    }

    /// <summary>
    /// FormatRemain 对分钟档返回可读文案。
    /// </summary>
    [Fact]
    public void FormatRemain_Minutes()
    {
        Assert.Contains("分钟", LoginFailCounter.FormatRemain(90));
    }

    /// <summary>
    /// 测试用内存缓存，只实现本测用到的 Get/Add/Remove。
    /// </summary>
    private sealed class MemorySimpXCache : ISimpXCache
    {
        private readonly Dictionary<string, object> _store = new(StringComparer.Ordinal);

        public void Add<T>(string cacheName, T value, TimeSpan expire) =>
            _store[$"{cacheName}:"] = value!;

        public void Add<T>(string cacheName, object id, T? value, TimeSpan expire)
        {
            if (value == null) return;
            _store[$"{cacheName}:{id}"] = value;
        }

        public Task<T?> GetOrAddAsync<T>(string cacheName, object? id, Func<CancellationToken, Task<T?>> factory, TimeSpan expire, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public T? GetOrAdd<T>(string cacheName, object? id, Func<T?> factory, TimeSpan expire) =>
            throw new NotSupportedException();

        public T? Get<T>(string cacheName, object? id)
        {
            if (_store.TryGetValue($"{cacheName}:{id}", out var v) && v is T typed)
                return typed;
            return default;
        }

        public void Remove(string cacheName, object? id) =>
            _store.Remove($"{cacheName}:{id}");

        public void RemoveEntity<T>(string cacheName, T value) => throw new NotSupportedException();

        public void RemoveAll(string cacheName)
        {
            foreach (var key in _store.Keys.Where(k => k.StartsWith(cacheName + ":", StringComparison.Ordinal)).ToList())
                _store.Remove(key);
        }
    }
}

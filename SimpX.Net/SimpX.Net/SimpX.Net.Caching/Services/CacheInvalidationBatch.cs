using Microsoft.Extensions.Logging;

namespace SimpX.Net.Caching;

/// <summary>
/// 当前请求（或作用域）的待失效清单；提交成功后统一执行，执行完清空。
/// </summary>
public sealed class CacheInvalidationBatch
{
    private readonly ICacheInvalidationExecutor _executor;
    private readonly ILogger<CacheInvalidationBatch> _logger;
    private readonly List<(CacheRemoveAttribute Attr, object? Value)> _items = new();
    private readonly object _sync = new();
    private int _executed;

    /// <summary>
    /// 注入执行器和日志。
    /// </summary>
    /// <param name="executor">失效执行器</param>
    /// <param name="logger">日志</param>
    public CacheInvalidationBatch(
        ICacheInvalidationExecutor executor,
        ILogger<CacheInvalidationBatch> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    /// <summary>
    /// 是否还有未执行的登记。
    /// </summary>
    public bool HasPending
    {
        get
        {
            lock (_sync)
                return _items.Count > 0 && _executed == 0;
        }
    }

    /// <summary>
    /// 登记一条 Attribute 失效。
    /// </summary>
    /// <param name="attribute">声明</param>
    /// <param name="argumentValue">参数值</param>
    public void Add(CacheRemoveAttribute attribute, object? argumentValue)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        lock (_sync)
            _items.Add((attribute, argumentValue));
    }

    /// <summary>
    /// 登记按 Key 或按名称失效。
    /// </summary>
    /// <param name="cacheName">缓存名</param>
    /// <param name="id">标识；整组删时传 null</param>
    /// <param name="mode">方式</param>
    public void Add(string cacheName, object? id = null, CacheRemoveMode mode = CacheRemoveMode.ByKey)
    {
        var attr = mode == CacheRemoveMode.ByName
            ? new CacheRemoveAttribute(cacheName, mode: CacheRemoveMode.ByName)
            : new CacheRemoveAttribute(cacheName, "id", CacheRemoveMode.ByKey);
        Add(attr, id);
    }

    /// <summary>
    /// 执行清单；同一作用域只真正执行一次。失败记日志，不向外抛。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        List<(CacheRemoveAttribute Attr, object? Value)> snapshot;
        lock (_sync)
        {
            if (_executed != 0 || _items.Count == 0)
                return;
            _executed = 1;
            snapshot = _items.ToList();
            _items.Clear();
        }

        var distinct = snapshot
            .GroupBy(x => $"{x.Attr.CacheName}|{x.Attr.Mode}|{x.Value}")
            .Select(g => g.First());

        foreach (var item in distinct)
        {
            try
            {
                await _executor.ExecuteAsync(item.Attr, item.Value, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "缓存失效失败（已提交业务不受影响）。CacheName={CacheName}, Mode={Mode}",
                    item.Attr.CacheName,
                    item.Attr.Mode);

                try
                {
                    await _executor.ExecuteAsync(item.Attr, item.Value, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(
                        retryEx,
                        "缓存失效重试仍失败。CacheName={CacheName}, Mode={Mode}",
                        item.Attr.CacheName,
                        item.Attr.Mode);
                }
            }
        }
    }

    /// <summary>
    /// 丢弃未执行的登记（事务回滚或业务失败时调用）。
    /// </summary>
    public void Discard()
    {
        lock (_sync)
        {
            _items.Clear();
            _executed = 1;
        }
    }
}

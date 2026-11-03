using SimpX.Net.Caching;

namespace SimpX.Net.Data;

/// <summary>
/// 缓存回源时关闭 SqlSugar 读写分离，强制读主库；结束时恢复。
/// </summary>
public sealed class SqlSugarCacheReadThroughScope : ICacheReadThroughScope
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入 SqlSugar 客户端。
    /// </summary>
    /// <param name="db">SqlSugar</param>
    public SqlSugarCacheReadThroughScope(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public IDisposable Begin()
    {
        var provider = _db.AsTenant().GetConnectionScope(SqlSugarConst.MainConfigId);
        var previous = provider.Ado.IsDisableMasterSlaveSeparation;
        provider.Ado.IsDisableMasterSlaveSeparation = true;
        return new Restore(provider, previous);
    }

    private sealed class Restore : IDisposable
    {
        private readonly ISqlSugarClient _provider;
        private readonly bool _previous;
        private bool _disposed;

        public Restore(ISqlSugarClient provider, bool previous)
        {
            _provider = provider;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _provider.Ado.IsDisableMasterSlaveSeparation = _previous;
        }
    }
}

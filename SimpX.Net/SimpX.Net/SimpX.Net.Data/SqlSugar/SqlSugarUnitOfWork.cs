using SimpX.Net.Caching;

namespace SimpX.Net.Data;

/// <summary>
/// SqlSugar 事务和工作单元；提交成功后执行本请求的缓存失效清单。
/// </summary>
public sealed class SqlSugarUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// SqlSugar 对象
    /// </summary>
    private readonly ISqlSugarClient _sqlSugarClient;

    /// <summary>
    /// 当前请求的缓存待失效清单；没有注册时可为 null。
    /// </summary>
    private readonly CacheInvalidationBatch? _cacheInvalidations;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="sqlSugarClient">SqlSugar 客户端</param>
    /// <param name="cacheInvalidations">缓存待失效清单</param>
    public SqlSugarUnitOfWork(ISqlSugarClient sqlSugarClient, CacheInvalidationBatch? cacheInvalidations = null)
    {
        _sqlSugarClient = sqlSugarClient;
        _cacheInvalidations = cacheInvalidations;
    }

    /// <summary>
    /// 开启工作单元处理
    /// </summary>
    /// <param name="context">过滤器上下文</param>
    /// <param name="unitOfWork">工作单元特性</param>
    public async Task BeginTransactionAsync(FilterContext context, UnitOfWorkAttribute unitOfWork)
    {
        await _sqlSugarClient.AsTenant().BeginTranAsync();
    }

    /// <summary>
    /// 提交工作单元；成功后再跑缓存失效（失败只记日志，不推翻已提交事务）。
    /// </summary>
    /// <param name="resultContext">结果上下文</param>
    /// <param name="unitOfWork">工作单元特性</param>
    public async Task CommitTransactionAsync(FilterContext resultContext, UnitOfWorkAttribute unitOfWork)
    {
        await _sqlSugarClient.AsTenant().CommitTranAsync();
        if (_cacheInvalidations is not null)
            await _cacheInvalidations.ExecuteAsync();
    }

    /// <summary>
    /// 回滚工作单元，并丢弃待失效清单。
    /// </summary>
    /// <param name="resultContext">结果上下文</param>
    /// <param name="unitOfWork">工作单元特性</param>
    public async Task RollbackTransactionAsync(FilterContext resultContext, UnitOfWorkAttribute unitOfWork)
    {
        await _sqlSugarClient.AsTenant().RollbackTranAsync();
        _cacheInvalidations?.Discard();
    }

    /// <summary>
    /// 执行完毕（无论成功失败）
    /// </summary>
    /// <param name="context">上下文</param>
    /// <param name="resultContext">结果上下文</param>
    public Task OnCompletedAsync(FilterContext context, FilterContext resultContext)
    {
        _sqlSugarClient.Dispose();
        return Task.CompletedTask;
    }
}

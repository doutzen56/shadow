namespace SimpX.Net.Application.Admin;

/// <summary>
/// 把数据变更日志写进后台差异日志表：配置了独立日志库就写日志库，否则写主库。
/// </summary>
public sealed class SysLogDiffWriter : IDataChangeLogWriter, ISingleton
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入数据库（多库管理器）。
    /// </summary>
    /// <param name="db">数据库</param>
    public SysLogDiffWriter(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 写入一条差异日志。用新连接写，不占用业务事务。
    /// </summary>
    /// <param name="log">变更日志</param>
    /// <returns>异步任务</returns>
    public async Task WriteAsync(DataChangeLog log)
    {
        var tenant = (ITenant)_db;
        var logDb = tenant.IsAnyConnection(SqlSugarConst.LogConfigId)
            ? tenant.GetConnectionScope(SqlSugarConst.LogConfigId)
            : tenant.GetConnectionScope(SqlSugarConst.MainConfigId);

        await logDb.CopyNew().Insertable(new SysLogDiff
        {
            DiffData = log.DiffData,
            BusinessData = log.BusinessData,
            DiffType = log.DiffType,
            Sql = log.Sql,
            Parameters = log.Parameters,
            Elapsed = log.Elapsed
        }).ExecuteCommandAsync();
    }
}

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 同步代理游戏日报表。对齐旧 SyncAgentGameDateJob → TeamReportAgent.SaveAgentGameBatch。
/// </summary>
public interface IAgentGameDateSyncService
{
    /// <summary>
    /// 跑一轮：当日 + 跨天回补调 SP rpt_AgentStatistics_GameBatch。
    /// </summary>
    Task<AgentGameDateSyncResult> SyncAsync(CancellationToken cancellationToken = default);
}

/// <summary>代理日结同步结果。</summary>
public sealed class AgentGameDateSyncResult
{
    /// <summary>各次 SP 返回合计（无输出时按调用次数计）。</summary>
    public int Total { get; set; }

    /// <summary>说明。</summary>
    public string Message { get; set; }
}

/// <summary>
/// 默认实现：调存储过程；SQLite/未部署 SP 时吞异常，避免拖垮 Job。
/// 不挂 ITransient：与用户日结一并在 Schedule 显式注册。
/// </summary>
public sealed class AgentGameDateSyncService : IAgentGameDateSyncService
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<AgentGameDateSyncService> _logger;

    public AgentGameDateSyncService(IBizDbAccessor biz, ILogger<AgentGameDateSyncService> logger)
    {
        _db = biz.Db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AgentGameDateSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var beijingTz = DateTime.UtcNow.AddHours(8);
        var easternTz = DateTime.UtcNow.AddHours(-4);
        var utcNow = DateTime.UtcNow;

        var total = 0;
        total += await SaveBatchAsync(beijingTz.Date, cancellationToken);

        if (ReportGameDateTimeZones.InCrossDayWindow(beijingTz))
            total += await SaveBatchAsync(beijingTz.Date.AddDays(-1), cancellationToken);
        if (ReportGameDateTimeZones.InCrossDayWindow(easternTz))
            total += await SaveBatchAsync(easternTz.Date.AddDays(-1), cancellationToken);
        if (ReportGameDateTimeZones.InCrossDayWindow(utcNow))
            total += await SaveBatchAsync(utcNow.Date.AddDays(-1), cancellationToken);

        return new AgentGameDateSyncResult
        {
            Total = total,
            Message = $"代理游戏日结 GameBatch≈{total}"
        };
    }

    /// <summary>
    /// 调 rpt_AgentStatistics_GameBatch @Date；失败返回 0。
    /// </summary>
    private async Task<int> SaveBatchAsync(DateTime date, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var totalParam = new SugarParameter("@TotalCount", null, true)
            {
                Direction = System.Data.ParameterDirection.Output,
                DbType = System.Data.DbType.Int32
            };
            await _db.Ado.UseStoredProcedure().ExecuteCommandAsync(
                "rpt_AgentStatistics_GameBatch",
                new SugarParameter("@Date", date.Date),
                totalParam);

            if (totalParam.Value == null || totalParam.Value is DBNull)
                return 0;
            return Convert.ToInt32(totalParam.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("rpt_AgentStatistics_GameBatch 失败 Date={Date}: {Msg}", date.Date, ex.Message);
            return 0;
        }
    }
}

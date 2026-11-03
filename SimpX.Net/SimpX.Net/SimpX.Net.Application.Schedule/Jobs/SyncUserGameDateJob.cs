using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 同步用户游戏日报表：对齐旧 SyncUserGameDateJob。
/// GroupName=ReportStatistics；周期 300 秒。
/// </summary>
[JobDetail("job_sync_user_game_date", Description = "同步游戏注单报表", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(300, TriggerId = "trigger_sync_user_game_date", Description = "每 5 分钟同步一轮", RunOnStart = false)]
public class SyncUserGameDateJob : IJob
{
    private readonly IUserGameDateSyncService _sync;
    private readonly ILogger<SyncUserGameDateJob> _logger;

    public SyncUserGameDateJob(IUserGameDateSyncService sync, ILogger<SyncUserGameDateJob> logger)
    {
        _sync = sync;
        _logger = logger;
    }

    /// <summary>跑 BJ 主表同步；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var result = await _sync.SyncAsync(stoppingToken);
        var msg = result.Message ?? $"upsert≈{result.Upserted}";
        if (result.Upserted > 0 || result.NotConfigured)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

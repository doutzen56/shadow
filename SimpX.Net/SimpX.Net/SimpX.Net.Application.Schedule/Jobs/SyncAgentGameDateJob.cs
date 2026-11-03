using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 同步代理游戏日报表：对齐旧 SyncAgentGameDateJob。
/// GroupName=ReportStatistics；周期 420 秒。
/// </summary>
[JobDetail("job_sync_agent_game_date", Description = "同步代理游戏日报表", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(420, TriggerId = "trigger_sync_agent_game_date", Description = "每 7 分钟同步一轮", RunOnStart = false)]
public class SyncAgentGameDateJob : IJob
{
    private readonly IAgentGameDateSyncService _sync;
    private readonly ILogger<SyncAgentGameDateJob> _logger;

    public SyncAgentGameDateJob(IAgentGameDateSyncService sync, ILogger<SyncAgentGameDateJob> logger)
    {
        _sync = sync;
        _logger = logger;
    }

    /// <summary>调 GameBatch SP；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var result = await _sync.SyncAsync(stoppingToken);
        var msg = result.Message ?? $"GameBatch≈{result.Total}";
        if (result.Total > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

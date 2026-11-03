using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 同步游戏入口记录：对齐旧 GameEnteranceJob。
/// GroupName=AutoCheck；周期 600 秒。保留旧类名拼写 Enterance。
/// </summary>
[JobDetail("job_game_entrance", Description = "同步游戏入口记录", GroupName = "AutoCheck", Concurrent = false)]
[PeriodSeconds(600, TriggerId = "trigger_game_entrance", Description = "每 10 分钟消费入口队列", RunOnStart = false)]
public class GameEnteranceJob : IJob
{
    private readonly IGameEntranceSyncService _svc;
    private readonly ILogger<GameEnteranceJob> _logger;

    /// <summary>
    /// 注入入口同步服务。
    /// </summary>
    /// <param name="svc">服务</param>
    /// <param name="logger">日志</param>
    public GameEnteranceJob(IGameEntranceSyncService svc, ILogger<GameEnteranceJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>消费 GAMEENTRANCE；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var result = await _svc.DrainAndSyncAsync(100, stoppingToken);
        if (result.Synced > 0)
            _logger.LogInformation("{Msg}", result.Message);
        context.Result = result.Message;
    }
}

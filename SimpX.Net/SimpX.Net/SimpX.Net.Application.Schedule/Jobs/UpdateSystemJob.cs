using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 复位第三方接口维护状态：对齐旧 UpdateSystemJob → ReSetGameMaintainStatus。
/// GroupName=AutoCheck；周期 300 秒。
/// </summary>
[JobDetail("job_reset_game_maintain", Description = "更新第三方接口维护状态", GroupName = "AutoCheck", Concurrent = false)]
[PeriodSeconds(300, TriggerId = "trigger_reset_game_maintain", Description = "每 5 分钟复位一轮", RunOnStart = false)]
public class UpdateSystemJob : IJob
{
    private readonly IGameMaintainResetService _svc;
    private readonly ILogger<UpdateSystemJob> _logger;

    /// <summary>
    /// 注入维护复位服务。
    /// </summary>
    /// <param name="svc">服务</param>
    /// <param name="logger">日志</param>
    public UpdateSystemJob(IGameMaintainResetService svc, ILogger<UpdateSystemJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>扫 Status&gt;=2 并按 Redis/结束时间复位；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var reset = await _svc.ResetExpiredAsync(stoppingToken);
        var msg = $"复位游戏维护状态 {reset} 条";
        if (reset > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

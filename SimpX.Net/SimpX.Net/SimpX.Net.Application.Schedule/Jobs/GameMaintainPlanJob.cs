using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 执行维护计划：对齐旧 GameMaintainPlanJob。
/// GroupName=AutoCheck；周期 60 秒。
/// </summary>
[JobDetail("job_game_maintain_plan", Description = "执行维护计划", GroupName = "AutoCheck", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_game_maintain_plan", Description = "每分钟评估一轮", RunOnStart = false)]
public class GameMaintainPlanJob : IJob
{
    private readonly IGameMaintainPlanService _svc;
    private readonly ILogger<GameMaintainPlanJob> _logger;

    /// <summary>
    /// 注入维护计划服务。
    /// </summary>
    /// <param name="svc">服务</param>
    /// <param name="logger">日志</param>
    public GameMaintainPlanJob(IGameMaintainPlanService svc, ILogger<GameMaintainPlanJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>评估 Temporary/Weekly/Monthly；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var result = await _svc.RunAsync(stoppingToken);
        if (result.Entered + result.Resumed > 0)
            _logger.LogInformation("{Msg}", result.Message);
        context.Result = result.Message;
    }
}

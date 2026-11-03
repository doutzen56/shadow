using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 检查平台游戏日志采集延时：对齐旧 CheckGemeLogDelayJob。
/// GroupName=AutoCheck；周期 300 秒。保留旧类名拼写。
/// </summary>
[JobDetail("job_check_game_log_delay", Description = "获取平台数据延时", GroupName = "AutoCheck", Concurrent = false)]
[PeriodSeconds(300, TriggerId = "trigger_check_game_log_delay", Description = "每 5 分钟检查一轮", RunOnStart = false)]
public class CheckGemeLogDelayJob : IJob
{
    private readonly IGameLogDelayCheckService _svc;
    private readonly ILogger<CheckGemeLogDelayJob> _logger;

    /// <summary>
    /// 注入延时检查服务。
    /// </summary>
    /// <param name="svc">服务</param>
    /// <param name="logger">日志</param>
    public CheckGemeLogDelayJob(IGameLogDelayCheckService svc, ILogger<CheckGemeLogDelayJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>扫 DelayTime&gt;0 的标记；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var delayed = await _svc.CheckDelayAsync(stoppingToken);
        var msg = delayed < 0
            ? "检查游戏日志采集延时失败"
            : $"游戏日志采集延时告警 {delayed} 条";
        if (delayed > 0)
            _logger.LogWarning("{Msg}", msg);
        context.Result = msg;
    }
}

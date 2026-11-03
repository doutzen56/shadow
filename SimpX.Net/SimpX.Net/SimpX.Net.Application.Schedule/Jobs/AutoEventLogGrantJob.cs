using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 自动审活动日志并发放奖金。对齐旧 AutoEventLogJob 中的 CheckEventLog() 资金发放段。
/// 全民代理 / 邀请 / 洗码等统计任务仍后置。
/// </summary>
[JobDetail("job_auto_event_log_grant", Description = "活动日志自动审发放", GroupName = "RebateEvent", Concurrent = false)]
[PeriodSeconds(15, TriggerId = "trigger_auto_event_log_grant", Description = "每 15 秒扫一次", RunOnStart = false)]
public class AutoEventLogGrantJob : IJob
{
    private readonly IEventLogCheckService _check;
    private readonly ILogger<AutoEventLogGrantJob> _logger;

    /// <summary>
    /// 注入审核服务与日志。
    /// </summary>
    /// <param name="check">活动审核</param>
    /// <param name="logger">日志</param>
    public AutoEventLogGrantJob(IEventLogCheckService check, ILogger<AutoEventLogGrantJob> logger)
    {
        _check = check;
        _logger = logger;
    }

    /// <summary>
    /// 自动审到期 Put/IsAuto=自动 的待处理日志。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var count = await _check.AutoCheckDueAsync(100, stoppingToken);
        var msg = $"活动自动审发放 {count} 笔";
        if (count > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

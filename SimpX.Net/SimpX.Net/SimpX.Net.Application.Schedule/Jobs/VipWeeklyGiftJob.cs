using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// VIP 每周礼金。对齐旧 UpdateUserVIPJob → ExeEveryWeek。
/// </summary>
[JobDetail("job_vip_weekly_gift", Description = "VIP 每周礼金", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_vip_weekly_gift", Description = "每分钟检查发放窗口", RunOnStart = false)]
public class VipWeeklyGiftJob : IJob
{
    private readonly IVipPeriodicGiftService _gifts;
    private readonly ILogger<VipWeeklyGiftJob> _logger;

    /// <summary>
    /// 注入礼金服务与日志。
    /// </summary>
    /// <param name="gifts">周期礼金</param>
    /// <param name="logger">日志</param>
    public VipWeeklyGiftJob(IVipPeriodicGiftService gifts, ILogger<VipWeeklyGiftJob> logger)
    {
        _gifts = gifts;
        _logger = logger;
    }

    /// <summary>
    /// 尝试发放本周礼金。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var count = await _gifts.GrantEveryWeekAsync(stoppingToken);
        var msg = $"VIP 每周礼金发放 {count} 人";
        if (count > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// VIP 生日礼金。对齐旧 AddPointJob → ExeBirthday。
/// </summary>
[JobDetail("job_vip_birthday_gift", Description = "VIP 生日礼金", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodMinutes(20, TriggerId = "trigger_vip_birthday_gift", Description = "每 20 分钟扫一次当天生日", RunOnStart = false)]
public class VipBirthdayGiftJob : IJob
{
    private readonly IVipPeriodicGiftService _gifts;
    private readonly ILogger<VipBirthdayGiftJob> _logger;

    /// <summary>
    /// 注入礼金服务与日志。
    /// </summary>
    /// <param name="gifts">周期礼金</param>
    /// <param name="logger">日志</param>
    public VipBirthdayGiftJob(IVipPeriodicGiftService gifts, ILogger<VipBirthdayGiftJob> logger)
    {
        _gifts = gifts;
        _logger = logger;
    }

    /// <summary>
    /// 尝试发放当天生日礼金。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var count = await _gifts.GrantBirthdayAsync(stoppingToken);
        var msg = $"VIP 生日礼金发放 {count} 人";
        if (count > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

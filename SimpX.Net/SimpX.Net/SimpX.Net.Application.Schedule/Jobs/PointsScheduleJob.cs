using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 生日积分 + 投注积分。对齐旧 AddPointJob 积分段。
/// </summary>
[JobDetail("job_points_schedule", Description = "生日/投注定时积分", GroupName = "RebateEvent", Concurrent = false)]
[PeriodMinutes(20, TriggerId = "trigger_points_schedule", Description = "每 20 分钟跑一次", RunOnStart = false)]
public class PointsScheduleJob : IJob
{
    private readonly IScheduledPointService _points;
    private readonly ILogger<PointsScheduleJob> _logger;

    /// <summary>
    /// 注入积分服务与日志。
    /// </summary>
    /// <param name="points">定时积分</param>
    /// <param name="logger">日志</param>
    public PointsScheduleJob(IScheduledPointService points, ILogger<PointsScheduleJob> logger)
    {
        _points = points;
        _logger = logger;
    }

    /// <summary>
    /// 先发生日积分，再发昨日投注积分。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var birthday = await _points.GrantBirthdayPointsAsync(stoppingToken);
        var betting = await _points.GrantBettingPointsAsync(stoppingToken);
        var msg = $"定时积分 生日={birthday} 投注={betting}";
        if (birthday + betting > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

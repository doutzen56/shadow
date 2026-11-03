using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// AutoEvent 统计段：全民代理 / 邀请有礼 / 洗码 / 邀请任务写待审日志；转盘次数发放与日清。
/// </summary>
[JobDetail("job_auto_event_stats", Description = "活动统计写 EventLog（全民代理/邀请有礼/洗码/邀请任务/转盘）", GroupName = "RebateEvent", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_auto_event_stats", Description = "每分钟扫一次", RunOnStart = false)]
public class AutoEventStatsJob : IJob
{
    private readonly IAutoEventStatsService _stats;
    private readonly ILogger<AutoEventStatsJob> _logger;

    /// <summary>
    /// 注入统计服务与日志。
    /// </summary>
    /// <param name="stats">活动统计</param>
    /// <param name="logger">日志</param>
    public AutoEventStatsJob(IAutoEventStatsService stats, ILogger<AutoEventStatsJob> logger)
    {
        _stats = stats;
        _logger = logger;
    }

    /// <summary>
    /// 跑本波已落地的 AutoEvent 统计段。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var invite = await _stats.RunUserInviteAsync(stoppingToken);
        var reward = await _stats.RunInviteRewardDailyAsync(stoppingToken);
        var washing = await _stats.RunWashingAsync(stoppingToken);
        var inviteTask = await _stats.RunInviteTaskDailyAsync(stoppingToken);
        var competition = await _stats.RunCompetitionAsync(stoppingToken);
        var delottery = await _stats.RunDelotteryBetGrantAsync(stoppingToken);
        var clean = await _stats.RunDelotteryCleanAsync(stoppingToken);
        var msg =
            $"活动统计 全民代理={invite} 邀请有礼={reward} 洗码={washing} 邀请任务={inviteTask} 擂台赛={competition} 转盘发={delottery} 转盘清={clean}";
        if (invite + reward + washing + inviteTask + competition + delottery + clean > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

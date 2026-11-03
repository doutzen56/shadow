using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 消费 QUEUE_USERVIP：对齐旧 UpdateUserVIPJob 的升级段（ExeUpgrade）。周/月礼金另开任务。
/// </summary>
[JobDetail("job_user_vip_drain", Description = "VIP 队列排水升级（QUEUE_USERVIP）", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_user_vip_drain", Description = "每分钟排水一次", RunOnStart = false)]
public class UserVipDrainJob : IJob
{
    private readonly ILedgerSideEffectQueues _queues;
    private readonly IVipUpgradeService _upgrade;
    private readonly ILogger<UserVipDrainJob> _logger;

    /// <summary>
    /// 注入队列、升级服务与日志。
    /// </summary>
    /// <param name="queues">旁路队列</param>
    /// <param name="upgrade">VIP 升级</param>
    /// <param name="logger">日志</param>
    public UserVipDrainJob(
        ILedgerSideEffectQueues queues,
        IVipUpgradeService upgrade,
        ILogger<UserVipDrainJob> logger)
    {
        _queues = queues;
        _upgrade = upgrade;
        _logger = logger;
    }

    /// <summary>
    /// 取出到期 VIP 用户并尝试升级；单轮最多 100 人。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var users = _queues.TakeDueUserVip(100);
        var upgraded = 0;
        foreach (var userId in users)
        {
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                if (await _upgrade.TryUpgradeAsync(userId, stoppingToken))
                    upgraded++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "QUEUE_USERVIP 升级失败 userId={UserId}", userId);
            }
        }

        var msg = $"QUEUE_USERVIP 取出 {users.Count} 人，升级成功 {upgraded}";
        if (users.Count > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

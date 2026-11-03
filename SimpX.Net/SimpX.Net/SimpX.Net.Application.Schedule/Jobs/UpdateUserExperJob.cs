using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 用户经验排水：对齐旧 UpdateUserExperJob（QUEUE_USEREXPER → Users.Exper ← usr_Money.Exper）。
/// GroupName=ReportStatistics；周期 60 秒。
/// </summary>
[JobDetail("job_user_exper_drain", Description = "用户经验队列消费（QUEUE_USEREXPER）", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_user_exper_drain", Description = "每分钟消费一次", RunOnStart = false)]
public class UpdateUserExperJob : IJob
{
    private readonly ILedgerSideEffectQueues _queues;
    private readonly IUserExperSyncService _sync;
    private readonly ILogger<UpdateUserExperJob> _logger;

    /// <summary>
    /// 注入队列、同步服务与日志。
    /// </summary>
    /// <param name="queues">旁路队列</param>
    /// <param name="sync">经验同步</param>
    /// <param name="logger">日志</param>
    public UpdateUserExperJob(
        ILedgerSideEffectQueues queues,
        IUserExperSyncService sync,
        ILogger<UpdateUserExperJob> logger)
    {
        _queues = queues;
        _sync = sync;
        _logger = logger;
    }

    /// <summary>
    /// 取出经验队列并去重后写回 Users.Exper；单轮最多 500 条。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var raw = _queues.TakeDueUserExper(500);
        var users = raw.Where(id => id != 0).Distinct().ToList();
        var ok = 0;
        foreach (var userId in users)
        {
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                if (await _sync.SyncAsync(userId, stoppingToken))
                    ok++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "QUEUE_USEREXPER 同步失败 userId={UserId}", userId);
            }
        }

        var msg = $"QUEUE_USEREXPER 取出 {raw.Count}，去重 {users.Count}，同步成功 {ok}";
        if (raw.Count > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

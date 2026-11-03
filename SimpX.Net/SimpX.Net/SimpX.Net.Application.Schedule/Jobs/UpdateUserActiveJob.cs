using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 在线会员活动时间：对齐旧 UpdateUserActiveJob（USER_ACTIVE → ActiveAt；每 3 轮写 site_Online）。
/// GroupName=ReportStatistics；周期 180 秒。
/// </summary>
[JobDetail("job_user_active_sync", Description = "在线会员活动时间同步", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(180, TriggerId = "trigger_user_active_sync", Description = "每 3 分钟同步一次", RunOnStart = false)]
public class UpdateUserActiveJob : IJob
{
    private static int _timerCount;
    private static readonly List<int> OnlineUsers = [];
    private static readonly object OnlineLock = new();

    private readonly ILedgerSideEffectQueues _queues;
    private readonly IUserActiveSyncService _sync;
    private readonly ILogger<UpdateUserActiveJob> _logger;

    /// <summary>
    /// 注入队列、同步服务与日志。
    /// </summary>
    /// <param name="queues">旁路队列</param>
    /// <param name="sync">活动时间 / 在线快照</param>
    /// <param name="logger">日志</param>
    public UpdateUserActiveJob(
        ILedgerSideEffectQueues queues,
        IUserActiveSyncService sync,
        ILogger<UpdateUserActiveJob> logger)
    {
        _queues = queues;
        _sync = sync;
        _logger = logger;
    }

    /// <summary>
    /// 取出 USER_ACTIVE，写 ActiveAt；累计在线 id，每第 3 轮写入 site_Online。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var users = _queues.TakeDueUserActive();
        List<int> snapshotIds = null;
        var tick = 0;

        lock (OnlineLock)
        {
            if (users.Count > 0)
                OnlineUsers.AddRange(users);

            tick = _timerCount;
            if (tick != 0 && tick % 3 == 0)
            {
                snapshotIds = OnlineUsers.Distinct().ToList();
                OnlineUsers.Clear();
            }

            _timerCount++;
        }

        var updated = await _sync.UpdateActiveAtAsync(users, stoppingToken);
        var onlineSaved = 0;
        if (snapshotIds != null)
        {
            onlineSaved = await _sync.SaveOnlineSnapshotAsync(snapshotIds, stoppingToken);
            if (onlineSaved > 0)
                _logger.LogInformation("在线会员统计 {Count} 人", onlineSaved);
        }

        var msg = $"USER_ACTIVE 取出 {users.Count}，ActiveAt 更新 {updated}，在线快照 {onlineSaved}（tick={tick}）";
        if (users.Count > 0 || onlineSaved > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

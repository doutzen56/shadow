using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 消费 QUEUE_USERMONEY：对齐旧 UpdateUserMoneyJob（data_UpdateUserMoney + 回写 Users 余额镜像）。
/// GroupName=ReportStatistics。
/// </summary>
[JobDetail("job_user_money_drain", Description = "用户余额队列消费（QUEUE_USERMONEY）", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_user_money_drain", Description = "每分钟消费一次", RunOnStart = false)]
public class UserMoneyDrainJob : IJob
{
    private readonly ILedgerSideEffectQueues _queues;
    private readonly IUserMoneySyncService _sync;
    private readonly IBizDbAccessor _biz;
    private readonly ILogger<UserMoneyDrainJob> _logger;

    public UserMoneyDrainJob(
        ILedgerSideEffectQueues queues,
        IUserMoneySyncService sync,
        IBizDbAccessor biz,
        ILogger<UserMoneyDrainJob> logger)
    {
        _queues = queues;
        _sync = sync;
        _biz = biz;
        _logger = logger;
    }

    /// <summary>取出到期 userId_currency，解析 SiteID 后调 SP；单轮最多 100 条。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var due = _queues.TakeDueUserMoney(100);
        var ok = 0;
        foreach (var (userId, currency) in due)
        {
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                var rows = await _biz.Db.Queryable<User>()
                    .Where(u => u.ID == userId)
                    .Select(u => u.SiteID)
                    .Take(1)
                    .ToListAsync(stoppingToken);
                var siteId = rows.FirstOrDefault();
                if (siteId <= 0)
                {
                    _logger.LogWarning("QUEUE_USERMONEY 跳过：无站点 userId={UserId}", userId);
                    continue;
                }

                if (await _sync.SyncAsync(userId, siteId, currency, stoppingToken))
                    ok++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "QUEUE_USERMONEY 同步失败 userId={UserId} currency={Currency}", userId, currency);
            }
        }

        var msg = $"QUEUE_USERMONEY 取出 {due.Count}，同步成功 {ok}";
        if (due.Count > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

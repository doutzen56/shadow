using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 消费 MONEY_LOG：对齐旧 TeamStatisticsJob → SaveTeamMoneyLog（rpt_AgentStatistics_Money）。
/// </summary>
[JobDetail("job_money_log_drain", Description = "账变报表队列消费（MONEY_LOG）", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_money_log_drain", Description = "每分钟消费一次", RunOnStart = false)]
public class MoneyLogDrainJob : IJob
{
    private readonly ILedgerSideEffectQueues _queues;
    private readonly ITeamMoneyLogConsumer _consumer;
    private readonly ILogger<MoneyLogDrainJob> _logger;

    /// <summary>
    /// 注入队列、消费端与日志。
    /// </summary>
    /// <param name="queues">旁路队列</param>
    /// <param name="consumer">团队账变统计</param>
    /// <param name="logger">日志</param>
    public MoneyLogDrainJob(
        ILedgerSideEffectQueues queues,
        ITeamMoneyLogConsumer consumer,
        ILogger<MoneyLogDrainJob> logger)
    {
        _queues = queues;
        _consumer = consumer;
        _logger = logger;
    }

    /// <summary>
    /// 弹出账变流水号并调存储过程；单轮最多 200 条。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var count = 0;
        while (count < 200 && _queues.TryDequeueMoneyLog(out var logId))
        {
            stoppingToken.ThrowIfCancellationRequested();
            count++;
            await _consumer.ConsumeAsync(logId, stoppingToken);
        }

        var msg = $"MONEY_LOG 消费 {count} 条";
        if (count > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

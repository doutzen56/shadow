using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 消费 PROXY_MONEY_STS：对齐旧 ProxyStatisticsJob → SaveProxyMoneySts。
/// </summary>
[JobDetail("job_proxy_money_sts_drain", Description = "代理上下分统计队列消费", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_proxy_money_sts_drain", Description = "每分钟消费一次", RunOnStart = false)]
public class ProxyMoneyStsDrainJob : IJob
{
    private readonly ILedgerSideEffectQueues _queues;
    private readonly IProxyMoneyStsConsumer _consumer;
    private readonly ILogger<ProxyMoneyStsDrainJob> _logger;

    /// <summary>
    /// 注入队列、消费端与日志。
    /// </summary>
    /// <param name="queues">旁路队列</param>
    /// <param name="consumer">代理上下分统计</param>
    /// <param name="logger">日志</param>
    public ProxyMoneyStsDrainJob(
        ILedgerSideEffectQueues queues,
        IProxyMoneyStsConsumer consumer,
        ILogger<ProxyMoneyStsDrainJob> logger)
    {
        _queues = queues;
        _consumer = consumer;
        _logger = logger;
    }

    /// <summary>
    /// 弹出统计 JSON 并调存储过程；单轮最多 200 条。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var count = 0;
        while (count < 200 && _queues.TryDequeueProxyMoneySts(out var payload))
        {
            stoppingToken.ThrowIfCancellationRequested();
            count++;
            await _consumer.ConsumeAsync(payload, stoppingToken);
        }

        var msg = $"PROXY_MONEY_STS 消费 {count} 条";
        if (count > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

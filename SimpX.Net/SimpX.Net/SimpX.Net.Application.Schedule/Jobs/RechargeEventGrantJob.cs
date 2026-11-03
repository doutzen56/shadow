using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 消费 RECHARGE_LOG：按订单补写充值活动参与日志。对齐旧 RechargeEventJob → ResetOrderAddEvent。
/// </summary>
[JobDetail("job_recharge_event_grant", Description = "充值活动日志（RECHARGE_LOG）", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(120, TriggerId = "trigger_recharge_event_grant", Description = "每 2 分钟消费一次", RunOnStart = false)]
public class RechargeEventGrantJob : IJob
{
    private readonly ILedgerSideEffectQueues _queues;
    private readonly IRechargePromoGrantService _promo;
    private readonly ILogger<RechargeEventGrantJob> _logger;

    /// <summary>
    /// 注入队列、发奖与日志。
    /// </summary>
    /// <param name="queues">旁路队列</param>
    /// <param name="promo">充值活动</param>
    /// <param name="logger">日志</param>
    public RechargeEventGrantJob(
        ILedgerSideEffectQueues queues,
        IRechargePromoGrantService promo,
        ILogger<RechargeEventGrantJob> logger)
    {
        _queues = queues;
        _promo = promo;
        _logger = logger;
    }

    /// <summary>
    /// 弹出订单号并尝试写活动日志；单轮最多 50 条。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var processed = 0;
        var granted = 0;
        while (processed < 50 && _queues.TryDequeueRechargeLog(out var orderId))
        {
            processed++;
            try
            {
                granted += await _promo.GrantForOrderAsync(orderId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "充值活动消费失败 Order={OrderId}", orderId);
            }
        }

        var msg = $"RECHARGE_LOG 处理 {processed} 笔，新写活动日志 {granted} 条";
        _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

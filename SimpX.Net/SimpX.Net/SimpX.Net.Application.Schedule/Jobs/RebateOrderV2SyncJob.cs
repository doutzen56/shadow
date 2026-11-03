using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 自助反水 V2 同步计算：对齐旧 RebateOrderV2SyncJob（每 5 分钟，不入账）。
/// GroupName=Rebate。
/// </summary>
[JobDetail("job_rebate_order_v2_sync", Description = "自助反水同步计算", GroupName = "Rebate", Concurrent = false)]
[PeriodMinutes(5, TriggerId = "trigger_rebate_order_v2_sync", Description = "每 5 分钟同步一轮", RunOnStart = false)]
public class RebateOrderV2SyncJob : IJob
{
    private readonly IRebateOrderV2SyncService _svc;
    private readonly ILogger<RebateOrderV2SyncJob> _logger;

    /// <summary>
    /// 注入同步服务与日志。
    /// </summary>
    /// <param name="svc">同步服务</param>
    /// <param name="logger">日志</param>
    public RebateOrderV2SyncJob(IRebateOrderV2SyncService svc, ILogger<RebateOrderV2SyncJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>
    /// 门控后探测 ES / 水位；不入账。结果写入 context.Result。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var msg = await _svc.SyncAsync(stoppingToken);
        _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

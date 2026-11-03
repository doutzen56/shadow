using Furion.TimeCrontab;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// ES 索引月度维护：对齐旧 EsMonthlyRolloverJob（每月 1/2/28 号 00:05）。
/// GroupName=CleanUp。
/// </summary>
[JobDetail("job_es_monthly_rollover", Description = "ES 索引月度维护", GroupName = "CleanUp", Concurrent = false)]
[Cron("0 5 0 1,2,28 * *", CronStringFormat.WithSeconds, TriggerId = "trigger_es_monthly_rollover", Description = "每月 1/2/28 日 00:05", RunOnStart = false)]
public class EsMonthlyRolloverJob : IJob
{
    private readonly IEsIndexMaintenanceService _svc;
    private readonly ILogger<EsMonthlyRolloverJob> _logger;

    /// <summary>
    /// 注入月度维护服务与日志。
    /// </summary>
    /// <param name="svc">ES 月度维护</param>
    /// <param name="logger">日志</param>
    public EsMonthlyRolloverJob(IEsIndexMaintenanceService svc, ILogger<EsMonthlyRolloverJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>
    /// 检查 gamelogv2 / rebateorderv2 当月索引；结果写入 context.Result。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var msg = await _svc.RunAsync(cancellationToken: stoppingToken);
        _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

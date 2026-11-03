using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 生成代理报表 / 沉寂改组：对齐旧 DailyReportJob。
/// GroupName=ReportStatistics；周期 60 秒（仅 00:05 / 02:05 有动作）。
/// </summary>
[JobDetail("job_daily_report", Description = "生成代理报表", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_daily_report", Description = "每分钟检查一次", RunOnStart = false)]
public class DailyReportJob : IJob
{
    private readonly IDailyReportService _svc;
    private readonly ILogger<DailyReportJob> _logger;

    public DailyReportJob(IDailyReportService svc, ILogger<DailyReportJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>跑一轮 tick；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var result = await _svc.RunTickAsync(cancellationToken: stoppingToken);
        var msg = result.Message ?? "ok";
        if (result.AgentUpdates > 0 || result.DormantMoved > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

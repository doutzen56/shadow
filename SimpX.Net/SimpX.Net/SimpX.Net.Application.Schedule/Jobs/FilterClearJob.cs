using Furion.TimeCrontab;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 布隆过滤器维护：对齐旧 FilterClearJob（每天 04:11）。
/// GroupName=CleanUp。
/// </summary>
[JobDetail("job_bloom_filter_refresh", Description = "布隆过滤器自动维护", GroupName = "CleanUp", Concurrent = false)]
[Cron("0 11 4 * * *", CronStringFormat.WithSeconds, TriggerId = "trigger_bloom_filter_refresh", Description = "每天 04:11", RunOnStart = false)]
public class FilterClearJob : IJob
{
    private readonly IBloomFilterMaintenanceService _svc;
    private readonly ILogger<FilterClearJob> _logger;

    /// <summary>
    /// 注入布隆维护服务与日志。
    /// </summary>
    /// <param name="svc">布隆维护</param>
    /// <param name="logger">日志</param>
    public FilterClearJob(IBloomFilterMaintenanceService svc, ILogger<FilterClearJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>
    /// 刷新过滤器统计（无实现时空跑）；结果写入 context.Result。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var msg = await _svc.RefreshAsync(stoppingToken);
        _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 报表下载文件清理：对齐旧 DataMaintenanceJob（删两天前 site_FileDownload）。
/// GroupName=CleanUp；周期 86400 秒。
/// </summary>
[JobDetail("job_site_file_cleanup", Description = "清理两天前报表下载文件记录", GroupName = "CleanUp", Concurrent = false)]
[PeriodSeconds(86400, TriggerId = "trigger_site_file_cleanup", Description = "每天清理一次", RunOnStart = false)]
public class DataMaintenanceJob : IJob
{
    private readonly ISiteFileCleanupService _cleanup;
    private readonly ILogger<DataMaintenanceJob> _logger;

    /// <summary>
    /// 注入清理服务与日志。
    /// </summary>
    /// <param name="cleanup">文件记录清理</param>
    /// <param name="logger">日志</param>
    public DataMaintenanceJob(ISiteFileCleanupService cleanup, ILogger<DataMaintenanceJob> logger)
    {
        _cleanup = cleanup;
        _logger = logger;
    }

    /// <summary>
    /// 删除 CreateAt 早于两天前的 site_FileDownload。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var cutoff = DateTime.Now.AddDays(-2);
        var deleted = await _cleanup.DeleteOlderThanAsync(cutoff, stoppingToken);
        var msg = $"site_FileDownload 删除 CreateAt 早于 {cutoff:yyyy-MM-dd} 共 {deleted} 条";
        if (deleted > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

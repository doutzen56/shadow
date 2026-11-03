using SimpX.Net.Contracts.Jobs;
namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 每天清理一次系统日志。作业编号沿用 job_log，已在后台调整过的触发器不受影响。
/// </summary>
[JobDetail("job_log", Description = "清理操作日志", GroupName = "CleanUp", Concurrent = false)]
[Daily(TriggerId = "trigger_log", Description = "清理操作日志")]
public class SystemLogCleanupJob : IJob
{
    private readonly IAdminMaintenanceApi _adminMaintenanceApi;
    private readonly ILogger<SystemLogCleanupJob> _logger;

    /// <summary>
    /// 注入 Admin 维护接口客户端和日志。
    /// </summary>
    /// <param name="adminMaintenanceApi">Admin 维护接口</param>
    /// <param name="logger">日志</param>
    public SystemLogCleanupJob(IAdminMaintenanceApi adminMaintenanceApi, ILogger<SystemLogCleanupJob> logger)
    {
        _adminMaintenanceApi = adminMaintenanceApi;
        _logger = logger;
    }

    /// <summary>
    /// 调 Admin 清理过期日志，结果写到本次执行记录。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var days = await _adminMaintenanceApi.CleanExpiredLogs();
        var msg = $"清理系统日志成功，删除 {days} 天前的日志数据";
        _logger.LogInformation(msg);
        context.Result = msg;
    }
}

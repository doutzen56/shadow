using Furion.TimeCrontab;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 更新用户代理层级：对齐旧 UpdateUserAgentJob（每 10 分钟）。
/// GroupName=AutoCheck；Cron 带秒：每 10 分钟。
/// </summary>
[JobDetail("job_update_user_agent", Description = "更新用户代理层级", GroupName = "AutoCheck", Concurrent = false)]
[Cron("0 */10 * * * *", CronStringFormat.WithSeconds, TriggerId = "trigger_update_user_agent", Description = "每 10 分钟跑一轮", RunOnStart = false)]
public class UpdateUserAgentJob : IJob
{
    private readonly IUserAgentUpdateService _svc;
    private readonly ILogger<UpdateUserAgentJob> _logger;

    /// <summary>
    /// 注入代理层级更新服务。
    /// </summary>
    /// <param name="svc">服务</param>
    /// <param name="logger">日志</param>
    public UpdateUserAgentJob(IUserAgentUpdateService svc, ILogger<UpdateUserAgentJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>处理本月未完成的代理变更日志；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var done = await _svc.ProcessPendingAsync(stoppingToken);
        var msg = $"更新用户代理层级完成 {done} 条";
        if (done > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

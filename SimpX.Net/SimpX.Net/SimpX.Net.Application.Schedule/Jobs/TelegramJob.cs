using Furion.TimeCrontab;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 纸飞机监控：对齐旧 TelegramJob（每小时）。
/// GroupName=AutoCheck；Cron 带秒：整点。
/// </summary>
[JobDetail("job_telegram", Description = "纸飞机监控服务", GroupName = "AutoCheck", Concurrent = false)]
[Cron("0 0 * * * *", CronStringFormat.WithSeconds, TriggerId = "trigger_telegram", Description = "每小时跑一轮", RunOnStart = false)]
public class TelegramJob : IJob
{
    private readonly ITelegramMonitorService _svc;
    private readonly ILogger<TelegramJob> _logger;

    /// <summary>
    /// 注入纸飞机监控服务。
    /// </summary>
    /// <param name="svc">服务</param>
    /// <param name="logger">日志</param>
    public TelegramJob(ITelegramMonitorService svc, ILogger<TelegramJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>扫 Telegram 配置站；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var result = await _svc.RunAsync(stoppingToken);
        _logger.LogInformation("{Msg}", result.Message);
        context.Result = result.Message;
    }
}

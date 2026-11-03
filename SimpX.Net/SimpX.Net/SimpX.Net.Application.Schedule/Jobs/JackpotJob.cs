using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 自动爆奖：对齐旧 JackpotJob（约 62 分钟）。
/// GroupName=AutoCheck；周期 3720 秒。
/// </summary>
[JobDetail("job_jackpot", Description = "执行爆奖", GroupName = "AutoCheck", Concurrent = false)]
[PeriodSeconds(3720, TriggerId = "trigger_jackpot", Description = "每 62 分钟跑一轮", RunOnStart = false)]
public class JackpotJob : IJob
{
    private readonly IJackpotAutoService _svc;
    private readonly ILogger<JackpotJob> _logger;

    /// <summary>
    /// 注入自动爆奖服务。
    /// </summary>
    /// <param name="svc">服务</param>
    /// <param name="logger">日志</param>
    public JackpotJob(IJackpotAutoService svc, ILogger<JackpotJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>扫 Jackpot 配置站；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var result = await _svc.RunAsync(stoppingToken);
        _logger.LogInformation("{Msg}", result.Message);
        context.Result = result.Message;
    }
}

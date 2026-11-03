using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 自助反水 V2 到点切换：对齐旧 RebateV2CutoverJob（每 60 秒）。
/// GroupName=Rebate。
/// </summary>
[JobDetail("job_rebate_v2_cutover", Description = "自助反水到点切换", GroupName = "Rebate", Concurrent = false)]
[PeriodSeconds(60, TriggerId = "trigger_rebate_v2_cutover", Description = "每 60 秒扫一轮", RunOnStart = false)]
public class RebateV2CutoverJob : IJob
{
    private readonly IRebateV2CutoverService _svc;
    private readonly ILogger<RebateV2CutoverJob> _logger;

    /// <summary>
    /// 注入切换服务与日志。
    /// </summary>
    /// <param name="svc">V2 切换</param>
    /// <param name="logger">日志</param>
    public RebateV2CutoverJob(IRebateV2CutoverService svc, ILogger<RebateV2CutoverJob> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>
    /// 扫描到点站点并幂等翻旗；结果写入 context.Result。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var activated = await _svc.ActivateDueSitesAsync(stoppingToken);
        var msg = $"自助反水 V2 到点切换完成，本轮激活 {activated} 站";
        if (activated > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 付款中提现核对：对齐旧 CheckWithdrawJob → WithdrawCheckforService。
/// GroupName=AutoCheck；周期 300 秒。
/// </summary>
[JobDetail("job_check_withdraw", Description = "查询核对提现订单", GroupName = "AutoCheck", Concurrent = false)]
[PeriodSeconds(300, TriggerId = "trigger_check_withdraw", Description = "每 5 分钟核对一轮", RunOnStart = false)]
public class CheckWithdrawJob : IJob
{
    private readonly IWithdrawCheckService _check;
    private readonly ILogger<CheckWithdrawJob> _logger;

    public CheckWithdrawJob(IWithdrawCheckService check, ILogger<CheckWithdrawJob> logger)
    {
        _check = check;
        _logger = logger;
    }

    /// <summary>扫付款中订单并查单；结果写入 context.Result。</summary>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var scanned = await _check.CheckInPaymentAsync(200, stoppingToken);
        var msg = $"提现核对扫描付款中 {scanned} 笔";
        if (scanned > 0)
            _logger.LogInformation("{Msg}", msg);
        context.Result = msg;
    }
}

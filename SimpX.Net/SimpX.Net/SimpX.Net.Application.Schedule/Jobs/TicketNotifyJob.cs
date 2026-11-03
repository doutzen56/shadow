namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 工单提醒：对齐旧 TicketNotifyJob 占位（旧 Notify 已注释，空跑）。
/// GroupName=AutoCheck；周期 3600 秒。
/// </summary>
[JobDetail("job_ticket_notify", Description = "工单提醒", GroupName = "AutoCheck", Concurrent = false)]
[PeriodSeconds(3600, TriggerId = "trigger_ticket_notify", Description = "每小时空跑占位", RunOnStart = false)]
public class TicketNotifyJob : IJob
{
    /// <summary>空跑；旧 TicketAgent.Notify 未移植。</summary>
    public Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        // 旧：TicketAgent.Instance().Notify();
        context.Result = "工单提醒任务空跑（旧逻辑未移植）";
        return Task.CompletedTask;
    }
}

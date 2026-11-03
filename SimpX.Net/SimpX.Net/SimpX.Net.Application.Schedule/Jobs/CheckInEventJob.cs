namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 自动处理签到活动：对齐旧 CheckInEventJob 占位（旧逻辑暂未移植，空跑）。
/// GroupName=AutoCheck；周期 3600 秒。
/// </summary>
[JobDetail("job_checkin_event", Description = "自动处理活动", GroupName = "AutoCheck", Concurrent = false)]
[PeriodSeconds(3600, TriggerId = "trigger_checkin_event", Description = "每小时空跑占位", RunOnStart = false)]
public class CheckInEventJob : IJob
{
    /// <summary>空跑；旧 ExecSignIn 未移植。</summary>
    public Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        // 旧：EventAgent.Instance().ExecSignIn();
        context.Result = "签到活动任务空跑（旧逻辑未移植）";
        return Task.CompletedTask;
    }
}

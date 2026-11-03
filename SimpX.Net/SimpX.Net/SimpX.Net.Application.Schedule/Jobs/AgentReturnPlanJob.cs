namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 代理返水计划：对齐旧 AgentReturnPlanJob（旧 ExecuteAgentReturn 已注释，本任务空跑）。
/// GroupName=ReportStatistics；周期 3600 秒。
/// </summary>
[JobDetail("job_agent_return_plan", Description = "代理返水（空跑）", GroupName = "ReportStatistics", Concurrent = false)]
[PeriodSeconds(3600, TriggerId = "trigger_agent_return_plan", Description = "每小时空跑一次", RunOnStart = false)]
public class AgentReturnPlanJob : IJob
{
    /// <summary>
    /// 空跑；旧自动发放已停用。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        context.Result = "空跑（旧 ExecuteAgentReturn 已注释）";
        return Task.CompletedTask;
    }
}

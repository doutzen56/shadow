using Microsoft.Extensions.Options;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 作业执行监视器：出错时记本地日志，按 ScheduleHost 配置决定是否发告警邮件。
/// </summary>
public class JobMonitor : IJobMonitor
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IOptionsMonitor<ScheduleHostOptions> _options;
    private readonly ILogger<JobMonitor> _logger;

    /// <summary>
    /// 注入事件发布、调度宿主配置和日志。
    /// </summary>
    /// <param name="eventPublisher">事件发布器</param>
    /// <param name="options">ScheduleHost 配置</param>
    /// <param name="logger">日志</param>
    public JobMonitor(IEventPublisher eventPublisher, IOptionsMonitor<ScheduleHostOptions> options, ILogger<JobMonitor> logger)
    {
        _eventPublisher = eventPublisher;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 作业执行前不做处理。
    /// </summary>
    /// <param name="context">执行前上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    /// <returns>已完成任务</returns>
    public Task OnExecutingAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 作业执行后：有异常就记日志，配置允许时再发告警邮件。
    /// </summary>
    /// <param name="context">执行后上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task OnExecutedAsync(JobExecutedContext context, CancellationToken stoppingToken)
    {
        if (context.Exception == null) return;

        var exception = $"定时任务【{context.Trigger.Description}】错误：{context.Exception}";
        _logger.LogError(exception);

        if (_options.CurrentValue.SendErrorMail)
            await _eventPublisher.PublishAsync(CommonConst.SendErrorMail, new ErrorAlertMessage { Source = "定时任务", Content = exception }, stoppingToken);
    }
}

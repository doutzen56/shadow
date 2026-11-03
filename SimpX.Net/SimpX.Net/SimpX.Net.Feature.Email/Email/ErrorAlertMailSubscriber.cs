namespace SimpX.Net.Feature.Email;

/// <summary>
/// 处理“发送告警邮件”事件。放在 Core，任何宿主发出的告警都能在本进程发出去。
/// </summary>
public sealed class ErrorAlertMailSubscriber : IEventSubscriber, ISingleton
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<EmailOptions> _emailOptions;
    private readonly ILogger<ErrorAlertMailSubscriber> _logger;

    /// <summary>
    /// 注入作用域工厂、邮件配置和日志。
    /// </summary>
    /// <param name="scopeFactory">作用域工厂</param>
    /// <param name="emailOptions">Email 配置</param>
    /// <param name="logger">日志</param>
    public ErrorAlertMailSubscriber(IServiceScopeFactory scopeFactory, IOptionsMonitor<EmailOptions> emailOptions, ILogger<ErrorAlertMailSubscriber> logger)
    {
        _scopeFactory = scopeFactory;
        _emailOptions = emailOptions;
        _logger = logger;
    }

    /// <summary>
    /// 发告警邮件。内容不是 <see cref="ErrorAlertMessage"/> 时按 JSON 原样发出；发送失败只记日志，不再抛出，避免告警本身又触发告警。
    /// </summary>
    /// <param name="context">事件上下文</param>
    /// <returns>异步任务</returns>
    [EventSubscribe(CommonConst.SendErrorMail)]
    public async Task SendErrorAlertMail(EventHandlerExecutingContext context)
    {
        var payload = context.Source.Payload;
        var alert = payload as ErrorAlertMessage ?? new ErrorAlertMessage { Content = payload as string ?? JSON.Serialize(payload) };

        var appName = string.IsNullOrWhiteSpace(_emailOptions.CurrentValue.DefaultFromName) ? "SimpX.Net" : _emailOptions.CurrentValue.DefaultFromName;
        var title = string.IsNullOrWhiteSpace(alert.Source) ? $"{appName} 系统异常" : $"{appName} {alert.Source}异常";

        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(title, alert.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "告警邮件发送失败：{Title}", title);
        }
    }
}

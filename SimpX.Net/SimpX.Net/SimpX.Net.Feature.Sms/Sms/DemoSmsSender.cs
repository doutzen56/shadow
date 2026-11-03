namespace SimpX.Net.Feature.Sms;

/// <summary>
/// 默认的演示短信通道：不对接任何服务商，只记一条日志（手机号脱敏，不记验证码）。
/// 只在 Sms:Demo:Enabled 为 true 时注册，验证码用配置里的固定值。
/// </summary>
public sealed class DemoSmsSender : ISmsSender
{
    private readonly ILogger<DemoSmsSender> _logger;

    /// <summary>
    /// 注入日志。
    /// </summary>
    /// <param name="logger">日志</param>
    public DemoSmsSender(ILogger<DemoSmsSender> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 模拟发送：只记录脱敏后的手机号和模板编号。
    /// </summary>
    /// <param name="message">短信内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已完成任务</returns>
    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("演示短信通道：已模拟发送，手机号 {Phone}，模板 {TemplateId}", MaskPhone(message.PhoneNumber), message.TemplateId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 手机号脱敏：保留前三位和后四位。
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <returns>脱敏后的手机号</returns>
    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 7) return "****";
        return phone[..3] + "****" + phone[^4..];
    }
}

namespace SimpX.Net.Feature.Sms;

/// <summary>
/// 短信发送通道。接入一家短信服务商就实现一个，并注册为 <see cref="ISmsSender"/>；没有注册时发送短信会直接报错。
/// </summary>
public interface ISmsSender
{
    /// <summary>
    /// 发送一条模板短信。
    /// </summary>
    /// <param name="message">短信内容（手机号、模板、模板参数）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <exception cref="AppFriendlyException">服务商返回发送失败</exception>
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// 一条模板短信。
/// </summary>
public sealed class SmsMessage
{
    /// <summary>手机号。</summary>
    public string PhoneNumber { get; set; }

    /// <summary>模板编号，由各通道自己映射到服务商的模板。</summary>
    public string TemplateId { get; set; }

    /// <summary>模板参数，如验证码 code。</summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

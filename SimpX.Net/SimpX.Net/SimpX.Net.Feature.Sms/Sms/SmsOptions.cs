namespace SimpX.Net.Feature.Sms;

/// <summary>
/// 短信配置，从配置里的 Sms 这一段读取。
/// </summary>
public sealed class SmsOptions : IConfigurableOptions
{
    /// <summary>
    /// 验证码有效期（秒）。
    /// </summary>
    public int VerifyCodeExpireSeconds { get; set; } = 60;

    /// <summary>
    /// 演示通道配置。
    /// </summary>
    public SmsDemoOptions Demo { get; set; } = new();
}

/// <summary>
/// 演示短信通道：不真正发短信，验证码固定，只用于开发联调。
/// </summary>
public sealed class SmsDemoOptions
{
    /// <summary>
    /// 是否启用演示通道。启用后任何人都能用固定验证码通过手机号校验，上线前一定要关掉。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 演示通道使用的固定验证码。
    /// </summary>
    public string FixedVerifyCode { get; set; } = "123456";
}

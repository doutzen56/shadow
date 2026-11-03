namespace SimpX.Net.Feature.ExternalLogin;

/// <summary>
/// 第三方登录配置，从配置里的 ExternalLogin 这一段读取。
/// </summary>
public sealed class ExternalLoginOptions : IConfigurableOptions
{
    /// <summary>
    /// 是否启用演示提供方（demo）。它不校验任何身份，谁都能登录成演示用户，只能在开发、联调时打开。
    /// </summary>
    public bool EnableDemo { get; set; }

    /// <summary>
    /// 登录完成后允许跳回的前端地址前缀（如 http://localhost:8888）。为空时只允许站内相对路径。
    /// </summary>
    public List<string> AllowedRedirectUrls { get; set; } = new();
}

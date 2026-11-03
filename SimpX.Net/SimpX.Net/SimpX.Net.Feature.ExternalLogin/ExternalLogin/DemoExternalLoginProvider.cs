namespace SimpX.Net.Feature.ExternalLogin;

/// <summary>
/// 默认的演示提供方：不对接任何真实平台，“授权页”直接带着演示授权码跳回回调地址，
/// 用来在没有第三方账号时把整条登录流程测通。只在 ExternalLogin:EnableDemo 为 true 时注册。
/// </summary>
public sealed class DemoExternalLoginProvider : IExternalLoginProvider
{
    private const string CodePrefix = "demo-";

    /// <summary>
    /// 提供方名称：demo。
    /// </summary>
    public string Name => "demo";

    /// <summary>
    /// 直接返回带演示授权码的回调地址，模拟用户已在第三方同意授权。
    /// </summary>
    /// <param name="callbackUrl">回调地址</param>
    /// <param name="state">防伪随机串</param>
    /// <returns>回调地址（已带 code 和 state）</returns>
    public Task<string> BuildAuthorizeUrlAsync(string callbackUrl, string state)
    {
        var separator = callbackUrl.Contains('?') ? '&' : '?';
        var code = CodePrefix + Guid.NewGuid().ToString("N");
        return Task.FromResult($"{callbackUrl}{separator}code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}");
    }

    /// <summary>
    /// 校验演示授权码格式，固定返回同一个演示用户，便于反复登录验证绑定逻辑。
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="callbackUrl">回调地址</param>
    /// <returns>演示用户信息</returns>
    /// <exception cref="AppFriendlyException">授权码不是演示格式</exception>
    public Task<ExternalUserInfo> GetUserInfoAsync(string code, string callbackUrl)
    {
        if (string.IsNullOrWhiteSpace(code) || !code.StartsWith(CodePrefix, StringComparison.Ordinal))
            throw Oops.Oh("授权码无效");

        return Task.FromResult(new ExternalUserInfo
        {
            Provider = Name,
            OpenId = "demo-user",
            NickName = "演示用户"
        });
    }
}

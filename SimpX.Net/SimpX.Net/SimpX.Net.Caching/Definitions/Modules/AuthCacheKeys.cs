namespace SimpX.Net.Caching;

/// <summary>
/// 登录与安全状态缓存键。新加缓存只在本文件加常量即可。
/// </summary>
public static class AuthCacheKeys
{
    /// <summary>模块名。</summary>
    public const string Module = "auth";

    /// <summary>密码错误次数。</summary>
    public const string PasswordError = "password-error";

    /// <summary>短信验证码。</summary>
    public const string PhoneCode = "phone-code";

    /// <summary>外部登录 state。</summary>
    public const string ExternalLogin = "external-login";

    /// <summary>开放接口随机数。</summary>
    public const string OpenAccessNonce = "open-access-nonce";

    /// <summary>图形验证码。</summary>
    public const string Captcha = "captcha";

    /// <summary>Token 黑名单。</summary>
    public const string TokenBlacklist = "token-blacklist";

    /// <summary>用户黑名单。</summary>
    public const string UserBlacklist = "user-blacklist";
}

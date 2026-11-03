namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 内部调用的常量。
/// </summary>
public static class InternalCallDefaults
{
    /// <summary>
    /// 内部签名身份验证方案名。接口上写 [Authorize(AuthenticationSchemes = InternalCallDefaults.AuthenticationScheme)]。
    /// </summary>
    public const string AuthenticationScheme = "InternalSignature";

    /// <summary>
    /// 签名验证通过后加到身份上的标记，后台权限检查看到它就直接放过（宿主之间互相信任，权限已在调用方检查过）。
    /// </summary>
    public const string ClaimType = "simpx:internal_call";
}

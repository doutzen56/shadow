namespace SimpX.Net.Application.Shared;

/// <summary>
/// 短信验证码校验。Admin 宿主注册实现；未注册时手机号登录不可用。
/// </summary>
public interface ISmsCodeVerifier
{
    /// <summary>
    /// 校验手机号与短信验证码是否匹配且未过期。
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="code">验证码</param>
    void VerifyPhoneCode(string phone, string code);
}

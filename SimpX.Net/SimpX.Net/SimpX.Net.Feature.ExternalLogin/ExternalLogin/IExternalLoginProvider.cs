namespace SimpX.Net.Feature.ExternalLogin;

/// <summary>
/// 第三方登录提供方。接入一家就实现一个，并注册为 <see cref="IExternalLoginProvider"/>。
/// 授权码流程：先跳到 <see cref="BuildAuthorizeUrlAsync"/> 给的地址，用户同意后对方带着 code 回调，再用 <see cref="GetUserInfoAsync"/> 换取用户信息。
/// </summary>
public interface IExternalLoginProvider
{
    /// <summary>
    /// 提供方名称，登录地址里的 provider 参数用它，大小写不敏感。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 构造跳转到第三方授权页的地址。
    /// </summary>
    /// <param name="callbackUrl">授权后回调到本系统的地址</param>
    /// <param name="state">防伪随机串，回调时原样带回</param>
    /// <returns>授权页地址</returns>
    Task<string> BuildAuthorizeUrlAsync(string callbackUrl, string state);

    /// <summary>
    /// 用回调带回的授权码换取第三方用户信息。
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="callbackUrl">发起授权时用的回调地址（部分平台换令牌时要求一致）</param>
    /// <returns>第三方用户信息</returns>
    Task<ExternalUserInfo> GetUserInfoAsync(string code, string callbackUrl);
}

/// <summary>
/// 第三方用户信息。OpenId 在同一提供方内唯一，用来和本系统用户绑定。
/// </summary>
public sealed class ExternalUserInfo
{
    /// <summary>提供方名称。</summary>
    public string Provider { get; set; }

    /// <summary>用户在该提供方的唯一标识。</summary>
    public string OpenId { get; set; }

    /// <summary>昵称。</summary>
    public string NickName { get; set; }

    /// <summary>头像地址。</summary>
    public string Avatar { get; set; }

    /// <summary>邮箱，没有时为空。</summary>
    public string Email { get; set; }
}

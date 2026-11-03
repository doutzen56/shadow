namespace SimpX.Net.Application.Shared;

/// <summary>
/// 域账号校验。Admin 宿主注册实现；总后台可不注册。
/// </summary>
public interface ILdapAccountAuthenticator
{
    /// <summary>
    /// 用域控校验账号密码。
    /// </summary>
    /// <param name="tenantId">租户 Id</param>
    /// <param name="account">域账号</param>
    /// <param name="password">明文密码</param>
    /// <returns>通过返回 true</returns>
    Task<bool> AuthAccount(long? tenantId, string account, string password);
}

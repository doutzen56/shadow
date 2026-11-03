using Furion.UnifyResult;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台兼容账号兼容路由：/admin/account/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "account", Order = 20)]
public class AccountAppService : IDynamicApiController, ITransient
{
    private readonly SiteAdminAuthService _auth;

    /// <summary>
    /// 注入站点后台登录服务。
    /// </summary>
    /// <param name="auth">站点后台登录</param>
    public AccountAppService(SiteAdminAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// 站点后台登录。对应旧版 POST /admin/account/login。
    /// </summary>
    /// <returns>旧版返回格式；成功时写 Cookie 和响应头 TOKEN</returns>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/admin/account/login")]
    [NonUnify]
    [DisplayName("站点后台登录")]
    public Task<OldApiResult> Login()
    {
        var http = App.HttpContext;
        return _auth.LoginAsync(
            OldApiRequestParam.Read(http, "UserName"),
            OldApiRequestParam.Read(http, "Password"),
            OldApiRequestParam.Read(http, "Code"),
            http?.RequestAborted ?? default);
    }

    /// <summary>
    /// 站点后台登出。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/admin/account/logout")]
    [NonUnify]
    [DisplayName("站点后台登出")]
    public Task<OldApiResult> Logout() =>
        _auth.LogoutAsync(App.HttpContext?.RequestAborted ?? default);
}

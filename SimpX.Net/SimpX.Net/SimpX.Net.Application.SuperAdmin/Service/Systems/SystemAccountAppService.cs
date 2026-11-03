using Furion.UnifyResult;

namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台账号兼容路由：/system/config/account/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "betwin-system-account", Order = 20)]
public class SystemAccountAppService : IDynamicApiController, ITransient
{
    private readonly SystemAdminAuthService _auth;

    /// <summary>
    /// 注入总后台登录服务。
    /// </summary>
    /// <param name="auth">总后台登录</param>
    public SystemAccountAppService(SystemAdminAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// 总后台登录。对应旧版 POST /system/config/account/login。
    /// </summary>
    /// <returns>旧版返回格式；成功时 info.Token，并写 Cookie</returns>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/system/config/account/login")]
    [NonUnify]
    [DisplayName("总后台登录")]
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
    /// 总后台登出。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/system/config/account/logout")]
    [NonUnify]
    [DisplayName("总后台登出")]
    public Task<OldApiResult> Logout() =>
        _auth.LogoutAsync(App.HttpContext?.RequestAborted ?? default);
}

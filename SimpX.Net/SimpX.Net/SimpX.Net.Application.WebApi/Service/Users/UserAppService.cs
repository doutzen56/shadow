using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员兼容路由：/request/user/*。入参来自 Form 和 Query；出参是旧版 success/msg/info（加 NonUnify，不要统一包装）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "user", Order = 20)]
public class UserAppService : IDynamicApiController, ITransient
{
    private readonly MemberAuthService _auth;
    private readonly MemberRegisterService _register;
    private readonly MemberProfileService _profile;

    /// <summary>
    /// 注入会员兼容登录、注册与资料。
    /// </summary>
    public UserAppService(
        MemberAuthService auth,
        MemberRegisterService register,
        MemberProfileService profile)
    {
        _auth = auth;
        _register = register;
        _profile = profile;
    }

    /// <summary>会员登录。路径跟旧版 POST /request/user/login。</summary>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/user/login")]
    [NonUnify]
    [DisplayName("会员登录")]
    public async Task<OldApiResult> Login()
    {
        var http = App.HttpContext;
        return await _auth.LoginAsync(
            OldApiRequestParam.Read(http, "UserName"),
            OldApiRequestParam.Read(http, "Password"),
            http?.RequestAborted ?? default);
    }

    /// <summary>会员注册（先做精简版）。</summary>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/user/register")]
    [NonUnify]
    [DisplayName("会员注册")]
    public async Task<OldApiResult> Register()
    {
        var http = App.HttpContext;
        return await _register.RegisterAsync(
            OldApiRequestParam.Read(http, "UserName"),
            OldApiRequestParam.Read(http, "Password"),
            OldApiRequestParam.Read(http, "Password2"),
            OldApiRequestParam.Read(http, "InviteCode") ?? OldApiRequestParam.Read(http, "InviteID"),
            http?.RequestAborted ?? default);
    }

    /// <summary>会员登出。</summary>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/user/logout")]
    [NonUnify]
    [DisplayName("会员登出")]
    public async Task<OldApiResult> Logout() => await _auth.LogoutAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>是否已登录。</summary>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/user/islogin")]
    [HttpGet("/request/user/islogin")]
    [NonUnify]
    [DisplayName("会员是否登录")]
    public async Task<OldApiResult> IsLogin() => await _auth.IsLoginAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>需登录探针（AUTH-05）。</summary>
    [AllowAnonymous]
    [HttpPost("/request/user/pingauth")]
    [HttpGet("/request/user/pingauth")]
    [NonUnify]
    [DisplayName("会员登录探针")]
    public OldApiResult PingAuth() => OldApiResult.Ok("ok");

    /// <summary>当前登录会员资料（脱敏）。对应旧版 /request/user/getinfo。</summary>
    [AllowAnonymous]
    [HttpPost("/request/user/getinfo")]
    [HttpGet("/request/user/getinfo")]
    [NonUnify]
    [DisplayName("会员资料")]
    public async Task<OldApiResult> GetInfo() => await _profile.GetSelfInfoAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>会话信息。对应旧 /request/user/info。</summary>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/user/info")]
    [HttpGet("/request/user/info")]
    [NonUnify]
    [DisplayName("会员会话信息")]
    public async Task<OldApiResult> Info() => await _profile.GetSessionInfoAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>银行卡列表。对应旧 /request/user/getbanklist。</summary>
    [AllowAnonymous]
    [HttpPost("/request/user/getbanklist")]
    [HttpGet("/request/user/getbanklist")]
    [NonUnify]
    [DisplayName("会员银行卡列表")]
    public async Task<OldApiResult> Getbanklist() =>
        await _profile.GetBankListAsync(
            OldApiRequestParam.Read(App.HttpContext, "IsVir"),
            App.HttpContext?.RequestAborted ?? default);

    /// <summary>当前登录会员安全信息。</summary>
    [AllowAnonymous]
    [HttpPost("/request/user/safeinfo")]
    [HttpGet("/request/user/safeinfo")]
    [NonUnify]
    [DisplayName("会员安全信息")]
    public async Task<OldApiResult> SafeInfo() => await _profile.GetSafeInfoAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>修改登录密码。</summary>
    [AllowAnonymous]
    [HttpPost("/request/user/updatepassword")]
    [NonUnify]
    [DisplayName("会员改登录密码")]
    public async Task<OldApiResult> UpdatePassword()
    {
        var http = App.HttpContext;
        return await _profile.UpdatePasswordAsync(
            OldApiRequestParam.Read(http, "Password"),
            OldApiRequestParam.Read(http, "NewPassword1"),
            OldApiRequestParam.Read(http, "NewPassword2"),
            http?.RequestAborted ?? default);
    }
}

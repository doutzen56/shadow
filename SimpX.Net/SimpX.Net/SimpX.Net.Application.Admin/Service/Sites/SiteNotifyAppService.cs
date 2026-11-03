using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台待办角标旧路径：/admin/site/notify/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-notify", Order = 25)]
public class SiteNotifyAppService : IDynamicApiController, ITransient
{
    /// <summary>沿用会员列表权限 GUID（细权限后续拆）。</summary>
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteAdminNotifyService _svc;

    /// <summary>注入。</summary>
    public SiteNotifyAppService(SiteAdminNotifyService svc) => _svc = svc;

    /// <summary>对齐旧 get。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/notify/get")]
    [HttpGet("/admin/site/notify/get")]
    [NonUnify]
    [DisplayName("站点待办角标")]
    public async Task<OldApiResult> Get() => await _svc.GetOldApiAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 update。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/notify/update")]
    [HttpGet("/admin/site/notify/update")]
    [NonUnify]
    [DisplayName("刷新站点待办角标")]
    public async Task<OldApiResult> Update() => await _svc.UpdateOldApiAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 withdraw。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/notify/withdraw")]
    [HttpGet("/admin/site/notify/withdraw")]
    [NonUnify]
    [DisplayName("待审提现数")]
    public async Task<OldApiResult> Withdraw() => await _svc.WithdrawOldApiAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 events。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/notify/events")]
    [HttpGet("/admin/site/notify/events")]
    [NonUnify]
    [DisplayName("待审活动数")]
    public async Task<OldApiResult> Events() => await _svc.EventsOldApiAsync(App.HttpContext?.RequestAborted ?? default);
}

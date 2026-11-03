using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点管理员旧路径：/admin/site/admin/*
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-admin", Order = 31)]
public class SiteAdminAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;
    private readonly SiteAdminManageService _svc;

    public SiteAdminAppService(SiteAdminManageService svc) => _svc = svc;

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/list")]
    [HttpGet("/admin/site/admin/list")]
    [NonUnify]
    [DisplayName("管理员-list")]
    public Task<OldApiResult> List() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return await _svc.PageAdminsAsync(new SiteAdminPageInput
            {
                Page = page,
                PageSize = size,
                AdminName = OldApiFormReader.ReadString(http, "AdminName", "UserName", "user"),
                GroupID = OldApiFormReader.ReadInt(http, "GroupID", "groupID")
            }, ct);
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/info")]
    [HttpGet("/admin/site/admin/info")]
    [NonUnify]
    [DisplayName("管理员-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _svc.GetAdminAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/save")]
    [NonUnify]
    [DisplayName("管理员-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            return await _svc.SaveAdminAsync(new SiteAdminSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                AdminName = OldApiFormReader.ReadString(http, "AdminName", "UserName"),
                NickName = OldApiFormReader.ReadString(http, "NickName", "nickName"),
                Password = OldApiFormReader.ReadString(http, "Password", "password"),
                Status = OldApiFormReader.ReadInt(http, "Status", "status"),
                GroupID = OldApiFormReader.ReadInt(http, "GroupID", "groupID"),
                AuditUpperLimit = OldApiFormReader.ReadInt(http, "AuditUpperLimit")
            }, ct);
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/delete")]
    [NonUnify]
    [DisplayName("管理员-delete")]
    public Task<OldApiResult> Delete() =>
        Run(async ct =>
        {
            await _svc.DeleteAdminAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/grouplist")]
    [HttpGet("/admin/site/admin/grouplist")]
    [NonUnify]
    [DisplayName("管理员-grouplist")]
    public Task<OldApiResult> Grouplist() => Run(ct => _svc.ListGroupsAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/groupinfo")]
    [HttpGet("/admin/site/admin/groupinfo")]
    [NonUnify]
    [DisplayName("管理员-groupinfo")]
    public Task<OldApiResult> Groupinfo() =>
        Run(ct => _svc.GetGroupAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/savegroupinfo")]
    [NonUnify]
    [DisplayName("管理员-savegroupinfo")]
    public Task<OldApiResult> Savegroupinfo() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            return await _svc.SaveGroupAsync(new SiteAdminGroupSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                PermissionString = OldApiFormReader.ReadString(http, "Permission", "PermissionString", "permission")
            }, ct);
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/deletegroup")]
    [NonUnify]
    [DisplayName("管理员-deletegroup")]
    public Task<OldApiResult> Deletegroup() =>
        Run(async ct =>
        {
            await _svc.DeleteGroupAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/refreshgroup")]
    [NonUnify]
    [DisplayName("管理员-refreshgroup")]
    public Task<OldApiResult> Refreshgroup() =>
        Run(async ct =>
        {
            await _svc.RefreshGroupAsync(ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/resetpassword")]
    [NonUnify]
    [DisplayName("管理员-resetpassword")]
    public Task<OldApiResult> Resetpassword() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.ResetPasswordAsync(new SiteAdminResetPwdInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Password = OldApiFormReader.ReadString(http, "Password", "password")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/clearcode")]
    [NonUnify]
    [DisplayName("管理员-clearcode")]
    public Task<OldApiResult> Clearcode() =>
        Run(async ct =>
        {
            await _svc.ClearTotpAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/sound")]
    [HttpGet("/admin/site/admin/sound")]
    [NonUnify]
    [DisplayName("管理员-sound")]
    public Task<OldApiResult> Sound() => Run(ct => _svc.GetAdminSoundAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/savesound")]
    [NonUnify]
    [DisplayName("管理员-savesound")]
    public Task<OldApiResult> Savesound() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveAdminSoundAsync(new SiteAdminSoundSaveInput
            {
                Type = OldApiFormReader.ReadString(http, "Type", "type"),
                Url = OldApiFormReader.ReadString(http, "Url", "url")
            }, ct);
            return (object?)null;
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

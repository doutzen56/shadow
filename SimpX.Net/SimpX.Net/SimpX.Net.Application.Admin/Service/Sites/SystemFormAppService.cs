using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台系统辅助旧路径：/admin/system/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "system-form", Order = 55)]
public class SystemFormAppService : IDynamicApiController, ITransient
{
    /// <summary>沿用会员列表权限 GUID（与其它 Admin 只读壳一致）。</summary>
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly AdminSystemService _svc;

    public SystemFormAppService(AdminSystemService svc) => _svc = svc;

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/system/updatenews")]
    [HttpGet("/admin/system/updatenews")]
    [NonUnify]
    [DisplayName("系统更新通知")]
    public async Task<OldApiResult> Updatenews()
    {
        var http = App.HttpContext;
        var (page, size) = OldApiShowResult.ReadPaging(http);
        var result = await _svc.UpdatenewsOldAsync(page, size, http?.RequestAborted ?? default);
        return result;
    }

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/system/billlist")]
    [HttpGet("/admin/system/billlist")]
    [NonUnify]
    [DisplayName("系统账单列表")]
    public async Task<OldApiResult> Billlist()
    {
        var (page, size) = OldApiShowResult.ReadPaging(App.HttpContext);
        var result = await _svc.BilllistOldAsync(page, size, App.HttpContext?.RequestAborted ?? default);
        return result;
    }

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/system/billinfo")]
    [HttpGet("/admin/system/billinfo")]
    [NonUnify]
    [DisplayName("系统账单详情")]
    public async Task<OldApiResult> Billinfo()
    {
        _ = int.TryParse(OldApiRequestParam.Read(App.HttpContext, "ID"), out var id);
        var result = await _svc.BillinfoOldAsync(id, App.HttpContext?.RequestAborted ?? default);
        return result;
    }

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/system/functioninfo")]
    [HttpGet("/admin/system/functioninfo")]
    [NonUnify]
    [DisplayName("系统功能开关")]
    public async Task<OldApiResult> Functioninfo()
    {
        var result = await _svc.FunctioninfoOldAsync(App.HttpContext?.RequestAborted ?? default);
        return result;
    }

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/system/updatefunction")]
    [NonUnify]
    [DisplayName("更新系统功能开关")]
    public async Task<OldApiResult> Updatefunction()
    {
        var http = App.HttpContext;
        var result = await _svc.UpdatefunctionOldAsync(
            OldApiRequestParam.Read(http, "IsOpen"),
            OldApiRequestParam.Read(http, "Value"),
            http?.RequestAborted ?? default);
        return result;
    }
}

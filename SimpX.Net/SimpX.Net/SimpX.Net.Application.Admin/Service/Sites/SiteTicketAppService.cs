using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>工单旧路径占位：无实体，先挂通返回尚未接通。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-ticket", Order = 33)]
public class SiteTicketAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/ticket/list"), HttpGet("/admin/site/ticket/list")]
    [HttpPost("/admin/site/ticket/info"), HttpGet("/admin/site/ticket/info")]
    [HttpPost("/admin/site/ticket/category"), HttpGet("/admin/site/ticket/category")]
    [HttpPost("/admin/site/ticket/addnew")]
    [HttpPost("/admin/site/ticket/addcomplement")]
    [HttpPost("/admin/site/ticket/saverank")]
    [NonUnify, DisplayName("site-ticket-stub")]
    public Task<OldApiResult> Stub() =>
        Task.FromResult(OldApiResult.Fail("#功能尚未接通#"));
}

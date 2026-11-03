using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 内容区域旧路径：/admin/site/region/* 与 regioncontent/*
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-region", Order = 30)]
public class SiteRegionFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;
    private readonly SiteRegionService _svc;

    public SiteRegionFormAppService(SiteRegionService svc) => _svc = svc;

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/region/list")]
    [HttpGet("/admin/site/region/list")]
    [NonUnify]
    [DisplayName("区域-list")]
    public Task<OldApiResult> List() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return await _svc.PageAsync(new SiteRegionPageInput
            {
                Page = page,
                PageSize = size,
                Name = OldApiFormReader.ReadString(http, "Name", "name")
            }, ct);
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/region/info")]
    [HttpGet("/admin/site/region/info")]
    [NonUnify]
    [DisplayName("区域-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _svc.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/region/save")]
    [NonUnify]
    [DisplayName("区域-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveAsync(new SiteRegionSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                Description = OldApiFormReader.ReadString(http, "Description", "description"),
                Type = OldApiFormReader.ReadInt(http, "Type", "type"),
                Content = OldApiFormReader.ReadString(http, "Content", "content")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/region/delete")]
    [NonUnify]
    [DisplayName("区域-delete")]
    public Task<OldApiResult> Delete() =>
        Run(async ct =>
        {
            await _svc.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/regioncontent/list")]
    [HttpGet("/admin/site/regioncontent/list")]
    [NonUnify]
    [DisplayName("区域内容-list")]
    public Task<OldApiResult> ContentList() =>
        Run(async ct =>
        {
            // 旧 regioncontent/list 常按 RegionID；无独立分页服务时复用区域详情 Content
            var detail = await _svc.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "RegionID", "ID", "Id"), ct);
            return detail;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/regioncontent/save")]
    [NonUnify]
    [DisplayName("区域内容-save")]
    public Task<OldApiResult> ContentSave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.ContentSaveAsync(new SiteRegionContentSaveInput
            {
                RegionID = OldApiFormReader.ReadInt(http, "RegionID", "regionID"),
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                Content = OldApiFormReader.ReadString(http, "Content", "content"),
                Url = OldApiFormReader.ReadString(http, "Url", "url"),
                Pic = OldApiFormReader.ReadString(http, "Pic", "pic"),
                Sort = OldApiFormReader.ReadInt(http, "Sort", "sort"),
                IsShow = OldApiFormReader.ReadBool(http, "IsShow", "isShow")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/regioncontent/delete")]
    [NonUnify]
    [DisplayName("区域内容-delete")]
    public Task<OldApiResult> ContentDelete() =>
        Run(async ct =>
        {
            await _svc.ContentDeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/regioncontent/info")]
    [HttpGet("/admin/site/regioncontent/info")]
    [NonUnify]
    [DisplayName("区域内容-info")]
    public Task<OldApiResult> ContentInfo() =>
        Run(ct => _svc.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "RegionID", "ID", "Id"), ct));

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

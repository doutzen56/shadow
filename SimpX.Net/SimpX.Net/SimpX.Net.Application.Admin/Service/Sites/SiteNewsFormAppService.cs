using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 新闻公告旧路径：/admin/site/news/*
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-news", Order = 28)]
public class SiteNewsFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;
    private readonly SiteNewsService _svc;

    public SiteNewsFormAppService(SiteNewsService svc) => _svc = svc;

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/list")]
    [HttpGet("/admin/site/news/list")]
    [NonUnify]
    [DisplayName("新闻-list")]
    public Task<OldApiResult> List() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return await _svc.PageAsync(new SiteNewsPageInput
            {
                Page = page,
                PageSize = size,
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                ColID = OldApiFormReader.ReadInt(http, "ColID", "colID")
            }, ct);
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/info")]
    [HttpGet("/admin/site/news/info")]
    [NonUnify]
    [DisplayName("新闻-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _svc.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/save")]
    [NonUnify]
    [DisplayName("新闻-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveAsync(new SiteNewsSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                ColID = OldApiFormReader.ReadInt(http, "ColID", "colID"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                Content = OldApiFormReader.ReadString(http, "Content", "content"),
                Cover = OldApiFormReader.ReadString(http, "Cover", "cover"),
                Sort = OldApiFormReader.ReadInt(http, "Sort", "sort"),
                IsTipAfterLogin = OldApiFormReader.ReadBool(http, "IsTipAfterLogin"),
                IsTipBeforeLogin = OldApiFormReader.ReadBool(http, "IsTipBeforeLogin")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/delete")]
    [NonUnify]
    [DisplayName("新闻-delete")]
    public Task<OldApiResult> Delete() =>
        Run(async ct =>
        {
            await _svc.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/columnlist")]
    [HttpGet("/admin/site/news/columnlist")]
    [NonUnify]
    [DisplayName("新闻-columnlist")]
    public Task<OldApiResult> Columnlist() =>
        Run(ct => _svc.ColumnListAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/getcolumninfo")]
    [HttpGet("/admin/site/news/getcolumninfo")]
    [NonUnify]
    [DisplayName("新闻-getcolumninfo")]
    public Task<OldApiResult> Getcolumninfo() =>
        Run(ct => _svc.GetcolumninfoAsync(ToDict(App.HttpContext), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/savecolumn")]
    [NonUnify]
    [DisplayName("新闻-savecolumn")]
    public Task<OldApiResult> Savecolumn() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveColumnAsync(new SiteNewsColumnSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Translate", "Name", "name"),
                Code = OldApiFormReader.ReadString(http, "Code", "code"),
                Sort = (short)OldApiFormReader.ReadInt(http, "Sort", "sort")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/savecolumncode")]
    [NonUnify]
    [DisplayName("新闻-savecolumncode")]
    public Task<OldApiResult> Savecolumncode() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveColumnCodeAsync(
                OldApiFormReader.ReadInt(http, "ID", "Id"),
                OldApiFormReader.ReadString(http, "Code", "code"),
                ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/deletecolumn")]
    [NonUnify]
    [DisplayName("新闻-deletecolumn")]
    public Task<OldApiResult> Deletecolumn() =>
        Run(async ct =>
        {
            await _svc.DeleteColumnAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/savecolumnsort")]
    [NonUnify]
    [DisplayName("新闻-savecolumnsort")]
    public Task<OldApiResult> Savecolumnsort() =>
        Run(async ct =>
        {
            var raw = OldApiFormReader.ReadString(App.HttpContext, "ID", "Id");
            var ids = (raw ?? string.Empty)
                .Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                .Where(n => n > 0)
                .ToArray();
            await _svc.SaveColumnSortAsync(ids, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/news/updatesort")]
    [HttpPost("/admin/site/news/savesort")]
    [NonUnify]
    [DisplayName("新闻-updatesort")]
    public Task<OldApiResult> Updatesort() =>
        Run(ct => _svc.UpdatesortAsync(ToDict(App.HttpContext), ct));

    private static Dictionary<string, object?> ToDict(Microsoft.AspNetCore.Http.HttpContext http)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in OldApiFormReader.ReadJsonMap(http))
            map[k] = v.ToString();
        return map;
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

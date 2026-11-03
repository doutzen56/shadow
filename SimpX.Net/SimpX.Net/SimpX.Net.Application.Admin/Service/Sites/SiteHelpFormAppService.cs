using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 帮助中心旧路径：/admin/site/help/*
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-help", Order = 29)]
public class SiteHelpFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;
    private readonly SiteHelpService _svc;

    public SiteHelpFormAppService(SiteHelpService svc) => _svc = svc;

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/help/list")]
    [HttpGet("/admin/site/help/list")]
    [NonUnify]
    [DisplayName("帮助-list")]
    public Task<OldApiResult> List() => Run(ct => _svc.ListAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/help/columnlist")]
    [HttpGet("/admin/site/help/columnlist")]
    [NonUnify]
    [DisplayName("帮助-columnlist")]
    public Task<OldApiResult> Columnlist() => Run(ct => _svc.ColumnListAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/help/info")]
    [HttpGet("/admin/site/help/info")]
    [NonUnify]
    [DisplayName("帮助-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _svc.InfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/help/save")]
    [NonUnify]
    [DisplayName("帮助-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveAsync(new SiteHelpSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                ColID = OldApiFormReader.ReadInt(http, "ColID", "colID"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                Content = OldApiFormReader.ReadString(http, "Content", "content"),
                Sort = (short)OldApiFormReader.ReadInt(http, "Sort", "sort")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/help/delete")]
    [NonUnify]
    [DisplayName("帮助-delete")]
    public Task<OldApiResult> Delete() =>
        Run(async ct =>
        {
            await _svc.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/help/columninfo")]
    [HttpGet("/admin/site/help/columninfo")]
    [NonUnify]
    [DisplayName("帮助-columninfo")]
    public Task<OldApiResult> Columninfo() =>
        Run(ct => _svc.ColumnInfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/help/columnsave")]
    [NonUnify]
    [DisplayName("帮助-columnsave")]
    public Task<OldApiResult> Columnsave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveColumnAsync(new SiteHelpColumnSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                Sort = (short)OldApiFormReader.ReadInt(http, "Sort", "sort"),
                Url = OldApiFormReader.ReadString(http, "Url", "url"),
                IsFooter = OldApiFormReader.ReadBool(http, "IsFooter", "isFooter")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/help/columndelete")]
    [NonUnify]
    [DisplayName("帮助-columndelete")]
    public Task<OldApiResult> Columndelete() =>
        Run(async ct =>
        {
            await _svc.DeleteColumnAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/help/columnsort")]
    [NonUnify]
    [DisplayName("帮助-columnsort")]
    public Task<OldApiResult> Columnsort() =>
        Run(ct =>
        {
            var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in OldApiFormReader.ReadJsonMap(App.HttpContext))
                map[k] = v.ToString();
            return _svc.ColumnsortAsync(map, ct);
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

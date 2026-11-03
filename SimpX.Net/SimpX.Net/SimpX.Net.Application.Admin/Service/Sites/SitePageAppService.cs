using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点页面配置旧路径：/admin/site/page/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-page", Order = 28)]
public class SitePageAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteAdminManageService _admin;

    public SitePageAppService(SiteAdminManageService admin) => _admin = admin;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/page/itemlist"), HttpGet("/admin/site/page/itemlist")]
    [HttpPost("/admin/site/page/setting"), HttpGet("/admin/site/page/setting")]
    [NonUnify, DisplayName("site-page-itemlist")]
    public Task<OldApiResult> Itemlist() =>
        Run(ct => _admin.ListPageSettingsAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/page/iteminfo"), HttpGet("/admin/site/page/iteminfo")]
    [NonUnify, DisplayName("site-page-iteminfo")]
    public Task<OldApiResult> Iteminfo() =>
        Run(async ct =>
        {
            var id = OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id");
            var list = await _admin.ListPageSettingsAsync(ct);
            var row = list.FirstOrDefault(x => x.ID == id);
            return (object?)(row ?? throw Oops.Oh("#记录不存在#"));
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/page/saveitem"), HttpPost("/admin/site/page/saveitemsort")]
    [NonUnify, DisplayName("site-page-saveitem")]
    public Task<OldApiResult> Saveitem() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _admin.SavePageSettingAsync(new SitePageSettingSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Sort = (short)OldApiFormReader.ReadInt(http, "Sort", "sort"),
                IsHide = OldApiFormReader.ReadInt(http, "IsHide", "isHide") == 1
                    || string.Equals(OldApiFormReader.ReadString(http, "IsHide"), "true", StringComparison.OrdinalIgnoreCase)
            }, ct);
            return (object?)null;
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

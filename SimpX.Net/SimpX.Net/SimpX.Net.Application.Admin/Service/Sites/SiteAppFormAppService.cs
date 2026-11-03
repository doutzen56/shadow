using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点 APP 旧路径：/admin/site/app/*（list / appinfo / savecustomqrcode；upload 分片后置）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-app", Order = 29)]
public class SiteAppFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteAppAdminService _svc;

    public SiteAppFormAppService(SiteAppAdminService svc) => _svc = svc;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/app/list"), HttpGet("/admin/site/app/list")]
    [NonUnify, DisplayName("site-app-list")]
    public Task<OldApiResult> List() =>
        Run(ct => _svc.ListAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/app/appinfo"), HttpGet("/admin/site/app/appinfo")]
    [NonUnify, DisplayName("site-app-appinfo")]
    public Task<OldApiResult> Appinfo() =>
        Run(ct => _svc.AppInfoAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/app/savecustomqrcode")]
    [NonUnify, DisplayName("site-app-savecustomqrcode")]
    public Task<OldApiResult> Savecustomqrcode() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveCustomQrCodeAsync(
                OldApiFormReader.ReadString(http, "CustomQRCodeShow", "customQRCodeShow"),
                OldApiFormReader.ReadString(http, "CustomDownload", "customDownload"),
                ct);
            return (object?)null;
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

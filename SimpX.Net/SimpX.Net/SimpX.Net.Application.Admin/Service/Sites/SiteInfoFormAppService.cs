using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点设置旧路径：/admin/site/info/*
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-info", Order = 26)]
public class SiteInfoFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;
    private readonly SiteInfoService _svc;

    public SiteInfoFormAppService(SiteInfoService svc) => _svc = svc;

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/get")]
    [HttpGet("/admin/site/info/get")]
    [NonUnify]
    [DisplayName("站点设置-get")]
    public Task<OldApiResult> Get() =>
        Run(ct => _svc.GetAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/getgooglesecret"), HttpGet("/admin/site/info/getgooglesecret")]
    [NonUnify, DisplayName("site-info-getgooglesecret")]
    public Task<OldApiResult> Getgooglesecret() =>
        Run(async ct =>
        {
            var info = await _svc.GetAsync(ct);
            object? secret = null;
            info.Setting?.TryGetValue("GoogleSecret", out secret);
            return (object?)(secret?.ToString() ?? string.Empty);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/savegooglesecret")]
    [NonUnify, DisplayName("site-info-savegooglesecret")]
    public Task<OldApiResult> Savegooglesecret() =>
        Run(async ct =>
        {
            var secret = OldApiFormReader.ReadString(App.HttpContext, "GoogleSecret", "Secret", "secret");
            await _svc.SaveGoogleSecretAsync(secret, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/save")]
    [NonUnify]
    [DisplayName("站点设置-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveAsync(new SiteInfoSaveInput
            {
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                Currency = OldApiFormReader.ReadInt(http, "Currency", "currency"),
                PCHomeTemplateName = OldApiFormReader.ReadString(http, "PCHomeTemplateName", "PCSetting.HomeTemplateName"),
                Setting = OldApiFormReader.ReadJsonMap(http)
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/api")]
    [HttpGet("/admin/site/info/api")]
    [NonUnify]
    [DisplayName("站点设置-api")]
    public Task<OldApiResult> Api() =>
        Run(ct => _svc.GetApiKeyAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/updateapikey")]
    [NonUnify]
    [DisplayName("站点设置-updateapikey")]
    public Task<OldApiResult> Updateapikey() =>
        Run(ct => _svc.UpdateApiKeyAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/cache")]
    [NonUnify]
    [DisplayName("站点设置-cache")]
    public Task<OldApiResult> Cache() =>
        Run(async ct =>
        {
            await _svc.ClearCacheAsync(OldApiFormReader.ReadString(App.HttpContext, "key", "Key"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/online")]
    [HttpGet("/admin/site/info/online")]
    [NonUnify]
    [DisplayName("站点设置-online")]
    public Task<OldApiResult> Online() =>
        Run(ct => _svc.OnlineAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/admindomain")]
    [HttpGet("/admin/site/info/admindomain")]
    [NonUnify]
    [DisplayName("站点设置-admindomain")]
    public Task<OldApiResult> Admindomain() =>
        Run(ct => _svc.AdminDomainAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/saveadmindomain")]
    [NonUnify]
    [DisplayName("站点设置-saveadmindomain")]
    public Task<OldApiResult> Saveadmindomain() =>
        Run(async ct =>
        {
            await _svc.SaveAdminDomainAsync(OldApiFormReader.ReadString(App.HttpContext, "AdminDomain", "adminDomain"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/list"), HttpGet("/admin/site/info/list")]
    [NonUnify]
    [DisplayName("站点设置-list")]
    public Task<OldApiResult> List() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _svc.ListAsync(
                OldApiFormReader.ReadString(http, "Name", "name"),
                OldApiFormReader.ReadString(http, "Type", "Status", "status"),
                ct);
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/getgamecredit"), HttpGet("/admin/site/info/getgamecredit")]
    [NonUnify]
    [DisplayName("站点设置-getgamecredit")]
    public Task<OldApiResult> Getgamecredit() =>
        Run(async ct =>
        {
            var rows = await _svc.ListAsync(null, null, ct);
            return rows.FirstOrDefault() ?? new { GameCredit = 0m };
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/info/getnotifycredit"), HttpGet("/admin/site/info/getnotifycredit")]
    [HttpPost("/admin/site/info/getsitetype"), HttpGet("/admin/site/info/getsitetype")]
    [NonUnify]
    [DisplayName("站点设置-getnotifycredit/getsitetype")]
    public Task<OldApiResult> Getnotifycredit() =>
        Run(async ct =>
        {
            var rows = await _svc.ListAsync(null, null, ct);
            return rows.FirstOrDefault() ?? new { };
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

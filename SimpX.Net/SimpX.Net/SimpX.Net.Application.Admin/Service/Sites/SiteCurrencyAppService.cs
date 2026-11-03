using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点币种旧路径：/admin/site/currency/*
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-currency", Order = 27)]
public class SiteCurrencyAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;
    private readonly SiteCurrencyAdminService _svc;

    public SiteCurrencyAppService(SiteCurrencyAdminService svc) => _svc = svc;

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/list")]
    [HttpGet("/admin/site/currency/list")]
    [NonUnify]
    [DisplayName("币种-list")]
    public Task<OldApiResult> List() => Run(ct => _svc.ListAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/info")]
    [HttpGet("/admin/site/currency/info")]
    [NonUnify]
    [DisplayName("币种-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _svc.InfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/save")]
    [NonUnify]
    [DisplayName("币种-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveAsync(
                OldApiFormReader.ReadInt(http, "ID", "Id"),
                OldApiFormReader.ReadInt(http, "Sort", "sort"),
                OldApiFormReader.ReadBool(http, "IsOpen", "isOpen"),
                OldApiFormReader.ReadInt(http, "Accuracy", "accuracy"),
                OldApiFormReader.ReadBool(http, "IsRegister", "isRegister"),
                ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/initsitecurrency")]
    [HttpGet("/admin/site/currency/initsitecurrency")]
    [NonUnify]
    [DisplayName("币种-InitSiteCurrency")]
    public Task<OldApiResult> Initsitecurrency() =>
        Run(async ct =>
        {
            await _svc.InitSiteCurrencyAsync(ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/getcurrency")]
    [HttpGet("/admin/site/currency/getcurrency")]
    [NonUnify]
    [DisplayName("币种-getcurrency")]
    public Task<OldApiResult> Getcurrency() =>
        Run(ct =>
        {
            var raw = OldApiFormReader.ReadString(App.HttpContext, "IsOpen", "isOpen");
            bool? open = string.IsNullOrEmpty(raw) ? null : OldApiFormReader.ReadBool(App.HttpContext, "IsOpen", "isOpen");
            return _svc.GetCurrencyAsync(open, ct);
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/show")]
    [HttpGet("/admin/site/currency/show")]
    [NonUnify]
    [DisplayName("币种-show")]
    public Task<OldApiResult> Show() => Run(ct => _svc.ShowAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/showreport")]
    [HttpGet("/admin/site/currency/showreport")]
    [NonUnify]
    [DisplayName("币种-showreport")]
    public Task<OldApiResult> Showreport() => Run(ct => _svc.ShowReportAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/report")]
    [HttpGet("/admin/site/currency/report")]
    [NonUnify]
    [DisplayName("币种-report")]
    public Task<OldApiResult> Report() => Run(ct => _svc.ReportAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/sort")]
    [NonUnify]
    [DisplayName("币种-sort")]
    public Task<OldApiResult> Sort() =>
        Run(async ct =>
        {
            await _svc.SortAsync(OldApiFormReader.ReadString(App.HttpContext, "Currency", "currency"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/currency/topone")]
    [HttpGet("/admin/site/currency/topone")]
    [NonUnify]
    [DisplayName("币种-topone")]
    public Task<OldApiResult> Topone() => Run(ct => _svc.TopOneAsync(ct));

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

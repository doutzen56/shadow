using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台兼容域名：/admin/site/domain/*。只操作当前站点。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-domain", Order = 23)]
public class SiteDomainFormAppService : IDynamicApiController, ITransient
{
    private readonly SiteDomainService _domains;

    /// <summary>
    /// 注入域名服务。
    /// </summary>
    /// <param name="domains">域名查询</param>
    public SiteDomainFormAppService(SiteDomainService domains)
    {
        _domains = domains;
    }

    /// <summary>
    /// 当前站域名列表。对应旧版 /admin/site/domain/list；不接受客户端 SiteID。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(SiteDomainService.DomainManagePermissionId)]
    [HttpPost("/admin/site/domain/list")]
    [HttpGet("/admin/site/domain/list")]
    [HttpPost("/admin/site/domain/nodelist")]
    [HttpGet("/admin/site/domain/nodelist")]
    [NonUnify]
    [DisplayName("站点后台域名列表")]
    public Task<OldApiResult> List() =>
        _domains.ListOldApiAsync(App.HttpContext?.RequestAborted ?? default);

    [AllowAnonymous, OldApiPermission(SiteDomainService.DomainManagePermissionId)]
    [HttpPost("/admin/site/domain/info"), HttpGet("/admin/site/domain/info")]
    [NonUnify, DisplayName("site-domain-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _domains.InfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(SiteDomainService.DomainManagePermissionId)]
    [HttpPost("/admin/site/domain/save")]
    [NonUnify, DisplayName("site-domain-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _domains.SaveAsync(new SiteDomainSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Domain = OldApiFormReader.ReadString(http, "Domain", "domain"),
                AgentName = OldApiFormReader.ReadString(http, "AgentName", "agentName"),
                Weights = (short)OldApiFormReader.ReadInt(http, "Weights", "weights"),
                IsVisible = OldApiFormReader.ReadInt(http, "IsVisible") == 1,
                IsSSL = OldApiFormReader.ReadInt(http, "IsSSL") == 1,
                IsAPP = OldApiFormReader.ReadInt(http, "IsAPP") == 1,
                IsSingle = OldApiFormReader.ReadInt(http, "IsSingle") == 1,
                Speed = OldApiFormReader.ReadInt(http, "Speed", "speed")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(SiteDomainService.DomainManagePermissionId)]
    [HttpPost("/admin/site/domain/delete")]
    [NonUnify, DisplayName("site-domain-delete")]
    public Task<OldApiResult> Delete() =>
        Run(async ct =>
        {
            await _domains.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

using Furion.UnifyResult;

namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台站点兼容：/system/site/info/*。不按域名确定当前站点；查站靠请求里显式传的 SiteID。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "betwin-system-site", Order = 22)]
public class SystemSiteAppService : IDynamicApiController, ITransient
{
    private readonly SystemSiteService _sites;

    /// <summary>
    /// 注入站点查询。
    /// </summary>
    /// <param name="sites">总后台站点查询</param>
    public SystemSiteAppService(SystemSiteService sites)
    {
        _sites = sites;
    }

    /// <summary>
    /// 站点列表。对应旧版 POST/GET /system/site/info/sitelist。
    /// </summary>
    /// <returns>旧版返回格式；info 是站点数组</returns>
    [AllowAnonymous]
    [OldApiPermission(SystemSiteService.SiteListPermissionId)]
    [HttpPost("/system/site/info/sitelist")]
    [HttpGet("/system/site/info/sitelist")]
    [NonUnify]
    [DisplayName("总后台站点列表")]
    public Task<OldApiResult> SiteList()
    {
        var http = App.HttpContext;
        var siteId = OldApiRequestParam.ReadInt(http, "SiteID");
        var name = OldApiRequestParam.Read(http, "Name");
        var typeText = OldApiRequestParam.Read(http, "Type");
        int? status = null;
        if (!string.IsNullOrWhiteSpace(typeText) && int.TryParse(typeText.Trim(), out var st))
            status = st;

        return _sites.SiteListAsync(siteId, name, status, http?.RequestAborted ?? default);
    }

    /// <summary>
    /// 站点列表（list 别名，字段与 sitelist 相同子集）。对应旧版 /system/site/info/list。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(SystemSiteService.SiteListPermissionId)]
    [HttpPost("/system/site/info/list")]
    [HttpGet("/system/site/info/list")]
    [NonUnify]
    [DisplayName("总后台站点列表list")]
    public Task<OldApiResult> List() => SiteList();

    /// <summary>
    /// 站点主机列表。对应旧版 /system/site/host/list；必须显式 SiteID。
    /// </summary>
    /// <returns>旧版返回格式；info 是主机数组</returns>
    [AllowAnonymous]
    [OldApiPermission(SystemSiteService.SiteHostPermissionId)]
    [HttpPost("/system/site/host/list")]
    [HttpGet("/system/site/host/list")]
    [NonUnify]
    [DisplayName("总后台站点主机列表")]
    public Task<OldApiResult> HostList()
    {
        var siteId = OldApiRequestParam.ReadInt(App.HttpContext, "SiteID");
        return _sites.HostListAsync(siteId, App.HttpContext?.RequestAborted ?? default);
    }
}


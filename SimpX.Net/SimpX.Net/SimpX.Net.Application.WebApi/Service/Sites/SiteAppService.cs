using Furion.UnifyResult;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员侧站点公开接口：/request/site/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site", Order = 21)]
public class SiteAppService : IDynamicApiController, ITransient
{
    private readonly RequestSiteService _sites;
    private readonly SiteDomainService _domains;
    private readonly SiteConfigService _config;

    /// <summary>
    /// 注入站点公开查询。
    /// </summary>
    /// <param name="sites">会员侧站点服务</param>
    /// <param name="domains">站点域名</param>
    /// <param name="config">站点配置</param>
    public SiteAppService(RequestSiteService sites, SiteDomainService domains, SiteConfigService config)
    {
        _sites = sites;
        _domains = domains;
        _config = config;
    }

    /// <summary>
    /// 当前站点的公开信息。对应旧版 POST/GET /request/site/info/get。
    /// </summary>
    /// <returns>旧版返回格式；info 含 SiteID、Name 等常用字段</returns>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/site/info/get")]
    [HttpGet("/request/site/info/get")]
    [NonUnify]
    [DisplayName("会员站点信息")]
    public Task<OldApiResult> Get() =>
        _sites.GetAsync(App.HttpContext?.RequestAborted ?? default);

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/info/logo"), HttpGet("/request/site/info/logo")]
    [NonUnify, DisplayName("site-logo")]
    public Task<OldApiResult> Logo() =>
        Run(ct => _sites.LogoAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/info/getareacode"), HttpGet("/request/site/info/getareacode")]
    [NonUnify, DisplayName("site-areacode")]
    public Task<OldApiResult> Getareacode() =>
        Run(ct => _sites.GetAreaCodeAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/domain/list"), HttpGet("/request/site/domain/list")]
    [NonUnify, DisplayName("site-domain-list")]
    public Task<OldApiResult> Domainlist() =>
        Run(ct => _domains.ListAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/domain/seo"), HttpGet("/request/site/domain/seo")]
    [NonUnify, DisplayName("site-domain-seo")]
    public Task<OldApiResult> Domainseo() =>
        Run(ct => _config.SeoSettingAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/info/sitebackground"), HttpGet("/request/site/info/sitebackground")]
    [NonUnify, DisplayName("site-background")]
    public Task<OldApiResult> Sitebackground() =>
        Run(ct => _config.GetBackgroundConfigAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/domain/info"), HttpGet("/request/site/domain/info")]
    [HttpPost("/request/site/domain/domaininfo"), HttpGet("/request/site/domain/domaininfo")]
    [HttpPost("/request/site/domain/nodelist"), HttpGet("/request/site/domain/nodelist")]
    [NonUnify, DisplayName("site-domain-extra")]
    public Task<OldApiResult> DomainExtra() => Domainlist();

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/info/app"), HttpGet("/request/site/info/app")]
    [HttpPost("/request/site/app/apk"), HttpGet("/request/site/app/apk")]
    [HttpPost("/request/site/info/getlabel"), HttpGet("/request/site/info/getlabel")]
    [HttpPost("/request/site/page/info"), HttpGet("/request/site/page/info")]
    [NonUnify, DisplayName("site-app-page")]
    public Task<OldApiResult> AppPage() =>
        Run(_ => Task.FromResult<object>(new { }));

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

using Furion.UnifyResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;
using SiteEntity = SimpX.Net.Data.Entity.Sites.Site;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员侧配置旧路径：/request/config/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "config", Order = 12)]
public class ConfigAppService : IDynamicApiController, ITransient
{
    private readonly IBizDbAccessor _biz;
    private readonly ICurrentSiteAccessor _site;
    private readonly ExchangeRateService _rates;
    private readonly IHttpContextAccessor _http;

    public ConfigAppService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        ExchangeRateService rates,
        IHttpContextAccessor http)
    {
        _biz = biz;
        _site = site;
        _rates = rates;
        _http = http;
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/config/ping"), HttpGet("/request/config/ping")]
    [NonUnify, DisplayName("config-ping")]
    public Task<OldApiResult> Ping() =>
        Run(_ => Task.FromResult<object?>("#响应成功#"));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/config/header"), HttpGet("/request/config/header")]
    [NonUnify, DisplayName("config-header")]
    public Task<OldApiResult> Header() =>
        Run(_ =>
        {
            var http = _http.HttpContext;
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (http != null)
            {
                foreach (var h in http.Request.Headers)
                    map[h.Key] = h.Value.ToString();
            }

            return Task.FromResult<object?>(map);
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/config/getcurrencyrate"), HttpGet("/request/config/getcurrencyrate")]
    [NonUnify, DisplayName("config-getcurrencyrate")]
    public Task<OldApiResult> Getcurrencyrate() =>
        Run(ct => _rates.ListAsync(new ExchangeRateListInput
        {
            Currency = OldApiFormReader.ReadInt(App.HttpContext, "Currency", "currency", "Base")
        }, ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/config/getagent"), HttpGet("/request/config/getagent")]
    [NonUnify, DisplayName("config-getagent")]
    public Task<OldApiResult> Getagent() =>
        Run(async ct =>
        {
            var siteName = OldApiFormReader.ReadString(App.HttpContext, "SiteName", "Name");
            if (string.IsNullOrWhiteSpace(siteName))
                throw Oops.Oh("#站点名称不能为空#");
            var exists = await _biz.Db.Queryable<SiteEntity>()
                .AnyAsync(s => s.Name != null && s.Name.Contains(siteName.Trim()), ct);
            return new { IsAgent = exists };
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/config/enumtype"), HttpGet("/request/config/enumtype")]
    [NonUnify, DisplayName("config-enumtype")]
    public Task<OldApiResult> Enumtype() =>
        Run(_ =>
        {
            var name = OldApiFormReader.ReadString(App.HttpContext, "Name", "name");
            if (string.IsNullOrWhiteSpace(name))
                throw Oops.Oh("#请输入枚举类型#");
            // 标签库未迁全量时返回空字典，避免前端崩。
            return Task.FromResult<object?>(new Dictionary<string, string>());
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/config/sitecurrency"), HttpGet("/request/config/sitecurrency")]
    [NonUnify, DisplayName("config-sitecurrency")]
    public Task<OldApiResult> Sitecurrency() =>
        Run(async ct =>
        {
            var siteId = _site.RequireSiteId();
            var rows = await _biz.Db.Queryable<SiteCurrency>()
                .Where(c => c.SiteID == siteId && c.IsOpen)
                .OrderBy(c => c.Sort)
                .Select(c => new { c.Currency, c.Accuracy, c.IsRegister })
                .ToListAsync(ct);
            return rows;
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/config/sitelanguage"), HttpGet("/request/config/sitelanguage")]
    [NonUnify, DisplayName("config-sitelanguage")]
    public Task<OldApiResult> Sitelanguage() =>
        Run(async ct =>
        {
            var siteId = _site.RequireSiteId();
            var rows = await _biz.Db.Queryable<SiteSupportLanguage>()
                .Where(l => l.SiteID == siteId && l.IsOpen)
                .OrderBy(l => l.Sort)
                .Select(l => new { l.Language, l.Sort })
                .ToListAsync(ct);
            return rows;
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员内容旧路径：/request/content/{news|help|events|region}/*（Guest）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "content", Order = 22)]
public class ContentAppService : IDynamicApiController, ITransient
{
    private readonly SiteNewsService _news;
    private readonly SiteHelpService _help;
    private readonly SiteEventService _events;
    private readonly SiteRegionService _region;

    public ContentAppService(
        SiteNewsService news,
        SiteHelpService help,
        SiteEventService events,
        SiteRegionService region)
    {
        _news = news;
        _help = help;
        _events = events;
        _region = region;
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/news/list"), HttpGet("/request/content/news/list")]
    [NonUnify, DisplayName("news-list")]
    public Task<OldApiResult> Newslist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return _news.PageAsync(new SiteNewsPageInput
            {
                Page = page,
                PageSize = size,
                ColID = OldApiFormReader.ReadInt(http, "ColID", "colID"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                IsTip = OldApiFormReader.ReadString(http, "IsTip", "IsTipAfterLogin", "IsTipBeforeLogin"),
                IsTop = OldApiFormReader.ReadInt(http, "IsTop"),
                Content = 1
            }, ct);
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/news/column"), HttpGet("/request/content/news/column")]
    [NonUnify, DisplayName("news-column")]
    public Task<OldApiResult> Newscolumn() =>
        Run(ct => _news.ColumnListAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/news/info"), HttpGet("/request/content/news/info")]
    [NonUnify, DisplayName("news-info")]
    public Task<OldApiResult> Newsinfo() =>
        Run(ct => _news.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/news/tip"), HttpGet("/request/content/news/tip")]
    [NonUnify, DisplayName("news-tip")]
    public Task<OldApiResult> Newstip() =>
        Run(async ct =>
        {
            var page = await _news.PageAsync(new SiteNewsPageInput
            {
                Page = 1,
                PageSize = 50,
                IsTip = "1",
                Content = 0
            }, ct);
            var ids = (page.Data ?? []).Select(x => x.ID).ToArray();
            if (ids.Length == 0)
                throw Oops.Oh("#没有需要弹出的新闻#");
            return ids;
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/notice/list"), HttpGet("/request/content/notice/list")]
    [NonUnify, DisplayName("notice-list")]
    public Task<OldApiResult> Noticelist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return _news.PageAsync(new SiteNewsPageInput
            {
                Page = page,
                PageSize = size,
                ColID = OldApiFormReader.ReadInt(http, "ColID", "colID"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                Content = 1
            }, ct);
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/notice/info"), HttpGet("/request/content/notice/info")]
    [NonUnify, DisplayName("notice-info")]
    public Task<OldApiResult> Noticeinfo() =>
        Run(ct => _news.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/help/column"), HttpGet("/request/content/help/column")]
    [HttpPost("/request/content/help/footercolumn"), HttpGet("/request/content/help/footercolumn")]
    [NonUnify, DisplayName("help-column")]
    public Task<OldApiResult> Helpcolumn() =>
        Run(ct => _help.ColumnListAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/help/info"), HttpGet("/request/content/help/info")]
    [NonUnify, DisplayName("help-info")]
    public Task<OldApiResult> Helpinfo() =>
        Run(ct => _help.InfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/events/list"), HttpGet("/request/content/events/list")]
    [HttpPost("/request/content/events/column"), HttpGet("/request/content/events/column")]
    [NonUnify, DisplayName("events-list")]
    public Task<OldApiResult> Eventslist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _events.ListAsync(new SiteEventListInput
            {
                Type = OldApiFormReader.ReadString(http, "Type", "type"),
                ColID = OldApiFormReader.ReadInt(http, "ColID", "colID"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                Currency = OldApiFormReader.ReadString(http, "Currency", "currency")
            }, ct);
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/events/info"), HttpGet("/request/content/events/info")]
    [NonUnify, DisplayName("events-info")]
    public Task<OldApiResult> Eventsinfo() =>
        Run(ct => _events.InfoAsync(
            OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"),
            OldApiFormReader.ReadInt(App.HttpContext, "Type", "type"),
            ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/events/recharge"), HttpGet("/request/content/events/recharge")]
    [NonUnify, DisplayName("events-recharge")]
    public Task<OldApiResult> Eventsrecharge() =>
        Run(ct => _events.ListAsync(new SiteEventListInput
        {
            Type = "9"
        }, ct));

    [AllowAnonymous]
    [HttpPost("/request/content/events/open")]
    [NonUnify, DisplayName("events-open")]
    public Task<OldApiResult> Eventsopen() =>
        Run(_ => Task.FromResult<object?>(Array.Empty<object>()));

    [AllowAnonymous]
    [HttpPost("/request/content/events/nonopen")]
    [NonUnify, DisplayName("events-nonopen")]
    public Task<OldApiResult> Eventsnonopen() =>
        Run(_ => Task.FromResult<object?>(null));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/content/region/get"), HttpGet("/request/content/region/get")]
    [NonUnify, DisplayName("region-get")]
    public Task<OldApiResult> Regionget() =>
        Run(ct => _region.GetContentByNameAsync(
            OldApiFormReader.ReadString(App.HttpContext, "Name", "name"),
            ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/region/list"), HttpGet("/request/site/region/list")]
    [HttpPost("/request/site/region/content"), HttpGet("/request/site/region/content")]
    [NonUnify, DisplayName("site-region-list")]
    public Task<OldApiResult> Siteregionlist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _region.ListContentByNameAsync(
                OldApiFormReader.ReadString(http, "Name", "name"),
                OldApiFormReader.ReadString(http, "AdNo", "adNo"),
                ct);
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

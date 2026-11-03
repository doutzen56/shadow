using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员游戏目录扩展：slot / subassort / winshowcase / remote stub。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-catalog-extra", Order = 34)]
public class GameCatalogExtraAppService : IDynamicApiController, ITransient
{
    private readonly IBizDbAccessor _biz;
    private readonly ICurrentSiteAccessor _site;
    private readonly SiteGameCatalogService _catalog;

    public GameCatalogExtraAppService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        SiteGameCatalogService catalog)
    {
        _biz = biz;
        _site = site;
        _catalog = catalog;
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/game/slot/list"), HttpGet("/request/game/slot/list")]
    [HttpPost("/request/game/subassort/list"), HttpGet("/request/game/subassort/list")]
    [HttpPost("/request/game/subassort/sitegamelist"), HttpGet("/request/game/subassort/sitegamelist")]
    [HttpPost("/request/game/subassort/sitegamesubassort"), HttpGet("/request/game/subassort/sitegamesubassort")]
    [HttpPost("/request/game/winshowcase/list"), HttpGet("/request/game/winshowcase/list")]
    [HttpPost("/request/game/recommend/savefavorite")]
    [NonUnify, DisplayName("game-catalog-extra")]
    public Task<OldApiResult> Catalog() =>
        Run(async ct =>
        {
            var siteId = _site.RequireSiteId();
            var path = (App.HttpContext?.Request.Path.Value ?? string.Empty).ToLowerInvariant();
            if (path.Contains("savefavorite"))
                throw Oops.Oh("#功能尚未接通#");
            if (path.Contains("winshowcase"))
                return (object)Array.Empty<object>();
            if (path.Contains("subassort"))
            {
                var rows = await _biz.Db.Queryable<SiteGameSubAssort>()
                    .Where(s => s.SiteID == siteId)
                    .Take(200)
                    .ToListAsync(ct);
                return rows;
            }

            var list = await _catalog.GetListAsync(new SiteGameListFilterInput(), ct);
            return list;
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/game/remote/avia/category"), HttpGet("/request/game/remote/avia/category")]
    [HttpPost("/request/game/remote/avia/match"), HttpGet("/request/game/remote/avia/match")]
    [HttpPost("/request/game/remote/avia/matchinfo"), HttpGet("/request/game/remote/avia/matchinfo")]
    [HttpPost("/request/game/remote/avia/result"), HttpGet("/request/game/remote/avia/result")]
    [NonUnify, DisplayName("game-remote-avia")]
    public Task<OldApiResult> Avia() =>
        Run(_ => Task.FromResult<object>(Array.Empty<object>()));

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}

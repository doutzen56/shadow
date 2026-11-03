using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>会员游戏信息：/request/game/info|setting/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-info", Order = 31)]
public class GameInfoAppService : IDynamicApiController, ITransient
{
    // 对齐旧站 1002 gamecategory 顺序（抽检无 Animal）
    private static readonly string[] CategoryOrder =
    [
        "Slot", "Live", "Sport", "Fish", "Chess", "ESport", "Lottery", "Battle", "MiniGame", "Animal"
    ];

    private readonly GameSettingService _setting;
    private readonly SiteGameCatalogService _catalog;

    public GameInfoAppService(GameSettingService setting, SiteGameCatalogService catalog)
    {
        _setting = setting;
        _catalog = catalog;
    }

    /// <summary>开放游戏列表。对齐旧 /request/game/info/gamelist。</summary>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/game/info/gamelist")]
    [HttpGet("/request/game/info/gamelist")]
    [NonUnify]
    [DisplayName("会员游戏列表")]
    public async Task<OldApiResult> Gamelist()
    {
        try
        {
            var cate = OldApiRequestParam.Read(App.HttpContext, "Category");
            var rows = string.IsNullOrWhiteSpace(cate)
                ? await _setting.GameListAsync(App.HttpContext?.RequestAborted ?? default)
                : await _setting.GameListByCategoryAsync(cate, App.HttpContext?.RequestAborted ?? default);
            var list = rows.Select(g =>
            {
                var code = GameTypeMeta.Format(g.Type);
                return new
                {
                    Type = code,
                    // 旧站 Game 字段与 Type 同为厂商码（如 KS），Name 才是展示名
                    Game = code,
                    Name = string.IsNullOrEmpty(g.Name) ? code : g.Name,
                    IsFree = 0
                };
            });
            return OldApiResult.Ok(string.Empty, OldApiShowResult.OfList(list));
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }

    /// <summary>分类。对齐旧 /request/game/info/gamecategory。</summary>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/game/info/gamecategory")]
    [HttpGet("/request/game/info/gamecategory")]
    [NonUnify]
    [DisplayName("会员游戏分类")]
    public async Task<OldApiResult> Gamecategory()
    {
        try
        {
            var rows = await _setting.GameListAsync(App.HttpContext?.RequestAborted ?? default);
            var byCat = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in rows)
            {
                if (!GameTypeMeta.TryGet(g.Type, out var meta) || meta.Categories.Length == 0)
                    continue;
                var code = GameTypeMeta.Format(g.Type);
                var name = string.IsNullOrEmpty(g.Name) ? code : g.Name;
                foreach (var c in meta.Categories)
                {
                    if (!byCat.TryGetValue(c, out var games))
                    {
                        games = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        byCat[c] = games;
                    }

                    games[code] = name;
                }
            }

            var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Slot"] = "#电子#",
                ["Live"] = "#真人#",
                ["Chess"] = "#棋牌#",
                ["Sport"] = "#体育#",
                ["Fish"] = "#捕鱼#",
                ["ESport"] = "#电竞#",
                ["Lottery"] = "#彩票#",
                ["Animal"] = "#动物#",
                ["MiniGame"] = "#小游戏#",
                ["Battle"] = "#对战#"
            };

            var ordered = CategoryOrder
                .Where(byCat.ContainsKey)
                .Concat(byCat.Keys.Where(k => !CategoryOrder.Contains(k, StringComparer.OrdinalIgnoreCase)));
            var list = ordered.Select(key => new
            {
                Type = key,
                Name = labels.TryGetValue(key, out var lab) ? lab : key,
                Game = byCat[key]
            });
            return OldApiResult.Ok(string.Empty, OldApiShowResult.OfList(list));
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }

    /// <summary>厂商简表。对齐旧 /request/game/setting/gamelist（仅 Type/Name，分页）。</summary>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/game/setting/gamelist")]
    [HttpGet("/request/game/setting/gamelist")]
    [NonUnify]
    [DisplayName("会员厂商简表")]
    public async Task<OldApiResult> SettingGamelist()
    {
        try
        {
            var rows = await _setting.GameListAsync(App.HttpContext?.RequestAborted ?? default);
            var slim = rows.Select(g =>
            {
                var code = GameTypeMeta.Format(g.Type);
                return (object)new
                {
                    Type = code,
                    Name = string.IsNullOrEmpty(g.Name) ? code : g.Name
                };
            }).ToList();
            var (page, size) = OldApiShowResult.ReadPaging(App.HttpContext);
            return OldApiResult.Ok(string.Empty, OldApiShowResult.SlicePage(slim, page, size));
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/game/info/maintain"), HttpGet("/request/game/info/maintain")]
    [NonUnify, DisplayName("game-maintain")]
    public async Task<OldApiResult> Maintain()
    {
        try
        {
            var type = OldApiFormReader.ReadInt(App.HttpContext, "Type", "type", "GameType");
            var info = await _setting.MaintainInfoAsync(type, App.HttpContext?.RequestAborted ?? default);
            return OldApiResult.Ok(string.Empty, info);
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/game/info/gameinfo"), HttpGet("/request/game/info/gameinfo")]
    [HttpPost("/request/game/info/getgameinfo"), HttpGet("/request/game/info/getgameinfo")]
    [HttpPost("/request/game/info/entrance"), HttpGet("/request/game/info/entrance")]
    [HttpPost("/request/game/info/entrancecategory"), HttpGet("/request/game/info/entrancecategory")]
    [NonUnify, DisplayName("game-info-extra")]
    public Task<OldApiResult> GameinfoExtra() => Gamelist();

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/game/recommend/list"), HttpGet("/request/game/recommend/list")]
    [HttpPost("/request/game/recommend/news"), HttpGet("/request/game/recommend/news")]
    [HttpPost("/request/game/recommend/favorites"), HttpGet("/request/game/recommend/favorites")]
    [NonUnify, DisplayName("game-recommend")]
    public async Task<OldApiResult> Recommend()
    {
        try
        {
            var path = (App.HttpContext?.Request.Path.Value ?? string.Empty).ToLowerInvariant();
            var rows = path.Contains("news")
                ? await _catalog.GetNewListAsync(App.HttpContext?.RequestAborted ?? default)
                : await _catalog.GetRecommendListAsync(App.HttpContext?.RequestAborted ?? default);
            return OldApiResult.Ok(string.Empty, OldApiShowResult.OfList(rows));
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/game/info/getnoembedded"), HttpGet("/request/game/info/getnoembedded")]
    [NonUnify, DisplayName("game-noembedded")]
    public Task<OldApiResult> Getnoembedded() =>
        Task.FromResult(OldApiResult.Ok(string.Empty, Array.Empty<object>()));
}

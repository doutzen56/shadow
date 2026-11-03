using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台游戏注单：/admin/game/log/*（旧路径 1:1）。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-log", Order = 45)]
public class GameLogAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly GameOrderSummaryService _svc;

    public GameLogAppService(GameOrderSummaryService svc) => _svc = svc;

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/list")]
    [HttpGet("/admin/game/log/list")]
    [NonUnify]
    [DisplayName("站点后台注单综合列表")]
    public Task<OldApiResult> List() => PageCategoryAsync(-1, true);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/chess")]
    [HttpGet("/admin/game/log/chess")]
    [NonUnify]
    [DisplayName("站点后台棋牌注单")]
    public Task<OldApiResult> Chess() => PageCategoryAsync(GameOrderSummaryService.CategoryChess);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/live")]
    [HttpGet("/admin/game/log/live")]
    [NonUnify]
    [DisplayName("站点后台真人注单")]
    public Task<OldApiResult> Live() => PageCategoryAsync(GameOrderSummaryService.CategoryLive);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/slot")]
    [HttpGet("/admin/game/log/slot")]
    [NonUnify]
    [DisplayName("站点后台电子注单")]
    public Task<OldApiResult> Slot() => PageCategoryAsync(GameOrderSummaryService.CategorySlot);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/fish")]
    [HttpGet("/admin/game/log/fish")]
    [NonUnify]
    [DisplayName("站点后台捕鱼注单")]
    public Task<OldApiResult> Fish() => PageCategoryAsync(GameOrderSummaryService.CategoryFish);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/sport")]
    [HttpGet("/admin/game/log/sport")]
    [NonUnify]
    [DisplayName("站点后台体育注单")]
    public Task<OldApiResult> Sport() => PageCategoryAsync(GameOrderSummaryService.CategorySport);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/esport")]
    [HttpGet("/admin/game/log/esport")]
    [NonUnify]
    [DisplayName("站点后台电竞注单")]
    public Task<OldApiResult> Esport() => PageCategoryAsync(GameOrderSummaryService.CategoryESport);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/lottery")]
    [HttpGet("/admin/game/log/lottery")]
    [NonUnify]
    [DisplayName("站点后台彩票注单")]
    public Task<OldApiResult> Lottery() => PageCategoryAsync(GameOrderSummaryService.CategoryLottery);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/animal")]
    [HttpGet("/admin/game/log/animal")]
    [NonUnify]
    [DisplayName("站点后台动物注单")]
    public Task<OldApiResult> Animal() => PageCategoryAsync(GameOrderSummaryService.CategoryAnimal);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/mini")]
    [HttpGet("/admin/game/log/mini")]
    [NonUnify]
    [DisplayName("站点后台小游戏注单")]
    public Task<OldApiResult> Mini() => PageCategoryAsync(GameOrderSummaryService.CategoryMini);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/fantasy")]
    [HttpGet("/admin/game/log/fantasy")]
    [NonUnify]
    [DisplayName("站点后台幻想注单")]
    public Task<OldApiResult> Fantasy() => PageCategoryAsync(GameOrderSummaryService.CategoryFantasy);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/reward")]
    [HttpGet("/admin/game/log/reward")]
    [NonUnify]
    [DisplayName("站点后台打赏记录")]
    public async Task<OldApiResult> Reward()
    {
        var input = ReadInput(esDetail: false);
        input.StartAt ??= DateTime.Today.AddDays(-90);
        var result = await RunAsync(async ct =>
        {
            var page = await _svc.PageRewardAsync(input, ct);
            var rows = page.Items.Select(MapRewardRow);
            var data = new
            {
                Type = 1,
                Members = page.Items.Select(t => t.UserID).Distinct().Count(),
                TradeAmount = page.Items.Sum(t => t.BetMoney)
            };
            return OldApiShowResult.OfPage(rows, page.Total, input.Page, input.PageSize, data);
        });
        return result;
    }

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/chessinfo")]
    [HttpGet("/admin/game/log/chessinfo")]
    [NonUnify]
    [DisplayName("站点后台棋牌详情")]
    public Task<OldApiResult> Chessinfo() => InfoAsync(GameOrderSummaryService.CategoryChess);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/liveinfo")]
    [HttpGet("/admin/game/log/liveinfo")]
    [NonUnify]
    [DisplayName("站点后台真人详情")]
    public Task<OldApiResult> Liveinfo() => InfoAsync(GameOrderSummaryService.CategoryLive);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/slotinfo")]
    [HttpGet("/admin/game/log/slotinfo")]
    [NonUnify]
    [DisplayName("站点后台电子详情")]
    public Task<OldApiResult> Slotinfo() => InfoAsync(GameOrderSummaryService.CategorySlot);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/fishinfo")]
    [HttpGet("/admin/game/log/fishinfo")]
    [NonUnify]
    [DisplayName("站点后台捕鱼详情")]
    public Task<OldApiResult> Fishinfo() => InfoAsync(GameOrderSummaryService.CategoryFish);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/sportinfo")]
    [HttpGet("/admin/game/log/sportinfo")]
    [NonUnify]
    [DisplayName("站点后台体育详情")]
    public Task<OldApiResult> Sportinfo() => InfoAsync(GameOrderSummaryService.CategorySport);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/esportinfo")]
    [HttpGet("/admin/game/log/esportinfo")]
    [NonUnify]
    [DisplayName("站点后台电竞详情")]
    public Task<OldApiResult> Esportinfo() => InfoAsync(GameOrderSummaryService.CategoryESport);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/lotteryinfo")]
    [HttpGet("/admin/game/log/lotteryinfo")]
    [NonUnify]
    [DisplayName("站点后台彩票详情")]
    public Task<OldApiResult> Lotteryinfo() => InfoAsync(GameOrderSummaryService.CategoryLottery);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/animalinfo")]
    [HttpGet("/admin/game/log/animalinfo")]
    [NonUnify]
    [DisplayName("站点后台动物详情")]
    public Task<OldApiResult> Animalinfo() => InfoAsync(GameOrderSummaryService.CategoryAnimal);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/miniinfo")]
    [HttpGet("/admin/game/log/miniinfo")]
    [NonUnify]
    [DisplayName("站点后台小游戏详情")]
    public Task<OldApiResult> Miniinfo() => InfoAsync(GameOrderSummaryService.CategoryMini);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/fantasyinfo")]
    [HttpGet("/admin/game/log/fantasyinfo")]
    [NonUnify]
    [DisplayName("站点后台幻想详情")]
    public Task<OldApiResult> Fantasyinfo() => InfoAsync(GameOrderSummaryService.CategoryFantasy);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/rewardinfo")]
    [HttpGet("/admin/game/log/rewardinfo")]
    [NonUnify]
    [DisplayName("站点后台打赏详情")]
    public Task<OldApiResult> Rewardinfo() => InfoAsync(-1);

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/excel")]
    [HttpGet("/admin/game/log/excel")]
    [NonUnify]
    [DisplayName("站点后台注单导出占位")]
    public async Task<OldApiResult> Excel()
    {
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.ExportStubAsync(ct);
            return OldApiShowResult.OfList(rows);
        });
        return result;
    }

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/exportlivelog")]
    [HttpGet("/admin/game/log/exportlivelog")]
    [NonUnify]
    [DisplayName("站点后台导出真人占位")]
    public Task<OldApiResult> Exportlivelog() => Excel();

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/exportanimallog")]
    [HttpGet("/admin/game/log/exportanimallog")]
    [NonUnify]
    [DisplayName("站点后台导出动物占位")]
    public Task<OldApiResult> Exportanimallog() => Excel();

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/exportrewardlog")]
    [HttpGet("/admin/game/log/exportrewardlog")]
    [NonUnify]
    [DisplayName("站点后台导出打赏占位")]
    public Task<OldApiResult> Exportrewardlog() => Excel();

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/save")]
    [HttpGet("/admin/game/log/save")]
    [NonUnify]
    [DisplayName("站点后台拉单测试占位")]
    public async Task<OldApiResult> Save()
    {
        var typeText = OldApiRequestParam.Read(App.HttpContext, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type);
        var result = await RunAsync(_ => Task.FromResult<object?>(new { count = 0, Type = GameTypeMeta.Format(type) }));
        return result;
    }

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/updatestatus")]
    [HttpGet("/admin/game/log/updatestatus")]
    [NonUnify]
    [DisplayName("站点后台注单状态占位")]
    public async Task<OldApiResult> Updatestatus()
    {
        var result = await RunAsync(_ => Task.FromResult<object?>(null));
        return result;
    }

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/log/rawdata")]
    [HttpGet("/admin/game/log/rawdata")]
    [NonUnify]
    [DisplayName("站点后台原始数据占位")]
    public async Task<OldApiResult> Rawdata()
    {
        var result = OldApiResult.Fail("#没有找到原始数据#");
        return result;
    }

    private async Task<OldApiResult> PageCategoryAsync(int category, bool readCategoryFromForm = false)
    {
        var input = ReadInput(esDetail: true);
        if (readCategoryFromForm)
        {
            var catText = OldApiRequestParam.Read(App.HttpContext, "Category");
            if (GameCategoryNameMap.TryParse(catText, out var cat) && cat >= 0)
                category = cat;
            else if (string.IsNullOrWhiteSpace(catText))
                category = -1;
        }

        var catFixed = category;
        var result = await RunAsync(async ct =>
        {
            var page = await _svc.PageByCategoryAsync(catFixed, input, ct);
            if (!string.IsNullOrEmpty(page.Message) && page.Total == 0 && (page.Items?.Count ?? 0) == 0)
                throw Oops.Oh(page.Message);
            var rows = page.Items.Select(MapDetailRow);
            var data = new
            {
                Type = 1,
                BetMoney = page.Items.Sum(t => t.BetMoney),
                BetAmount = page.Items.Sum(t => t.BetAmount),
                Money = page.Items.Sum(t => t.Money)
            };
            return OldApiShowResult.OfPage(rows, page.Total, input.Page, input.PageSize, data);
        });
        return result;
    }

    private async Task<OldApiResult> InfoAsync(int category)
    {
        var http = App.HttpContext;
        var sourceId = OldApiRequestParam.Read(http, "SourceID");
        var typeText = OldApiRequestParam.Read(http, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type);
        var result = await RunAsync(async ct =>
        {
            var info = await _svc.InfoAsync(sourceId, type, category, ct);
            if (!info.Found)
                throw Oops.Oh(string.IsNullOrEmpty(info.Message) ? "#编号错误#" : info.Message);
            return new
            {
                Type = string.IsNullOrEmpty(info.TypeName) ? GameTypeMeta.Format(info.Type) : info.TypeName,
                info.SourceID,
                info.UserID,
                info.UserName,
                info.PlayerName,
                info.BetMoney,
                info.BetAmount,
                info.Money,
                Currency = string.IsNullOrEmpty(info.CurrencyCode)
                    ? OldApiMoneyDisplay.FormatCurrency(info.Currency)
                    : info.CurrencyCode,
                info.Game,
                Status = info.Status,
                CreateAt = info.CreateAt,
                SettlementAt = info.SettlementAt,
            };
        });
        return result;
    }

    private static GameOrderSummaryPageInput ReadInput(bool esDetail)
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new GameOrderSummaryPageInput
        {
            Page = pageIndex,
            PageSize = pageSize,
            UseEsDetail = esDetail,
            SourceID = OldApiRequestParam.Read(http, "SourceID"),
            PlayerName = OldApiRequestParam.Read(http, "Player"),
            UserName = OldApiRequestParam.Read(http, "User")
                ?? OldApiRequestParam.Read(http, "UserName"),
            UserID = OldApiRequestParam.ReadInt(http, "UserID"),
        };

        var typeText = OldApiRequestParam.Read(http, "Type");
        if (!string.IsNullOrWhiteSpace(typeText))
        {
            if (GameTypeNameMap.TryParse(typeText, out var type) && type > 0)
            {
                input.Type = type;
                input.TypeName = int.TryParse(typeText.Trim(), out _)
                    ? GameTypeMeta.Format(type)
                    : typeText.Trim();
            }
            else
                input.TypeName = typeText.Trim();
        }

        var curText = OldApiRequestParam.Read(http, "Currency");
        if (!string.IsNullOrWhiteSpace(curText))
        {
            if (int.TryParse(curText, out var cur))
            {
                input.Currency = cur;
                input.CurrencyCode = OldApiMoneyDisplay.FormatCurrency(cur);
            }
            else
            {
                input.CurrencyCode = curText.Trim();
            }
        }

        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var start))
            input.StartAt = start;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var end))
            input.EndAt = end;
        // FromAt/ToAt 旧站筛 SettlementAt；当前 ES 壳按 CreateAt 窗口，FromAt 有值时仍作 StartAt 下限
        if (!input.StartAt.HasValue && DateTime.TryParse(OldApiRequestParam.Read(http, "FromAt"), out var from))
            input.StartAt = from;
        if (!input.EndAt.HasValue && DateTime.TryParse(OldApiRequestParam.Read(http, "ToAt"), out var to))
            input.EndAt = to;

        return input;
    }

    private static object MapDetailRow(GameOrderSummaryItemDto t)
    {
        var typeText = string.IsNullOrEmpty(t.TypeName) ? GameTypeMeta.Format(t.Type) : t.TypeName;
        return new
        {
            Type = typeText,
            Game = string.IsNullOrEmpty(t.Game) ? typeText : t.Game,
            t.SourceID,
            Category = GameCategoryNameMap.Format(t.Category),
            t.UserID,
            t.UserName,
            t.PlayerName,
            t.BetMoney,
            t.BetAmount,
            t.Money,
            Status = t.Status,
            CreateAt = t.Date == default ? t.SettlementAt : t.Date,
            StartAt = t.Date == default ? t.SettlementAt : t.Date,
            SettlementAt = t.SettlementAt,
            Currency = string.IsNullOrEmpty(t.CurrencyCode)
                ? OldApiMoneyDisplay.FormatCurrency(t.Currency)
                : t.CurrencyCode,
        };
    }

    private static object MapRewardRow(GameOrderSummaryItemDto t) => new
    {
        ID = 0,
        Type = string.IsNullOrEmpty(t.TypeName) ? GameTypeMeta.Format(t.Type) : t.TypeName,
        Game = t.Game,
        t.UserID,
        t.UserName,
        t.PlayerName,
        TradeAmount = t.BetMoney,
        t.SourceID,
        Currency = string.IsNullOrEmpty(t.CurrencyCode)
            ? OldApiMoneyDisplay.FormatCurrency(t.Currency)
            : t.CurrencyCode,
        CreateAt = t.Date,
    };

    private static async Task<OldApiResult> RunAsync(Func<CancellationToken, Task<object?>> action)
    {
        try
        {
            return OldApiResult.Ok(string.Empty, await action(App.HttpContext?.RequestAborted ?? default));
        }
        catch (AppFriendlyException ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }
}

using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台厂商设定：/admin/game/setting/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-setting", Order = 42)]
public class GameSettingFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly GameSettingService _svc;

    public GameSettingFormAppService(GameSettingService svc) => _svc = svc;

    /// <summary>厂商详情。对应旧 info。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/info")]
    [HttpGet("/admin/game/setting/info")]
    [NonUnify]
    [DisplayName("站点后台厂商详情")]
    public async Task<OldApiResult> Info()
    {
        var typeText = OldApiRequestParam.Read(App.HttpContext, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type);
        var result = await RunAsync(async ct =>
        {
            var d = await _svc.DetailAsync(type, ct);
            return new
            {
                Type = GameTypeMeta.Format(d.Type),
                Game = d.Game,
                d.Name,
                IsOpen = d.IsOpen ? 1 : 0,
                IsSelf = d.IsSelf ? 1 : 0,
                IsWater = d.IsWater ? 1 : 0,
                d.MinTransfer,
                d.MaxTransfer,
                d.Status,
                Setting = d.Setting,
            };
        });
        return result;
    }

    /// <summary>维护信息。对应旧 maintaininfo。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/maintaininfo")]
    [HttpGet("/admin/game/setting/maintaininfo")]
    [NonUnify]
    [DisplayName("站点后台维护信息")]
    public async Task<OldApiResult> Maintaininfo()
    {
        var typeText = OldApiRequestParam.Read(App.HttpContext, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type);
        var result = await RunAsync(async ct =>
        {
            var d = await _svc.MaintainInfoAsync(type, ct);
            return new
            {
                Type = GameTypeMeta.Format(d.Type),
                d.Name,
                d.Maintenance,
                d.GameCategorys,
                d.Status,
                d.RemainTime,
                d.RemainTimeSpan,
            };
        });
        return result;
    }

    /// <summary>按分类开通表。对应旧 gamelistbycategory。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/gamelistbycategory")]
    [HttpGet("/admin/game/setting/gamelistbycategory")]
    [NonUnify]
    [DisplayName("站点后台分类厂商表")]
    public async Task<OldApiResult> Gamelistbycategory()
    {
        var category = OldApiRequestParam.Read(App.HttpContext, "Category");
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.GameListByCategoryAsync(category, ct);
            return OldApiShowResult.OfList(rows.Select(g => new
            {
                IsOpen = g.IsOpen ? 1 : 0,
                Name = string.IsNullOrEmpty(g.Name) ? GameTypeMeta.Format(g.Type) : g.Name,
                Type = GameTypeMeta.Format(g.Type),
                Game = string.IsNullOrEmpty(g.Game) ? GameTypeMeta.Format(g.Type) : g.Game
            }));
        });
        return result;
    }

    /// <summary>厂商简表。对应旧 gamelist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/gamelist")]
    [HttpGet("/admin/game/setting/gamelist")]
    [NonUnify]
    [DisplayName("站点后台厂商简表")]
    public async Task<OldApiResult> Gamelist()
    {
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.GameListForOldApiAsync(ct);
            return OldApiShowResult.OfList(rows);
        });
        return result;
    }

    /// <summary>开通下拉。对应旧 gameopenlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/gameopenlist")]
    [HttpGet("/admin/game/setting/gameopenlist")]
    [NonUnify]
    [DisplayName("站点后台开通游戏下拉")]
    public async Task<OldApiResult> Gameopenlist()
    {
        var cate = OldApiRequestParam.Read(App.HttpContext, "Cate")
            ?? OldApiRequestParam.Read(App.HttpContext, "Type");
        var result = await RunAsync(async ct => (object?)await _svc.GameOpenListForOldApiAsync(cate, ct));
        return result;
    }

    /// <summary>币种。对应旧 gamecurrencylist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/gamecurrencylist")]
    [HttpGet("/admin/game/setting/gamecurrencylist")]
    [NonUnify]
    [DisplayName("站点后台游戏币种")]
    public async Task<OldApiResult> Gamecurrencylist()
    {
        var typeText = OldApiRequestParam.Read(App.HttpContext, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type);
        if (type <= 0)
            _ = int.TryParse(typeText, out type);
        var result = await RunAsync(async ct => (object?)await _svc.GameCurrencyListAsync(type, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/gamesort")]
    [NonUnify, DisplayName("站点后台厂商排序")]
    public async Task<OldApiResult> Gamesort()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.GameSortAsync(OldApiFormReader.ReadString(App.HttpContext, "Type", "Types", "ID"), ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/gametag"), HttpGet("/admin/game/setting/gametag")]
    [NonUnify, DisplayName("站点后台游戏标签")]
    public async Task<OldApiResult> Gametag()
    {
        var typeText = OldApiRequestParam.Read(App.HttpContext, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type);
        if (type <= 0) _ = int.TryParse(typeText, out type);
        var result = await RunAsync(async ct => await _svc.GameTagAsync(type, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/setgametag")]
    [NonUnify, DisplayName("站点后台保存游戏标签")]
    public async Task<OldApiResult> Setgametag()
    {
        var http = App.HttpContext;
        var typeText = OldApiRequestParam.Read(http, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type);
        if (type <= 0) _ = int.TryParse(typeText, out type);
        var result = await RunAsync(async ct =>
        {
            await _svc.SetGameTagAsync(new GameTagSettingDto
            {
                Type = type,
                Label = OldApiFormReader.ReadString(http, "Label", "Tag") ?? string.Empty,
                Logo = OldApiRequestParam.Read(http, "Logo") ?? string.Empty,
                Backdrop = OldApiRequestParam.Read(http, "Backdrop") ?? string.Empty,
                IsBackdrop = OldApiRequestParam.Read(http, "IsBackdrop") is "1" or "true" or "True"
            }, ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/sitegamecate"), HttpGet("/admin/game/setting/sitegamecate")]
    [NonUnify, DisplayName("站点后台游戏分类名")]
    public async Task<OldApiResult> Sitegamecate()
    {
        var result = await RunAsync(async ct => await _svc.SiteGameCateNamesAsync(ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/savemaintain")]
    [NonUnify, DisplayName("站点后台保存维护")]
    public async Task<OldApiResult> Savemaintain()
    {
        var http = App.HttpContext;
        var typeText = OldApiRequestParam.Read(http, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type);
        if (type <= 0) _ = int.TryParse(typeText, out type);
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveMaintainAsync(new GameMaintainSaveInput
            {
                Type = type,
                Status = OldApiRequestParam.ReadInt(http, "Status"),
                Maintenance = OldApiRequestParam.Read(http, "Maintenance") ?? string.Empty
            }, ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/save")]
    [NonUnify, DisplayName("站点后台保存厂商设定")]
    public async Task<OldApiResult> Save()
    {
        var http = App.HttpContext;
        var gameText = OldApiFormReader.ReadString(http, "Game", "Type");
        _ = GameTypeNameMap.TryParse(gameText, out var game);
        if (game <= 0) _ = int.TryParse(gameText, out game);
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveAsync(new GameSettingSaveInput
            {
                Game = game,
                Name = OldApiRequestParam.Read(http, "Name") ?? string.Empty,
                IsWater = OldApiRequestParam.Read(http, "IsWater") is "1" or "true" or "True",
                MinTransfer = OldApiRequestParam.ReadInt(http, "MinTransfer"),
                MaxTransfer = OldApiRequestParam.ReadInt(http, "MaxTransfer"),
                Setting = OldApiRequestParam.Read(http, "Setting") ?? string.Empty
            }, ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/washflow"), HttpGet("/admin/game/setting/washflow")]
    [NonUnify, DisplayName("站点后台流水比例")]
    public async Task<OldApiResult> Washflow()
    {
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.WashFlowAsync(ct);
            return OldApiShowResult.OfList(rows);
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/savewashflow")]
    [NonUnify, DisplayName("站点后台保存流水比例")]
    public async Task<OldApiResult> Savewashflow()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveWashFlowAsync(ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/setting/gameinit"), HttpGet("/admin/game/setting/gameinit")]
    [NonUnify, DisplayName("站点后台游戏预设初始化")]
    public async Task<OldApiResult> Gameinit()
    {
        var result = await RunAsync(async ct => await _svc.GameInitAsync(ct));
        return result;
    }

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

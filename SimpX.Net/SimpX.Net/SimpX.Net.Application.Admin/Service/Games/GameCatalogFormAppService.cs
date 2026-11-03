using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台游戏分类/列表兼容路由：/admin/game/cate|listing|info/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-catalog", Order = 40)]
public class GameCatalogFormAppService : IDynamicApiController, ITransient
{
    /// <summary>先挂会员列表权限（本环境 tzen001 可过）；权限表收窄另开刀。</summary>
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteGameCatalogService _svc;

    /// <summary>注入。</summary>
    public GameCatalogFormAppService(SiteGameCatalogService svc) => _svc = svc;

    /// <summary>分类列表。对应旧 /admin/game/cate/getcatelist（旧站直接吐数组）。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/cate/getcatelist")]
    [HttpGet("/admin/game/cate/getcatelist")]
    [NonUnify]
    [DisplayName("站点后台游戏分类列表")]
    public async Task<IActionResult> Getcatelist()
    {
        try
        {
            var rows = await _svc.ListCatesAsync(App.HttpContext?.RequestAborted ?? default);
            var json = System.Text.Json.JsonSerializer.Serialize(rows, OldApiContract.JsonOptions);
            return new ContentResult
            {
                Content = json,
                ContentType = "application/json; charset=utf-8",
                StatusCode = 200
            };
        }
        catch (AppFriendlyException ex)
        {
            return new JsonResult(OldApiResult.Fail(ex.Message), OldApiContract.JsonOptions)
            { ContentType = "application/json; charset=utf-8" };
        }
        catch (Exception ex)
        {
            return new JsonResult(OldApiResult.Fail(ex.Message), OldApiContract.JsonOptions)
            { ContentType = "application/json; charset=utf-8" };
        }
    }

    /// <summary>分类树。对应旧 /admin/game/listing/getcatetree。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/getcatetree")]
    [HttpGet("/admin/game/listing/getcatetree")]
    [NonUnify]
    [DisplayName("站点后台游戏分类树")]
    public async Task<OldApiResult> Getcatetree()
    {
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.GetCateTreeAsync(ct);
            return new { data = rows };
        });
        return result;
    }

    /// <summary>游戏列表。对应旧 /admin/game/listing/getlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/getlist")]
    [HttpGet("/admin/game/listing/getlist")]
    [NonUnify]
    [DisplayName("站点后台游戏列表")]
    public async Task<OldApiResult> Getlist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new SiteGameListFilterInput
        {
            // 缺参保持 null/空：旧站 QF("select")!="0" 时按空 GameCode 过滤 → RC=0
            Select = OldApiRequestParam.Read(http, "select") ?? OldApiRequestParam.Read(http, "Select"),
            Name = OldApiRequestParam.Read(http, "Name"),
            OpenType = OldApiRequestParam.Read(http, "OpenType"),
        };
        var typeText = OldApiRequestParam.Read(http, "Type");
        if (GameTypeNameMap.TryParse(typeText, out var type) && type > 0)
            input.Type = type;
        var catText = OldApiRequestParam.Read(http, "Category");
        if (GameCategoryNameMap.TryParse(catText, out var cat) && cat >= 0)
            input.Category = cat;
        if (int.TryParse(OldApiRequestParam.Read(http, "IsEnable"), out var en))
            input.IsEnable = en == 1;
        if (int.TryParse(OldApiRequestParam.Read(http, "RecommendEnable"), out var re))
            input.RecommendEnable = re == 1;
        if (int.TryParse(OldApiRequestParam.Read(http, "NewEnable"), out var ne))
            input.NewEnable = ne == 1;
        if (int.TryParse(OldApiRequestParam.Read(http, "CurStatus"), out var cur))
            input.CurStatus = cur == 1;

        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.GetListAsync(input, ct);
            var mapped = rows.Select(MapListRow);
            return OldApiShowResult.SlicePage(mapped, pageIndex, pageSize);
        });
        return result;
    }

    private static object MapListRow(SiteGameListDetailDto t) => new
    {
        t.ID,
        t.GameName,
        t.Name,
        t.Sort,
        t.Recommend,
        IsEnable = t.IsEnable ? 1 : 0,
        PcEnable = t.PcEnable ? 1 : 0,
        AppEnable = t.AppEnable ? 1 : 0,
        RecommendEnable = t.RecommendEnable ? 1 : 0,
        Type = GameTypeMeta.Format(t.Type),
        t.Code,
        Category = GameCategoryNameMap.Format(t.Category),
        NewEnable = t.NewEnable ? 1 : 0,
        t.New,
        Backdrop = FormatBackdrop(t.ShowImg),
    };

    private static string FormatBackdrop(string? cover)
    {
        if (string.IsNullOrEmpty(cover))
            return "/images/space.png";
        if (cover.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return cover;
        return cover.StartsWith('/') ? cover : "/" + cover;
    }

    /// <summary>列表详情。对应旧 gamelistinfo。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/gamelistinfo")]
    [HttpGet("/admin/game/listing/gamelistinfo")]
    [NonUnify]
    [DisplayName("站点后台游戏列表详情")]
    public async Task<OldApiResult> Gamelistinfo()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct =>
        {
            var t = await _svc.GameListInfoAsync(id, ct);
            return new
            {
                t.ID,
                t.GameName,
                t.GameCode,
                t.Name,
                Type = GameTypeMeta.Format(t.Type),
                Category = GameCategoryNameMap.Format(t.Category),
                t.Code,
                IsEnable = t.IsEnable ? 1 : 0,
                PcEnable = t.PcEnable ? 1 : 0,
                AppEnable = t.AppEnable ? 1 : 0,
                RecommendEnable = t.RecommendEnable ? 1 : 0,
                BackdShowImg = FormatBackdrop(t.ShowImg),
                t.ShowImg,
                t.Sort,
                t.Recommend,
                NewEnable = t.NewEnable ? 1 : 0,
                t.New,
                t.Remark,
            };
        });
        return result;
    }

    /// <summary>推荐列表。对应旧 getrecommendlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/getrecommendlist")]
    [HttpGet("/admin/game/listing/getrecommendlist")]
    [NonUnify]
    [DisplayName("站点后台推荐游戏")]
    public async Task<OldApiResult> Getrecommendlist()
    {
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(App.HttpContext);
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.GetRecommendListAsync(ct);
            var mapped = rows.Select(t => new
            {
                t.ID,
                t.GameName,
                t.Name,
                t.Recommend,
                RecommendEnable = t.RecommendEnable ? 1 : 0
            });
            return OldApiShowResult.SlicePage(mapped, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>最新列表。对应旧 getnewlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/getnewlist")]
    [HttpGet("/admin/game/listing/getnewlist")]
    [NonUnify]
    [DisplayName("站点后台最新游戏")]
    public async Task<OldApiResult> Getnewlist()
    {
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(App.HttpContext);
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.GetNewListAsync(ct);
            var mapped = rows.Select(t => new
            {
                t.ID,
                t.GameName,
                t.Name,
                t.New,
                NewEnable = t.NewEnable ? 1 : 0
            });
            return OldApiShowResult.SlicePage(mapped, pageIndex, pageSize);
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/savegamelist")]
    [NonUnify, DisplayName("站点后台保存游戏列表")]
    public async Task<OldApiResult> Savegamelist()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveGameListAsync(new SiteGameListDetailSaveInput
            {
                ID = OldApiRequestParam.ReadInt(http, "ID"),
                Name = OldApiRequestParam.Read(http, "Name"),
                Sort = OldApiRequestParam.ReadInt(http, "Sort"),
                IsEnable = OldApiRequestParam.Read(http, "IsEnable") is "1" or "true" or "True",
                PcEnable = OldApiRequestParam.Read(http, "PcEnable") is "1" or "true" or "True",
                AppEnable = OldApiRequestParam.Read(http, "AppEnable") is "1" or "true" or "True",
                ShowImg = OldApiRequestParam.Read(http, "ShowImg"),
                Remark = OldApiRequestParam.Read(http, "Remark")
            }, ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/updatesort")]
    [NonUnify, DisplayName("站点后台游戏置顶")]
    public async Task<OldApiResult> Updatesort()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateSortAsync(
                OldApiRequestParam.ReadInt(http, "ID"),
                OldApiRequestParam.Read(http, "IsTop") is "1" or "true" or "True",
                ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/saverecommendlist")]
    [NonUnify, DisplayName("站点后台保存推荐")]
    public async Task<OldApiResult> Saverecommendlist()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveRecommendListAsync(OldApiFormReader.ReadString(App.HttpContext, "Recommend", "Recommends"), ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/savenewlist")]
    [NonUnify, DisplayName("站点后台保存最新")]
    public async Task<OldApiResult> Savenewlist()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveNewListAsync(OldApiFormReader.ReadString(App.HttpContext, "New", "News"), ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/creategamelist")]
    [NonUnify, DisplayName("站点后台创建游戏列表")]
    public async Task<OldApiResult> Creategamelist()
    {
        var typeText = OldApiRequestParam.Read(App.HttpContext, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type);
        if (type <= 0) _ = int.TryParse(typeText, out type);
        var result = await RunAsync(async ct => await _svc.CreateGameListAsync(type, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/creategamelistall")]
    [NonUnify, DisplayName("站点后台创建全部游戏列表")]
    public async Task<OldApiResult> Creategamelistall()
    {
        var result = await RunAsync(async ct => await _svc.CreateGameListAllAsync(ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/listing/refreshcache")]
    [NonUnify, DisplayName("站点后台刷新游戏缓存")]
    public async Task<OldApiResult> Refreshcache()
    {
        var result = await RunAsync(async ct =>
        {
            _ = ct;
            return await _svc.RefreshCacheAsync();
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/cate/info"), HttpGet("/admin/game/cate/info")]
    [NonUnify, DisplayName("站点后台游戏分类详情")]
    public async Task<OldApiResult> Cateinfo()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct => await _svc.CateInfoAsync(id, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/cate/save")]
    [NonUnify, DisplayName("站点后台保存游戏分类")]
    public async Task<OldApiResult> Catesave()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveCateDetailAsync(
                OldApiRequestParam.ReadInt(http, "ID"),
                OldApiRequestParam.ReadInt(http, "Sort"),
                OldApiRequestParam.Read(http, "Remark"),
                ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/cate/creategamecate")]
    [NonUnify, DisplayName("站点后台创建游戏分类")]
    public async Task<OldApiResult> Creategamecate()
    {
        var result = await RunAsync(async ct => await _svc.CreateGameCateAsync(ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/cate/updatestatus")]
    [NonUnify, DisplayName("站点后台分类开关")]
    public async Task<OldApiResult> Cateupdatestatus()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateCateStatusAsync(
                OldApiRequestParam.ReadInt(http, "ID"),
                OldApiRequestParam.ReadInt(http, "Type"),
                OldApiRequestParam.Read(http, "Value") is "1" or "true" or "True",
                ct);
            return null;
        });
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

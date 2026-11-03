using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台游戏信息：/admin/game/info/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-info", Order = 41)]
public class GameInfoAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly GameSettingService _svc;

    public GameInfoAppService(GameSettingService svc) => _svc = svc;

    /// <summary>厂商简表。对应旧 get。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/info/get")]
    [HttpGet("/admin/game/info/get")]
    [NonUnify]
    [DisplayName("站点后台游戏信息列表")]
    public async Task<OldApiResult> Get()
    {
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.InfoGetForOldApiAsync(ct);
            return OldApiShowResult.OfList(rows);
        });
        return result;
    }

    /// <summary>按分类。对应旧 category。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/info/category")]
    [HttpGet("/admin/game/info/category")]
    [NonUnify]
    [DisplayName("站点后台游戏分类分组")]
    public async Task<OldApiResult> Category()
    {
        var result = await RunAsync(async ct => (object?)await _svc.CategoryForOldApiAsync(ct));
        return result;
    }

    /// <summary>游戏类型下拉。对应旧 gamecategory。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/info/gamecategory")]
    [HttpGet("/admin/game/info/gamecategory")]
    [NonUnify]
    [DisplayName("站点后台游戏类型下拉")]
    public async Task<OldApiResult> Gamecategory()
    {
        // 旧站 info 直接是数组
        var result = await RunAsync(async ct => (object?)await _svc.GameCategoryForOldApiAsync(ct));
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

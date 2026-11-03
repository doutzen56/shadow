using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台游戏会员：/admin/game/user/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-user", Order = 43)]
public class GameUserAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly GameUserQueryService _svc;

    public GameUserAppService(GameUserQueryService svc) => _svc = svc;

    /// <summary>游戏会员列表。对应旧 userlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/user/userlist")]
    [HttpGet("/admin/game/user/userlist")]
    [NonUnify]
    [DisplayName("站点后台游戏会员列表")]
    public async Task<OldApiResult> Userlist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new GameUserListInput
        {
            Page = pageIndex,
            PageSize = pageSize,
            UserID = OldApiRequestParam.ReadInt(http, "UserID"),
            User = OldApiRequestParam.Read(http, "User") ?? OldApiRequestParam.Read(http, "UserName"),
            GameUser = OldApiRequestParam.Read(http, "GameUser"),
        };
        var typeText = OldApiRequestParam.Read(http, "Type");
        if (GameTypeNameMap.TryParse(typeText, out var type) || int.TryParse(typeText, out type))
            input.Type = type;
        if (int.TryParse(OldApiRequestParam.Read(http, "Currency"), out var cur))
            input.Currency = cur;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var start))
            input.StartAt = start;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var end))
            input.EndAt = end;

        var result = await RunAsync(async ct =>
        {
            var page = await _svc.UserListAsync(input, ct);
            var rows = page.Items.Select(t => new
            {
                t.UserID,
                Type = GameTypeMeta.Format(t.Game),
                Game = GameTypeMeta.Format(t.Game),
                t.UserName,
                t.PlayerName,
                t.PlayerPass,
                t.Withdraw,
                t.Balance,
                t.CreateAt,
                t.UpdateAt,
                Lock = new Dictionary<string, object>(),
                IsTest = t.IsTest,
                Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
                t.Limits,
                t.LimitType,
            });
            return OldApiShowResult.OfPage(rows, page.Total, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>读余额。对应旧 getbalance。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/user/getbalance")]
    [HttpGet("/admin/game/user/getbalance")]
    [NonUnify]
    [DisplayName("站点后台游戏余额")]
    public async Task<OldApiResult> Getbalance()
    {
        var http = App.HttpContext;
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        var typeText = OldApiRequestParam.Read(http, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type) || int.TryParse(typeText, out type);
        var currency = OldApiRequestParam.ReadInt(http, "Currency");
        var result = await RunAsync(async ct => await _svc.GetBalanceAsync(userId, type, currency, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/user/getuserbalancelist"), HttpGet("/admin/game/user/getuserbalancelist")]
    [NonUnify, DisplayName("站点后台会员游戏余额列表")]
    public async Task<OldApiResult> Getuserbalancelist()
    {
        var http = App.HttpContext;
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        var curRaw = OldApiRequestParam.Read(http, "Currency");
        var currency = string.IsNullOrWhiteSpace(curRaw) ? -1 : OldApiRequestParam.ReadInt(http, "Currency");
        var result = await RunAsync(async ct => await _svc.GetUserBalanceListAsync(userId, currency, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/user/updatelock")]
    [NonUnify, DisplayName("站点后台游戏锁")]
    public async Task<OldApiResult> Updatelock()
    {
        var http = App.HttpContext;
        var typeText = OldApiRequestParam.Read(http, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type) || int.TryParse(typeText, out type);
        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateLockAsync(new GameUserLockInput
            {
                UserID = OldApiRequestParam.ReadInt(http, "UserID"),
                Type = type,
                Currency = OldApiRequestParam.ReadInt(http, "Currency"),
                Name = OldApiFormReader.ReadInt(http, "Name", "Lock"),
                IsLock = OldApiRequestParam.Read(http, "IsLock") is "1" or "true" or "True"
            }, ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/user/updatetest")]
    [NonUnify, DisplayName("站点后台游戏测试标记")]
    public async Task<OldApiResult> Updatetest()
    {
        var http = App.HttpContext;
        var typeText = OldApiRequestParam.Read(http, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type) || int.TryParse(typeText, out type);
        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateTestAsync(
                OldApiRequestParam.ReadInt(http, "UserID"),
                type,
                OldApiRequestParam.Read(http, "IsTest") is not ("0" or "false" or "False"),
                ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/user/updatelimit")]
    [NonUnify, DisplayName("站点后台游戏限红")]
    public async Task<OldApiResult> Updatelimit()
    {
        var http = App.HttpContext;
        var typeText = OldApiRequestParam.Read(http, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type) || int.TryParse(typeText, out type);
        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateLimitAsync(new GameUserLimitInput
            {
                UserID = OldApiRequestParam.ReadInt(http, "UserID"),
                Type = type,
                Currency = OldApiRequestParam.ReadInt(http, "Currency"),
                LimitType = OldApiRequestParam.ReadInt(http, "LimitType"),
                Limits = OldApiRequestParam.Read(http, "Limits") ?? string.Empty
            }, ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/user/register")]
    [NonUnify, DisplayName("站点后台注册游戏帐号")]
    public async Task<OldApiResult> Register()
    {
        var http = App.HttpContext;
        var typeText = OldApiRequestParam.Read(http, "Type");
        _ = GameTypeNameMap.TryParse(typeText, out var type) || int.TryParse(typeText, out type);
        var result = await RunAsync(async ct =>
        {
            await _svc.RegisterAsync(new GameUserRegisterInput
            {
                UserID = OldApiRequestParam.ReadInt(http, "UserID"),
                Type = type,
                Currency = OldApiRequestParam.ReadInt(http, "Currency")
            }, ct);
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

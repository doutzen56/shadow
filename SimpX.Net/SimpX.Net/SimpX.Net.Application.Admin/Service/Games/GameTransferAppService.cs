using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台游戏转账：/admin/game/transfer/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-transfer", Order = 44)]
public class GameTransferAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly GameUserQueryService _svc;

    public GameTransferAppService(GameUserQueryService svc) => _svc = svc;

    /// <summary>转账订单列表。对应旧 list。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/transfer/list")]
    [HttpGet("/admin/game/transfer/list")]
    [NonUnify]
    [DisplayName("站点后台游戏转账列表")]
    public async Task<OldApiResult> List()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new GameTransferPageInput
        {
            Page = pageIndex,
            PageSize = pageSize,
            OrderID = OldApiRequestParam.ReadInt(http, "OrderID"),
            User = OldApiRequestParam.Read(http, "User"),
        };
        var typeText = OldApiRequestParam.Read(http, "Type");
        if (GameTypeNameMap.TryParse(typeText, out var type) && type > 0)
            input.Type = type;
        if (int.TryParse(OldApiRequestParam.Read(http, "Currency"), out var cur))
            input.Currency = cur;
        else
        {
            var curText = OldApiRequestParam.Read(http, "Currency");
            // 兼容币种代码；未知则保持 -1
            _ = curText;
        }
        var statusText = OldApiRequestParam.Read(http, "Status");
        if (!string.IsNullOrWhiteSpace(statusText))
        {
            if (int.TryParse(statusText, out var st))
                input.Status = st;
            else
                input.Status = statusText.Trim().ToUpperInvariant() switch
                {
                    "NONE" => 0,
                    "FAILED" => 1,
                    "SUCCESS" => 2,
                    "PROCESSING" => 3,
                    _ => -1
                };
        }
        var actionText = OldApiRequestParam.Read(http, "Action");
        if (!string.IsNullOrWhiteSpace(actionText))
        {
            if (int.TryParse(actionText, out var act))
                input.Action = act;
            else
                input.Action = actionText.Trim().ToUpperInvariant() switch
                {
                    "IN" => 1,
                    "OUT" => 2,
                    _ => 0
                };
        }
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var start))
            input.StartAt = start;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var end))
            input.EndAt = end;

        var result = await RunAsync(async ct =>
        {
            var page = await _svc.PageTransfersAsync(input, ct);
            var rows = page.Items.Select(t => new
            {
                t.ID,
                t.UserID,
                t.UserName,
                Type = GameTypeMeta.Format(t.Type),
                Game = GameTypeMeta.Format(t.Type),
                t.Money,
                Status = OldApiMoneyDisplay.FormatCheckStatus(t.Status),
                Action = FormatAction(t.Action),
                t.CreateAt,
                t.CheckAt,
                Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
            });
            var moneySum = page.Items.Sum(t => t.Money);
            return OldApiShowResult.OfPage(rows, page.Total, pageIndex, pageSize, new { Money = moneySum });
        });
        return result;
    }

    /// <summary>转账详情。对应旧 info。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/transfer/info")]
    [HttpGet("/admin/game/transfer/info")]
    [NonUnify]
    [DisplayName("站点后台游戏转账详情")]
    public async Task<OldApiResult> Info()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct =>
        {
            var t = await _svc.TransferInfoAsync(id, ct);
            return new
            {
                t.ID,
                t.UserID,
                t.UserName,
                Type = GameTypeMeta.Format(t.Type),
                Game = GameTypeMeta.Format(t.Type),
                t.Money,
                Status = OldApiMoneyDisplay.FormatCheckStatus(t.Status),
                Action = FormatAction(t.Action),
                t.CreateAt,
                t.CheckAt,
                Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
            };
        });
        return result;
    }

    private static string FormatAction(int action) => action switch
    {
        1 => "IN",
        2 => "OUT",
        _ => action.ToString()
    };

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/transfer/action")]
    [NonUnify, DisplayName("站点后台游戏转账动作")]
    public Task<OldApiResult> Action() =>
        Task.FromResult(OldApiResult.Fail("#功能尚未接通#"));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/transfer/onetransferout")]
    [NonUnify, DisplayName("站点后台一键转出")]
    public Task<OldApiResult> Onetransferout() =>
        Task.FromResult(OldApiResult.Fail("#功能尚未接通#"));

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

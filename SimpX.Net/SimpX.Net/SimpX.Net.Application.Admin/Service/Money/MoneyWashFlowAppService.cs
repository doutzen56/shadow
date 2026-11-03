using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台打码流水：/admin/money/washflow/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money-washflow", Order = 37)]
public class MoneyWashFlowAppService : IDynamicApiController, ITransient
{
    /// <summary>提现订单权限（旧 washflow 挂在提现域附近，先用订单权限）。</summary>
    public const string PermissionId = "0a83144524de42b099cf8fd5da04ef54";

    private readonly WashFlowAdminService _svc;

    public MoneyWashFlowAppService(WashFlowAdminService svc) => _svc = svc;

    /// <summary>打码清单。对应旧 list。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/washflow/list")]
    [HttpGet("/admin/money/washflow/list")]
    [NonUnify]
    [DisplayName("站点后台打码清单")]
    public async Task<OldApiResult> List()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        var currencyText = OldApiRequestParam.Read(http, "Currency");
        if (string.IsNullOrWhiteSpace(currencyText))
            return OldApiResult.Fail("#货币不能为空#");
        _ = int.TryParse(currencyText, out var currency);
        // 也接受币种名
        if (currency <= 0 && string.Equals(currencyText, "TWD", StringComparison.OrdinalIgnoreCase))
            currency = 2;

        DateTime? start = DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var s) ? s : null;
        DateTime? end = DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var e) ? e : null;
        var orderId = OldApiRequestParam.ReadInt(http, "OrderID");
        var showOpen = !string.IsNullOrEmpty(OldApiRequestParam.Read(http, "Show"));

        var result = await RunAsync(async ct =>
        {
            var page = await _svc.ListAsync(userId, currency, orderId, showOpen, start, end, ct);
            return OldApiShowResult.SlicePage(page.Items, pageIndex, pageSize, data: page.Total);
        });
        return result;
    }

    /// <summary>流水日志。对应旧 log。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/washflow/log")]
    [HttpGet("/admin/money/washflow/log")]
    [NonUnify]
    [DisplayName("站点后台打码日志")]
    public async Task<OldApiResult> Log()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        var washId = OldApiRequestParam.ReadInt(http, "id");
        if (washId <= 0)
            washId = OldApiRequestParam.ReadInt(http, "ID");
        var currencyText = OldApiRequestParam.Read(http, "Currency");
        if (string.IsNullOrWhiteSpace(currencyText))
            return OldApiResult.Fail("#货币不能为空#");
        _ = int.TryParse(currencyText, out var currency);
        if (currency <= 0 && string.Equals(currencyText, "TWD", StringComparison.OrdinalIgnoreCase))
            currency = 2;

        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.LogAsync(userId, washId, currency, ct);
            return OldApiShowResult.SlicePage(rows, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>手动增减打码。对应旧 updatewashflow。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/washflow/updatewashflow")]
    [NonUnify]
    [DisplayName("站点后台更新打码")]
    public async Task<OldApiResult> Updatewashflow()
    {
        var http = App.HttpContext;
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "Money"), out var money);
        var type = OldApiRequestParam.Read(http, "Type") ?? "Bet";
        var description = OldApiRequestParam.Read(http, "Description");
        var currencyText = OldApiRequestParam.Read(http, "Currency");
        _ = int.TryParse(currencyText, out var currency);
        if (currency <= 0 && string.Equals(currencyText, "TWD", StringComparison.OrdinalIgnoreCase))
            currency = 2;

        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateAsync(userId, money, type, description, currency, 0, ct);
            return null;
        });
        return result;
    }

    /// <summary>按流水编号更新。对应旧 updatewashflowsingle。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/washflow/updatewashflowsingle")]
    [NonUnify]
    [DisplayName("站点后台按编号更新打码")]
    public async Task<OldApiResult> Updatewashflowsingle()
    {
        var http = App.HttpContext;
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        var washId = OldApiRequestParam.ReadInt(http, "ID");
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "Money"), out var money);
        var type = OldApiRequestParam.Read(http, "Type") ?? "BetID";
        var description = OldApiRequestParam.Read(http, "Description");
        var currencyText = OldApiRequestParam.Read(http, "Currency");
        _ = int.TryParse(currencyText, out var currency);
        if (currency <= 0 && string.Equals(currencyText, "TWD", StringComparison.OrdinalIgnoreCase))
            currency = 2;

        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateAsync(userId, money, type, description, currency, washId, ct);
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

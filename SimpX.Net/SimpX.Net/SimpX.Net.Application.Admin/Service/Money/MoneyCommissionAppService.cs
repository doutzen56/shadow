using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台佣金：/admin/money/commission/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money-commission", Order = 34)]
public class MoneyCommissionAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = "84025bef58444abd9bed68b583c6228c";

    private readonly CommissionPayoutService _svc;

    public MoneyCommissionAppService(CommissionPayoutService svc) => _svc = svc;

    /// <summary>佣金日志列表。对应旧 loglist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/commission/loglist")]
    [HttpGet("/admin/money/commission/loglist")]
    [NonUnify]
    [DisplayName("站点后台佣金日志")]
    public async Task<OldApiResult> Loglist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var result = await RunAsync(async ct =>
        {
            var items = await _svc.ListAsync(ct);
            var user = OldApiRequestParam.Read(http, "User");
            if (!string.IsNullOrWhiteSpace(user))
            {
                var uk = user.Trim();
                items = items.Where(x =>
                    (!string.IsNullOrEmpty(x.UserName) && x.UserName.Contains(uk, StringComparison.OrdinalIgnoreCase))
                    || (int.TryParse(uk, out var uid) && x.UserID == uid)).ToList();
            }
            var statusRaw = OldApiRequestParam.Read(http, "Status");
            if (OldApiMoneyDisplay.TryParseCheckStatus(statusRaw, out var st))
                items = items.Where(x => x.Status == st).ToList();
            else if (int.TryParse(statusRaw, out var stInt))
                items = items.Where(x => x.Status == stInt).ToList();
            if (int.TryParse(OldApiRequestParam.Read(http, "Currency"), out var cur) && cur > 0)
                items = items.Where(x => x.Currency == cur).ToList();
            if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var startAt))
                items = items.Where(x => x.CreateAt >= startAt.Date).ToList();
            if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var endAt))
                items = items.Where(x => x.CreateAt < endAt.Date.AddDays(1)).ToList();

            var money = items.Sum(x => x.Money);
            var rows = items.Select(t => new
            {
                t.ID,
                t.UserID,
                t.UserName,
                t.Money,
                t.StartDate,
                t.EndDate,
                t.CreateAt,
                Status = OldApiMoneyDisplay.FormatCheckStatus(t.Status),
                CommID = 0,
                Name = string.Empty,
                Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency)
            });
            var dataCurrency = items.Select(x => x.Currency).Distinct().Count() == 1
                ? OldApiMoneyDisplay.FormatCurrency(items.First().Currency)
                : string.Empty;
            return OldApiShowResult.SlicePage(rows, pageIndex, pageSize, data: new { Money = money, Currency = dataCurrency });
        });
        return result;
    }

    /// <summary>佣金详情。对应旧 logdetail。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/commission/logdetail")]
    [NonUnify]
    [DisplayName("站点后台佣金详情")]
    public async Task<OldApiResult> Logdetail()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct => await _svc.DetailAsync(id, ct));
        return result;
    }

    /// <summary>通过佣金。对应旧 logagree。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/commission/logagree")]
    [NonUnify]
    [DisplayName("站点后台佣金通过")]
    public async Task<OldApiResult> Logagree()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct =>
        {
            var ok = await _svc.PayAsync(id, ct);
            if (!ok)
                throw Oops.Oh("#操作失败#");
            return null;
        });
        return result;
    }

    /// <summary>拒绝佣金。对应旧 logreject。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/commission/logreject")]
    [NonUnify]
    [DisplayName("站点后台佣金拒绝")]
    public async Task<OldApiResult> Logreject()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct =>
        {
            await _svc.RejectAsync(id, ct);
            return null;
        });
        return result;
    }

    /// <summary>生成待发佣金。对齐旧 create（仅 POST）。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/commission/create")]
    [NonUnify]
    [DisplayName("站点后台生成佣金")]
    public async Task<OldApiResult> Create()
    {
        var http = App.HttpContext;
        DateTime start = default, end = default;
        _ = DateTime.TryParse(
            OldApiRequestParam.Read(http, "StartAt") ?? OldApiRequestParam.Read(http, "StartDate"),
            out start);
        _ = DateTime.TryParse(
            OldApiRequestParam.Read(http, "EndAt") ?? OldApiRequestParam.Read(http, "EndDate"),
            out end);
        var result = await RunAsync(async ct =>
            await _svc.CreateAsync(start, end, now: null, ct));
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

using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 返点发放旧路径：/admin/money/returnplan/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money-returnplan", Order = 33)]
public class MoneyReturnPlanAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = "3fcfe08855714489bffd2b973b2ba96e";

    private readonly ReturnPayoutService _svc;

    public MoneyReturnPlanAppService(ReturnPayoutService svc) => _svc = svc;

    /// <summary>手动生成返点。对应旧 execute。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/returnplan/execute")]
    [NonUnify]
    [DisplayName("站点后台生成返点")]
    public async Task<OldApiResult> Execute()
    {
        var http = App.HttpContext;
        DateTime.TryParse(OldApiRequestParam.Read(http, "StartDate"), out var start);
        DateTime.TryParse(OldApiRequestParam.Read(http, "EndDate"), out var end);
        if (start == default) start = DateTime.Now.Date;
        if (end == default) end = DateTime.Now.Date;
        try
        {
            var r = await _svc.CreateAsync(start, end, cancellationToken: http?.RequestAborted ?? default);
            return OldApiResult.Ok($"成功{r.Success}笔,失败{r.Failed}笔", null);
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

    /// <summary>返点日志。对应旧 loglist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/returnplan/loglist")]
    [HttpGet("/admin/money/returnplan/loglist")]
    [NonUnify]
    [DisplayName("站点后台返点日志")]
    public async Task<OldApiResult> Loglist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var result = await RunAsync(async () =>
        {
            var items = await _svc.ListAsync(http?.RequestAborted ?? default);
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

            return OldApiShowResult.SlicePage(
                items.Select(t => new
                {
                    t.ID,
                    t.PlanID,
                    t.UserID,
                    t.UserName,
                    t.CreateAt,
                    StartDate = t.StartAt,
                    Date = t.EndAt,
                    t.Money,
                    Status = OldApiMoneyDisplay.FormatCheckStatus(t.Status),
                    BetMoney = 0m,
                    Message = string.Empty,
                    Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
                    Detail = string.Empty
                }),
                pageIndex,
                pageSize);
        });
        return result;
    }

    /// <summary>通过返点。对应旧 logagree。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/returnplan/logagree")]
    [NonUnify]
    [DisplayName("站点后台返点通过")]
    public async Task<OldApiResult> Logagree()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async () =>
        {
            await _svc.PayAsync(id, App.HttpContext?.RequestAborted ?? default);
            return null;
        });
        return result;
    }

    /// <summary>拒绝返点。对应旧 logreject。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/returnplan/logreject")]
    [NonUnify]
    [DisplayName("站点后台返点拒绝")]
    public async Task<OldApiResult> Logreject()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async () =>
        {
            await _svc.RejectAsync(id, App.HttpContext?.RequestAborted ?? default);
            return null;
        });
        return result;
    }

    private static async Task<OldApiResult> RunAsync(Func<Task<object?>> action)
    {
        try
        {
            return OldApiResult.Ok(string.Empty, await action());
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

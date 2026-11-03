using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台加减款审核：/admin/money/moneyreview/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money-review", Order = 36)]
public class MoneyReviewFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = "e050f27894b9438e9169c2879056bd76";
    public const string AgreePermissionId = "ceeda2b5c8924a1a33a473c254ebb59f";

    private readonly MoneyReviewService _svc;
    private readonly SiteAdminAuthService _adminAuth;

    public MoneyReviewFormAppService(MoneyReviewService svc, SiteAdminAuthService adminAuth)
    {
        _svc = svc;
        _adminAuth = adminAuth;
    }

    /// <summary>审核列表。对应旧 list。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/moneyreview/list")]
    [HttpGet("/admin/money/moneyreview/list")]
    [NonUnify]
    [DisplayName("站点后台加减款审核列表")]
    public async Task<OldApiResult> List()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new MoneyReviewListInput
        {
            Status = OldApiRequestParam.Read(http, "Status"),
            User = OldApiRequestParam.Read(http, "User"),
            BelongUser = OldApiRequestParam.Read(http, "BelongUser"),
            Agent = OldApiRequestParam.Read(http, "Agent"),
            SourceID = OldApiRequestParam.Read(http, "SourceID"),
            CreatorAdminName = OldApiRequestParam.Read(http, "CreatorAdminName"),
            ReviewerAdminName = OldApiRequestParam.Read(http, "ReviewerAdminName"),
        };
        if (int.TryParse(OldApiRequestParam.Read(http, "Currency"), out var cur) && cur > 0)
            input.Currency = cur;
        if (int.TryParse(OldApiRequestParam.Read(http, "Type"), out var type) && type != 0)
            input.Type = type;
        if (int.TryParse(OldApiRequestParam.Read(http, "Action"), out var action) &&
            (action == MoneyReviewService.ActionIn || action == MoneyReviewService.ActionOut))
            input.Action = action;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var start))
            input.StartAt = start;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var end))
            input.EndAt = end;

        var result = await RunAsync(async ct =>
        {
            var items = await _svc.ListAsync(input, ct);
            var rows = items.Select(t => new
            {
                t.ID,
                t.UserID,
                Name = t.UserName,
                UserName = t.UserName,
                t.CreateAt,
                t.Money,
                t.Type,
                TypeName = OldApiMoneyDisplay.FormatMoneyTypeValue(t.Type),
                t.Action,
                Status = OldApiEnumDisplay.CheckStatus(t.Status),
                StatusValue = t.Status,
                Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
                CurrencyCode = t.Currency,
                t.SourceID,
                t.Description,
                t.DescriptionA,
                t.Flow,
                t.MinFlow,
                PayName = t.PayName ?? string.Empty,
                t.CreatorID,
                Creator = t.CreatorName ?? string.Empty,
                t.ReviewerID,
                Admin = t.ReviewerName ?? string.Empty,
                t.ReviewAt
            }).ToList();
            return OldApiShowResult.SlicePage(rows, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>单笔审核。对应旧 check。</summary>
    [AllowAnonymous]
    [OldApiPermission(AgreePermissionId)]
    [HttpPost("/admin/money/moneyreview/check")]
    [NonUnify]
    [DisplayName("站点后台加减款审核")]
    public async Task<OldApiResult> Check()
    {
        var http = App.HttpContext;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(http?.RequestAborted ?? default);
        var statusRaw = OldApiRequestParam.Read(http, "Status");
        var status = OldApiRequestParam.ReadInt(http, "Status");
        if (status == 0 && OldApiMoneyDisplay.TryParseCheckStatus(statusRaw, out var parsed))
            status = parsed;
        var input = new MoneyReviewCheckInput
        {
            ID = OldApiRequestParam.ReadInt(http, "ID"),
            Status = status,
            Description = OldApiRequestParam.Read(http, "Description"),
            DescriptionA = OldApiRequestParam.Read(http, "DescriptionA"),
        };
        var result = await RunAsync(async ct =>
        {
            await _svc.CheckAsync(input, admin?.ID ?? 0, ct);
            return null;
        });
        return result;
    }

    /// <summary>批量通过。对应旧 batchsuccess。</summary>
    [AllowAnonymous]
    [OldApiPermission(AgreePermissionId)]
    [HttpPost("/admin/money/moneyreview/batchsuccess")]
    [NonUnify]
    [DisplayName("站点后台加减款批量通过")]
    public async Task<OldApiResult> Batchsuccess()
    {
        var http = App.HttpContext;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(http?.RequestAborted ?? default);
        var ids = OldApiRequestParam.Read(http, "ID") ?? OldApiRequestParam.Read(http, "IDs") ?? string.Empty;
        var result = await RunAsync(async ct =>
        {
            await _svc.BatchCheckAsync(ids, MoneyReviewService.StatusSuccess, admin?.ID ?? 0, ct);
            return null;
        });
        return result;
    }

    /// <summary>批量拒绝。对应旧 batchfail。</summary>
    [AllowAnonymous]
    [OldApiPermission(AgreePermissionId)]
    [HttpPost("/admin/money/moneyreview/batchfail")]
    [NonUnify]
    [DisplayName("站点后台加减款批量拒绝")]
    public async Task<OldApiResult> Batchfail()
    {
        var http = App.HttpContext;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(http?.RequestAborted ?? default);
        var ids = OldApiRequestParam.Read(http, "ID") ?? OldApiRequestParam.Read(http, "IDs") ?? string.Empty;
        var result = await RunAsync(async ct =>
        {
            await _svc.BatchCheckAsync(ids, MoneyReviewService.StatusFailed, admin?.ID ?? 0, ct);
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

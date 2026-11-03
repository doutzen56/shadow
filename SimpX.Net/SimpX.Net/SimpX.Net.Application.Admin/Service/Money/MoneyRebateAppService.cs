using System.Collections;
using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台返水兼容路由：/admin/money/rebate/*（路径与方法名 1:1 旧版）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money-rebate", Order = 30)]
public class MoneyRebateAppService : IDynamicApiController, ITransient
{
    /// <summary>返水发放权限 GUID。</summary>
    public const string RebatePermissionId = "0c5acdfcc7d849c2b352e7466c447dd1";

    private readonly RebatePayoutService _svc;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>
    /// 注入返水与站点后台会话。
    /// </summary>
    public MoneyRebateAppService(RebatePayoutService svc, SiteAdminAuthService adminAuth)
    {
        _svc = svc;
        _adminAuth = adminAuth;
    }

    /// <summary>会员分组下拉。对应旧 grouplist。</summary>
    [AllowAnonymous]
    [OldApiPermission(RebatePermissionId)]
    [HttpPost("/admin/money/rebate/grouplist")]
    [HttpGet("/admin/money/rebate/grouplist")]
    [NonUnify]
    [DisplayName("站点后台返水分组列表")]
    public async Task<OldApiResult> Grouplist()
    {
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.GrouplistAsync(null, ct);
            return OldApiShowResult.OfList(AsEnumerable(rows));
        });
        return result;
    }

    /// <summary>返水日志列表。对应旧 loglist。</summary>
    [AllowAnonymous]
    [OldApiPermission(RebatePermissionId)]
    [HttpPost("/admin/money/rebate/loglist")]
    [HttpGet("/admin/money/rebate/loglist")]
    [NonUnify]
    [DisplayName("站点后台返水日志列表")]
    public async Task<OldApiResult> Loglist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = ReadFormDictionary(http);
        var result = await RunAsync(ct =>
            _svc.LoglistForOldApiAsync(input, pageIndex, pageSize, ct));
        return result;
    }

    /// <summary>返水计划列表。对应旧 planlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(RebatePermissionId)]
    [HttpPost("/admin/money/rebate/planlist")]
    [HttpGet("/admin/money/rebate/planlist")]
    [NonUnify]
    [DisplayName("站点后台返水计划列表")]
    public async Task<OldApiResult> Planlist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = ReadFormDictionary(http);
        var result = await RunAsync(async ct =>
        {
            var page = await _svc.PlanlistPageAsync(input, pageIndex, pageSize, ct);
            var rows = page.Items.Select(t => new
            {
                t.ID,
                t.Name,
                t.CreateAt,
                t.StartAt,
                t.EndAt,
                t.Money,
                t.MoneyT,
                t.Paid,
                t.PaidT,
                Admin = t.AdminName ?? string.Empty,
                t.Total,
                t.Success,
                t.Faild,
                t.FlowRate,
                t.Currency,
                t.FromCurrency
            });
            return OldApiShowResult.OfPage(
                rows,
                page.RecordCount,
                pageIndex,
                pageSize,
                data: new { Money = page.DataMoney, Paid = page.DataPaid });
        });
        return result;
    }

    /// <summary>切换返水设定启用。对应旧 updateenabled。</summary>
    [AllowAnonymous]
    [OldApiPermission(RebatePermissionId)]
    [HttpPost("/admin/money/rebate/updateenabled")]
    [NonUnify]
    [DisplayName("站点后台返水设定开关")]
    public async Task<OldApiResult> Updateenabled()
    {
        var input = ReadFormDictionary(App.HttpContext);
        var result = await RunAsync(ct => _svc.UpdateenabledAsync(input, ct));
        return result;
    }

    /// <summary>返水日志明细。对应旧 logdetail。</summary>
    [AllowAnonymous]
    [OldApiPermission(RebatePermissionId)]
    [HttpPost("/admin/money/rebate/logdetail")]
    [HttpGet("/admin/money/rebate/logdetail")]
    [NonUnify]
    [DisplayName("站点后台返水日志明细")]
    public async Task<OldApiResult> Logdetail()
    {
        var input = ReadFormDictionary(App.HttpContext);
        var result = await RunAsync(ct => _svc.LogdetailAsync(input, ct));
        return result;
    }

    /// <summary>自助返水开启时阻断手工。对应旧 blockwhenautoreturnv2enabled。</summary>
    [AllowAnonymous]
    [OldApiPermission(RebatePermissionId)]
    [HttpPost("/admin/money/rebate/blockwhenautoreturnv2enabled")]
    [HttpGet("/admin/money/rebate/blockwhenautoreturnv2enabled")]
    [NonUnify]
    [DisplayName("站点后台返水阻断检查")]
    public async Task<OldApiResult> Blockwhenautoreturnv2enabled()
    {
        var input = ReadFormDictionary(App.HttpContext);
        var result = await RunAsync(ct => _svc.BlockWhenAutoReturnV2EnabledAsync(input, ct));
        return result;
    }

    /// <summary>拒绝返水。对应旧 reject。</summary>
    [AllowAnonymous]
    [OldApiPermission(RebatePermissionId)]
    [HttpPost("/admin/money/rebate/reject")]
    [NonUnify]
    [DisplayName("站点后台拒绝返水")]
    public async Task<OldApiResult> Reject()
    {
        var http = App.HttpContext;
        var planId = OldApiRequestParam.ReadInt(http, "PlanID");
        var logId = OldApiRequestParam.ReadInt(http, "LogID");
        var result = await RunAsync(async ct =>
        {
            await _svc.RejectAsync(planId, logId, ct);
            return null;
        });
        return result;
    }

    /// <summary>重发返水。对应旧 reextcute。</summary>
    [AllowAnonymous]
    [OldApiPermission(RebatePermissionId)]
    [HttpPost("/admin/money/rebate/reextcute")]
    [NonUnify]
    [DisplayName("站点后台重发返水")]
    public async Task<OldApiResult> Reextcute()
    {
        var http = App.HttpContext;
        var planId = OldApiRequestParam.ReadInt(http, "PlanID");
        var logId = OldApiRequestParam.ReadInt(http, "LogID");
        var result = await RunAsync(async ct =>
        {
            var count = await _svc.ReExecuteAsync(planId, logId, ct);
            return new { Count = count };
        });
        return result;
    }

    /// <summary>执行返水发放。对应旧 execute（写路径，对打时慎用）。</summary>
    [AllowAnonymous]
    [OldApiPermission(RebatePermissionId)]
    [HttpPost("/admin/money/rebate/execute")]
    [HttpPost("/admin/money/rebate/create")]
    [HttpPost("/admin/money/rebate/executeagent")]
    [HttpPost("/admin/money/rebate/executev2")]
    [NonUnify]
    [DisplayName("站点后台执行返水")]
    public async Task<OldApiResult> Execute()
    {
        var http = App.HttpContext;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(http?.RequestAborted ?? default);
        var input = new RebateExecuteInput
        {
            Title = OldApiRequestParam.Read(http, "Title"),
            Name = OldApiRequestParam.Read(http, "Title") ?? OldApiRequestParam.Read(http, "Name"),
            FlowRate = decimal.TryParse(OldApiRequestParam.Read(http, "FlowRate"), out var fr) ? fr : 0,
            User = OldApiRequestParam.Read(http, "User"),
        };
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var start))
            input.StartAt = start;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var end))
            input.EndAt = end;
        if (int.TryParse(OldApiRequestParam.Read(http, "Currency"), out var cur) && cur > 0)
            input.Currency = cur;

        var checkedText = OldApiRequestParam.Read(http, "checked") ?? OldApiRequestParam.Read(http, "Checked");
        input.Checked = ParseIntArray(checkedText);
        input.ID = ParseIntArray(OldApiRequestParam.Read(http, "ID"));

        var result = await RunAsync(async ct =>
        {
            var exec = await _svc.ExecuteAsync(input, admin?.ID ?? 0, ct);
            return new { Success = exec.Success, Faild = exec.Faild, PlanID = exec.PlanID };
        });
        return result;
    }

    private static int[] ParseIntArray(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        return text.Split([',', '|', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .ToArray();
    }

    private static Dictionary<string, object?> ReadFormDictionary(Microsoft.AspNetCore.Http.HttpContext http)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (http == null)
            return map;
        if (http.Request.HasFormContentType)
        {
            foreach (var key in http.Request.Form.Keys)
                map[key] = http.Request.Form[key].ToString();
        }

        foreach (var key in http.Request.Query.Keys)
        {
            if (!map.ContainsKey(key))
                map[key] = http.Request.Query[key].ToString();
        }

        return map;
    }

    private static IEnumerable AsEnumerable(object? value)
    {
        if (value is IEnumerable enumerable and not string)
            return enumerable;
        return Array.Empty<object>();
    }

    private static async Task<OldApiResult> RunAsync(Func<CancellationToken, Task<object?>> action)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        try
        {
            var info = await action(ct);
            return OldApiResult.Ok(string.Empty, info);
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

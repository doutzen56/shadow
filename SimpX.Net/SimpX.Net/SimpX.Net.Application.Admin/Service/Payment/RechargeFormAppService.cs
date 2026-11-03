using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台充值订单兼容路由：/admin/recharge/*（与 /admin/money/recharge 并列，1:1 旧路径）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "recharge", Order = 38)]
public class RechargeFormAppService : IDynamicApiController, ITransient
{
    /// <summary>充值订单权限 GUID（与 money/recharge 同权）。</summary>
    public const string PermissionId = MoneyRechargeAppService.RechargeOrderPermissionId;

    private readonly RechargeOrderService _orders;
    private readonly SitePaymentService _payment;

    /// <summary>注入。</summary>
    private readonly RechargeOrderCheckService _check;
    private readonly RechargeOrderDeleteService _delete;
    private readonly RechargeCheckFailService _checkFail;
    private readonly SiteAdminAuthService _auth;
    private readonly IBizDbAccessor _biz;

    public RechargeFormAppService(
        RechargeOrderService orders,
        SitePaymentService payment,
        RechargeOrderCheckService check,
        RechargeOrderDeleteService delete,
        RechargeCheckFailService checkFail,
        SiteAdminAuthService auth,
        IBizDbAccessor biz)
    {
        _orders = orders;
        _payment = payment;
        _check = check;
        _delete = delete;
        _checkFail = checkFail;
        _auth = auth;
        _biz = biz;
    }

    /// <summary>充值订单列表。对应旧 orderlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/orderlist")]
    [HttpGet("/admin/recharge/orderlist")]
    [NonUnify]
    [DisplayName("站点后台充值订单列表")]
    public async Task<OldApiResult> Orderlist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new RechargeOrderPageInput
        {
            Page = pageIndex,
            PageSize = pageSize,
            OrderID = OldApiRequestParam.Read(http, "OrderID"),
            KeyWord = OldApiRequestParam.Read(http, "KeyWord") ?? OldApiRequestParam.Read(http, "Keyword"),
            User = OldApiRequestParam.Read(http, "User"),
            Agent = OldApiRequestParam.Read(http, "Agent"),
            PayID = OldApiRequestParam.ReadInt(http, "PayID"),
        };
        if (int.TryParse(OldApiRequestParam.Read(http, "Currency"), out var cur))
            input.Currency = cur;
        if (int.TryParse(OldApiRequestParam.Read(http, "FromCurrency"), out var fromCur) && fromCur > 0)
            input.FromCurrency = fromCur;
        // 旧站 Status：0未到账/1已到帐；兼容 IsPay
        var statusRaw = OldApiRequestParam.Read(http, "Status") ?? OldApiRequestParam.Read(http, "IsPay");
        if (bool.TryParse(statusRaw, out var isPay))
            input.IsPay = isPay;
        else if (statusRaw is "0" or "1")
            input.IsPay = statusRaw == "1";

        if (decimal.TryParse(OldApiRequestParam.Read(http, "MoneyFrom"), out var mf))
            input.MoneyFrom = mf;
        if (decimal.TryParse(OldApiRequestParam.Read(http, "MoneyTo"), out var mt))
            input.MoneyTo = mt;
        var proxyRaw = OldApiRequestParam.Read(http, "IsProxyRecharge");
        if (proxyRaw is "0" or "1")
            input.IsProxyRecharge = proxyRaw == "1";
        else if (bool.TryParse(proxyRaw, out var proxy))
            input.IsProxyRecharge = proxy;
        input.DateType = OldApiRequestParam.Read(http, "DateType");
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var start))
            input.StartAt = start;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var end))
            input.EndAt = end;

        var result = await RunAsync(async ct =>
        {
            var page = await _orders.PageAsync(input, ct);
            var rows = page.Items.Select(t => new
            {
                ID = (t.OrderID ?? string.Empty).Trim(),
                OrderID = (t.OrderID ?? string.Empty).Trim(),
                t.UserID,
                t.UserName,
                Agent = t.AgentName,
                AgentName = t.AgentName,
                t.Money,
                t.Paid,
                t.Fee,
                t.Cost,
                t.PayID,
                t.Payment,
                t.ThirdParty,
                RealPaid = OldApiMoneyDisplay.NormalizeCurrencySuffix(t.RealPaid, t.Currency),
                t.IsPay,
                t.Count,
                t.IsProxyRecharge,
                SystemID = (t.SystemID ?? string.Empty).Trim(),
                t.CreateAt,
                t.PayAt,
                Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
                t.Remark,
                t.Admin,
                DealTime = t.DealTime ?? string.Empty
            });
            return OldApiShowResult.OfPage(rows, page.Total, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>充值订单详情。对应旧 orderinfo。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/orderinfo")]
    [HttpGet("/admin/recharge/orderinfo")]
    [NonUnify]
    [DisplayName("站点后台充值订单详情")]
    public async Task<OldApiResult> Orderinfo()
    {
        var form = ReadForm(App.HttpContext);
        var result = await RunAsync(ct => _orders.OrderinfoAsync(form, ct));
        return result;
    }

    /// <summary>充值通道设定。对应旧 /admin/recharge/settinglist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/settinglist")]
    [HttpGet("/admin/recharge/settinglist")]
    [NonUnify]
    [DisplayName("站点后台充值设定列表")]
    public async Task<OldApiResult> Settinglist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new SitePaymentListInput
        {
            Status = OldApiRequestParam.Read(http, "Status"),
            Name = OldApiRequestParam.Read(http, "Name"),
            Currency = OldApiRequestParam.Read(http, "Currency"),
            CateID = OldApiRequestParam.ReadInt(http, "CateID"),
        };
        var result = await RunAsync(async ct =>
        {
            var items = await _payment.ListAsync(input, ct);
            var sysIds = items
                .Select(t => int.TryParse(t.Payment, out var sid) ? sid : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            var sysNames = new Dictionary<int, string>();
            if (sysIds.Count > 0)
            {
                var sysRows = await _biz.Db.Queryable<SimpX.Net.Data.Entity.Systems.SystemPayment>()
                    .Where(p => sysIds.Contains(p.ID))
                    .Select(p => new { p.ID, p.Name })
                    .ToListAsync(ct);
                foreach (var s in sysRows)
                    sysNames[s.ID] = s.Name ?? string.Empty;
            }

            // 对齐 /admin/money/recharge/settinglist 主字段（旧站 settinglist 全列）
            var rows = items.Select(t =>
            {
                var sysId = int.TryParse(t.Payment, out var sid) ? sid : 0;
                return new
                {
                    t.ID,
                    t.Category,
                    GroupNames = t.GroupNames ?? string.Empty,
                    t.Name,
                    t.Currency,
                    t.Remark,
                    t.Payment,
                    SysID = sysId,
                    SysName = sysId > 0 && sysNames.TryGetValue(sysId, out var sn) ? sn : string.Empty,
                    t.MinMoney,
                    t.MaxMoney,
                    t.MaxPay,
                    t.Paid,
                    t.Sort,
                    t.Fee,
                    t.Cost,
                    t.Status,
                    IsOpen = t.Status != 0
                };
            });
            return OldApiShowResult.SlicePage(rows, pageIndex, pageSize);
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/open")]
    [NonUnify, DisplayName("站点后台开启充值通道")]
    public Task<OldApiResult> Open() =>
        RunDict(ct => _payment.OpenAsync(ReadForm(App.HttpContext), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/stop")]
    [NonUnify, DisplayName("站点后台停用充值通道")]
    public Task<OldApiResult> Stop() =>
        RunDict(ct => _payment.StopAsync(ReadForm(App.HttpContext), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/deletepayment")]
    [NonUnify, DisplayName("站点后台删除充值通道")]
    public Task<OldApiResult> Deletepayment() =>
        RunDict(ct => _payment.DeletepaymentAsync(ReadForm(App.HttpContext), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/updateisrecommend")]
    [NonUnify, DisplayName("站点后台充值分类推荐")]
    public Task<OldApiResult> Updateisrecommend() =>
        RunDict(ct => _payment.UpdateisrecommendAsync(ReadForm(App.HttpContext), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/updatepaymentstatus")]
    [NonUnify, DisplayName("站点后台充值通道启停")]
    public Task<OldApiResult> Updatepaymentstatus() =>
        RunDict(ct => _payment.UpdatepaymentstatusAsync(ReadForm(App.HttpContext), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/getcategoryinfo"), HttpGet("/admin/recharge/getcategoryinfo")]
    [NonUnify, DisplayName("站点后台充值分类信息")]
    public Task<OldApiResult> Getcategoryinfo() =>
        RunDict(ct => _payment.GetpaymentsettingAsync(ReadForm(App.HttpContext), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/checkorder")]
    [NonUnify, DisplayName("站点后台确认充值到账")]
    public async Task<OldApiResult> Checkorder()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            var admin = await _auth.TryGetCurrentAdminAsync(ct);
            _ = decimal.TryParse(OldApiFormReader.ReadString(http, "Paid"), out var paid);
            _ = decimal.TryParse(OldApiFormReader.ReadString(http, "Fee"), out var fee);
            _ = decimal.TryParse(OldApiFormReader.ReadString(http, "Cost"), out var cost);
            await _check.CheckAsync(new RechargeOrderCheckInput
            {
                ID = OldApiFormReader.ReadString(http, "ID", "Id"),
                SystemID = OldApiFormReader.ReadString(http, "SystemID"),
                Paid = paid,
                Fee = fee,
                Cost = cost
            }, admin?.ID ?? 0, cancellationToken: ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/deleteorder")]
    [NonUnify, DisplayName("站点后台删除充值订单")]
    public async Task<OldApiResult> Deleteorder()
    {
        var result = await RunAsync(async ct =>
        {
            var admin = await _auth.TryGetCurrentAdminAsync(ct);
            await _delete.DeleteAsync(
                OldApiFormReader.ReadString(App.HttpContext, "ID", "Id"),
                admin?.ID ?? 0,
                ct);
            return null;
        });
        return result;
    }

    /// <summary>比对失败订单列表。对应旧 orderlistfail（IsCheckFail）。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/recharge/orderlistfail"), HttpGet("/admin/recharge/orderlistfail")]
    [NonUnify, DisplayName("站点后台充值失败单")]
    public async Task<OldApiResult> Orderlistfail()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new RechargeCheckFailPageInput
        {
            Page = pageIndex,
            PageSize = pageSize,
            OrderID = OldApiRequestParam.Read(http, "OrderID") ?? OldApiRequestParam.Read(http, "ID"),
            User = OldApiRequestParam.Read(http, "User"),
            Agent = OldApiRequestParam.Read(http, "Agent"),
        };
        if (int.TryParse(OldApiRequestParam.Read(http, "Currency"), out var cur) && cur > 0)
            input.Currency = cur;
        var isPayRaw = OldApiRequestParam.Read(http, "IsPay");
        if (isPayRaw is "1" or "true" or "True")
            input.IsPay = true;
        else if (isPayRaw is "0" or "false" or "False")
            input.IsPay = false;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var start))
            input.StartAt = start;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var end))
            input.EndAt = end;

        return await RunAsync(async ct =>
        {
            var page = await _checkFail.PageAsync(input, ct);
            var rows = page.Items.Select(t => new
            {
                ID = t.OrderID,
                OrderID = t.OrderID,
                t.UserID,
                t.UserName,
                t.AgentID,
                t.AgentName,
                t.Money,
                t.Paid,
                t.IsPay,
                t.Count,
                t.PayAt,
                t.PayID,
                Payment = string.IsNullOrEmpty(t.PayName) ? (t.PayID > 0 ? $"#{t.PayID}" : string.Empty) : t.PayName,
                PayName = t.PayName ?? string.Empty,
                Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
                t.CreateAt,
                Remark = t.Remark,
                Description = t.Remark,
            });
            return OldApiShowResult.OfPage(rows, page.Total, pageIndex, pageSize);
        });
    }

    private static async Task<OldApiResult> RunDict(Func<CancellationToken, Task<object?>> action) => await OldApiFormReader.RunAsync(action);

    private static Dictionary<string, object?> ReadForm(Microsoft.AspNetCore.Http.HttpContext http)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (http == null) return map;
        if (http.Request.HasFormContentType)
            foreach (var key in http.Request.Form.Keys)
                map[key] = http.Request.Form[key].ToString();
        foreach (var key in http.Request.Query.Keys)
            if (!map.ContainsKey(key))
                map[key] = http.Request.Query[key].ToString();
        return map;
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

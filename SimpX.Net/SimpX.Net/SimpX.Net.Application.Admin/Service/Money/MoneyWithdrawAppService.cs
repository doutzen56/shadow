using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台提现兼容路由：/admin/money/withdraw/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money-withdraw", Order = 31)]
public class MoneyWithdrawAppService : IDynamicApiController, ITransient
{
    /// <summary>提现接口权限。</summary>
    public const string WithdrawChannelPermissionId = "3fcfe08855714489bffd2b973b2ba96e";

    /// <summary>提现订单权限。</summary>
    public const string WithdrawOrderPermissionId = "0a83144524de42b099cf8fd5da04ef54";

    private readonly WithdrawChannelService _channels;
    private readonly WithdrawChannelOperateService _channelOperate;
    private readonly WithdrawOrderService _orders;
    private readonly WithdrawOrderOperateService _orderOperate;
    private readonly SiteAdminAuthService _adminAuth;
    private readonly IBizDbAccessor _biz;

    /// <summary>注入。</summary>
    public MoneyWithdrawAppService(
        WithdrawChannelService channels,
        WithdrawChannelOperateService channelOperate,
        WithdrawOrderService orders,
        WithdrawOrderOperateService orderOperate,
        SiteAdminAuthService adminAuth,
        IBizDbAccessor biz)
    {
        _channels = channels;
        _channelOperate = channelOperate;
        _orders = orders;
        _orderOperate = orderOperate;
        _adminAuth = adminAuth;
        _biz = biz;
    }

    /// <summary>提现渠道列表。对应旧 channelist。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawChannelPermissionId)]
    [HttpPost("/admin/money/withdraw/channelist")]
    [HttpGet("/admin/money/withdraw/channelist")]
    [NonUnify]
    [DisplayName("站点后台提现渠道列表")]
    public async Task<OldApiResult> Channelist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var ct = http?.RequestAborted ?? default;
        var result = await RunAsync(async () =>
        {
            var items = await _channels.ListAsync(ct);
            if (items == null || items.Count == 0)
                throw Oops.Oh("#没有数据#");

            var rows = items.Select(c => new
            {
                c.ID,
                c.Name,
                c.SysID,
                SysName = c.SysName ?? string.Empty,
                IsOpen = c.IsOpen ? 1 : 0,
                c.Cost,
                c.Sort,
                c.CreateAt,
                c.Currency
            });
            return OldApiShowResult.SlicePage(rows, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>开放提现渠道。对应旧 openchannelist。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/openchannelist")]
    [HttpGet("/admin/money/withdraw/openchannelist")]
    [NonUnify]
    [DisplayName("站点后台开放提现渠道")]
    public async Task<OldApiResult> Openchannelist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var ct = http?.RequestAborted ?? default;
        var result = await RunAsync(async () =>
        {
            var items = await _channels.ListOpenAsync(ct);
            return OldApiShowResult.SlicePage(
                items.Select(c => new { c.ID, c.Name, c.Currency }),
                pageIndex,
                pageSize);
        });
        return result;
    }

    /// <summary>提现订单列表。对应旧 orderlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/orderlist")]
    [HttpGet("/admin/money/withdraw/orderlist")]
    [NonUnify]
    [DisplayName("站点后台提现订单列表")]
    public async Task<OldApiResult> Orderlist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new WithdrawOrderPageInput
        {
            Page = pageIndex,
            PageSize = pageSize,
            User = OldApiRequestParam.Read(http, "User"),
            Agent = OldApiRequestParam.Read(http, "Agent"),
            SystemID = OldApiRequestParam.Read(http, "SystemID"),
        };
        var orderId = OldApiRequestParam.ReadInt(http, "ID");
        if (orderId > 0)
            input.OrderID = orderId;
        var statusRaw = OldApiRequestParam.Read(http, "Status");
        if (int.TryParse(statusRaw, out var st))
            input.Status = st;
        else if (!string.IsNullOrWhiteSpace(statusRaw)
                 && Enum.TryParse<WithdrawStatus>(statusRaw.Trim(), true, out var stEnum))
            input.Status = (int)stEnum;
        else if (string.Equals(statusRaw, "Failed", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(statusRaw, "Fail", StringComparison.OrdinalIgnoreCase))
            input.Status = (int)WithdrawStatus.Faild;
        if (int.TryParse(OldApiRequestParam.Read(http, "ChannelID"), out var ch) && ch > 0)
            input.ChannelID = ch;
        if (int.TryParse(OldApiRequestParam.Read(http, "Currency"), out var cur) && cur > 0)
            input.Currency = cur;
        if (int.TryParse(OldApiRequestParam.Read(http, "FromCurrency"), out var fromCur) && fromCur > 0)
            input.FromCurrency = fromCur;
        input.DateType = OldApiRequestParam.Read(http, "DateType");
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var start))
            input.StartAt = start;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var end))
            input.EndAt = end;
        if (decimal.TryParse(OldApiRequestParam.Read(http, "MoneyFrom"), out var mf))
            input.MoneyFrom = mf;
        if (decimal.TryParse(OldApiRequestParam.Read(http, "MoneyTo"), out var mt))
            input.MoneyTo = mt;
        if (int.TryParse(OldApiRequestParam.Read(http, "StartAudit"), out var startAudit) && startAudit >= 0)
            input.StartAudit = startAudit;
        if (int.TryParse(OldApiRequestParam.Read(http, "EndAudit"), out var endAudit) && endAudit >= 0)
            input.EndAudit = endAudit;
        if (int.TryParse(OldApiRequestParam.Read(http, "StartCash"), out var startCash) && startCash >= 0)
            input.StartCash = startCash;
        if (int.TryParse(OldApiRequestParam.Read(http, "EndCash"), out var endCash) && endCash >= 0)
            input.EndCash = endCash;

        var ct = http?.RequestAborted ?? default;
        var result = await RunAsync(async () =>
        {
            var page = await _orders.PageAsync(input, ct);
            var rows = page.Items.Select(t => new
            {
                ID = t.OrderID,
                OrderID = t.OrderID,
                t.UserID,
                t.UserName,
                Agent = t.AgentName,
                AgentName = t.AgentName,
                Group = t.Group,
                t.Money,
                t.Paid,
                t.Fee,
                t.Cost,
                RealPaid = t.RealPaid,
                t.Status,
                StatusValue = t.StatusValue,
                t.ChannelID,
                Channel = t.Channel,
                t.CreateAt,
                t.PayAt,
                Currency = t.CurrencyCode,
                t.AccountName,
                t.AccountNumber,
                Bank = t.BankInfo,
                Admin = t.Admin,
                Cashier = t.Cashier,
                t.AdminID,
                t.CashierID,
                t.SystemID,
                t.AuditAt,
                AuditTime = t.AuditTime,
                DealTime = t.DealTime,
                Description = t.Description,
                DescriptionA = t.DescriptionA,
                t.Mark
            });
            return OldApiShowResult.OfPage(rows, page.Total, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>提现订单详情。对应旧 orderinfo。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/orderinfo")]
    [HttpGet("/admin/money/withdraw/orderinfo")]
    [NonUnify]
    [DisplayName("站点后台提现订单详情")]
    public async Task<OldApiResult> Orderinfo()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var ct = App.HttpContext?.RequestAborted ?? default;
        var result = await RunAsync(async () => await _orders.InfoAsync(id, ct));
        return result;
    }

    /// <summary>提现订单日志。对应旧 orderlog。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/orderlog")]
    [HttpGet("/admin/money/withdraw/orderlog")]
    [NonUnify]
    [DisplayName("站点后台提现订单日志")]
    public async Task<OldApiResult> Orderlog()
    {
        var http = App.HttpContext;
        var orderId = OldApiRequestParam.ReadInt(http, "OrderID");
        if (orderId <= 0)
            orderId = OldApiRequestParam.ReadInt(http, "ID");
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var ct = http?.RequestAborted ?? default;
        var result = await RunAsync(async () =>
        {
            var logs = await _orders.ListLogsAsync(orderId, ct);
            return OldApiShowResult.SlicePage(
                logs.Select(l => new
                {
                    l.ID,
                    l.CreateAt,
                    l.Status,
                    IP = string.Empty,
                    IPAddress = string.Empty,
                    Admin = l.AdminID
                }),
                pageIndex,
                pageSize);
        });
        return result;
    }

    /// <summary>审核提现订单。对应旧 checkorder。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/checkorder")]
    [NonUnify]
    [DisplayName("站点后台提现审核")]
    public async Task<OldApiResult> Checkorder()
    {
        var http = App.HttpContext;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(http?.RequestAborted ?? default);
        var orderId = OldApiRequestParam.ReadInt(http, "OrderID");
        if (orderId <= 0)
            orderId = OldApiRequestParam.ReadInt(http, "ID");
        var input = new WithdrawOrderCheckInput
        {
            OrderID = orderId,
            Action = OldApiRequestParam.Read(http, "Action"),
            Description = OldApiRequestParam.Read(http, "Description"),
            DescriptionA = OldApiRequestParam.Read(http, "DescriptionA"),
            Mark = OldApiRequestParam.Read(http, "Mark")
        };
        var result = await RunAsync(async () =>
        {
            await _orderOperate.CheckAsync(input, admin?.ID ?? 0, http?.RequestAborted ?? default);
            return null;
        });
        return result;
    }

    /// <summary>处理提现订单。对应旧 execorder。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/execorder")]
    [NonUnify]
    [DisplayName("站点后台提现出款")]
    public async Task<OldApiResult> Execorder()
    {
        var http = App.HttpContext;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(http?.RequestAborted ?? default);
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "Fee"), out var fee);
        var orderId = OldApiRequestParam.ReadInt(http, "OrderID");
        if (orderId <= 0)
            orderId = OldApiRequestParam.ReadInt(http, "ID");
        var input = new WithdrawOrderExecInput
        {
            OrderID = orderId,
            ChannelID = OldApiRequestParam.ReadInt(http, "ChannelID"),
            Action = OldApiRequestParam.Read(http, "Action"),
            Fee = fee,
            Description = OldApiRequestParam.Read(http, "Description"),
            DescriptionA = OldApiRequestParam.Read(http, "DescriptionA"),
            Mark = OldApiRequestParam.Read(http, "Mark")
        };
        var result = await RunAsync(async () =>
        {
            await _orderOperate.ExecAsync(input, admin?.ID ?? 0, http?.RequestAborted ?? default);
            return null;
        });
        return result;
    }

    /// <summary>代付查单。对应旧 queryorder。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/queryorder")]
    [NonUnify]
    [DisplayName("站点后台提现查单")]
    public async Task<OldApiResult> Queryorder()
    {
        var orderId = OldApiRequestParam.ReadInt(App.HttpContext, "OrderID");
        if (orderId <= 0)
            orderId = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var ct = App.HttpContext?.RequestAborted ?? default;
        var result = await RunAsync(async () => await _orderOperate.QueryGatewayAsync(orderId, ct));
        return result;
    }

    /// <summary>开放渠道首条。对应旧 openchannefirst。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/openchannefirst")]
    [HttpGet("/admin/money/withdraw/openchannefirst")]
    [NonUnify]
    [DisplayName("站点后台开放提现首渠道")]
    public async Task<OldApiResult> Openchannefirst()
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var result = await RunAsync(async () => await _channels.OpenFirstAsync(ct));
        return result;
    }

    /// <summary>开放代付渠道。对应旧 openchannessystem。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/openchannessystem")]
    [HttpGet("/admin/money/withdraw/openchannessystem")]
    [NonUnify]
    [DisplayName("站点后台开放代付渠道")]
    public async Task<OldApiResult> Openchannessystem()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var ct = http?.RequestAborted ?? default;
        var result = await RunAsync(async () =>
        {
            var items = await _channels.ListOpenSystemAsync(ct);
            return OldApiShowResult.SlicePage(
                items.Select(c => new { c.ID, c.Name }),
                pageIndex,
                pageSize);
        });
        return result;
    }

    /// <summary>系统代付渠道列表（编辑弹层 SysID 下拉）。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawChannelPermissionId)]
    [HttpPost("/admin/money/withdraw/channelsys")]
    [HttpGet("/admin/money/withdraw/channelsys")]
    [NonUnify]
    [DisplayName("站点后台系统代付渠道")]
    public Task<OldApiResult> Channelsys() =>
        RunAsync(async () =>
        {
            var ct = App.HttpContext?.RequestAborted ?? default;
            var rows = await _biz.Db.Queryable<SimpX.Net.Data.Entity.Systems.SystemWithdraw>()
                .Where(p => p.IsOpen)
                .OrderBy(p => p.ID)
                .Select(p => new { p.ID, p.Name })
                .Take(500)
                .ToListAsync(ct);
            return OldApiShowResult.OfList(rows);
        });

    /// <summary>渠道详情。对应旧 channelinfo / checkchannelinfo。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawChannelPermissionId)]
    [HttpPost("/admin/money/withdraw/channelinfo")]
    [HttpGet("/admin/money/withdraw/channelinfo")]
    [HttpPost("/admin/money/withdraw/checkchannelinfo")]
    [HttpGet("/admin/money/withdraw/checkchannelinfo")]
    [NonUnify]
    [DisplayName("站点后台提现渠道详情")]
    public async Task<OldApiResult> Channelinfo()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var ct = App.HttpContext?.RequestAborted ?? default;
        var result = await RunAsync(async () =>
        {
            var c = await _channelOperate.InfoAsync(id, ct);
            return new
            {
                c.ID,
                c.SysID,
                c.Name,
                c.IsOpen,
                Cost = c.Cost * 100m,
                MinCost = c.MinCost == 0m ? "" : c.MinCost.ToString("0.00"),
                MaxCost = c.MaxCost == 0m ? "" : c.MaxCost.ToString("0.00"),
                Currency = (byte)c.Currency,
                Sort = c.Sort,
                Setting = c.Setting ?? string.Empty,
                QuerySetting = c.QuerySetting ?? string.Empty
            };
        });
        return result;
    }

    /// <summary>保存渠道。对应旧 savechannel。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawChannelPermissionId)]
    [HttpPost("/admin/money/withdraw/savechannel")]
    [NonUnify]
    [DisplayName("站点后台保存提现渠道")]
    public async Task<OldApiResult> Savechannel()
    {
        var http = App.HttpContext;
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "Cost"), out var costRaw);
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "MinCost"), out var minCost);
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "MaxCost"), out var maxCost);
        var isOpenRaw = OldApiRequestParam.Read(http, "IsOpen");
        var isOpen = isOpenRaw is "1" or "true" or "True";
        var input = new WithdrawChannelSaveInput
        {
            ID = OldApiRequestParam.ReadInt(http, "ID"),
            Name = OldApiRequestParam.Read(http, "Name"),
            SysID = OldApiRequestParam.ReadInt(http, "SysID"),
            IsOpen = isOpen,
            Cost = costRaw / 100m,
            MinCost = minCost,
            MaxCost = maxCost,
            Setting = OldApiRequestParam.Read(http, "Setting") ?? string.Empty,
            QuerySetting = OldApiRequestParam.Read(http, "QuerySetting") ?? string.Empty,
            Currency = OldApiRequestParam.ReadInt(http, "Currency"),
            Sort = (short)OldApiRequestParam.ReadInt(http, "Sort")
        };
        var result = await RunAsync(async () =>
        {
            await _channelOperate.SaveAsync(input, http?.RequestAborted ?? default);
            return null;
        });
        return result;
    }

    /// <summary>删除渠道。对应旧 deletechannel。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawChannelPermissionId)]
    [HttpPost("/admin/money/withdraw/deletechannel")]
    [NonUnify]
    [DisplayName("站点后台删除提现渠道")]
    public async Task<OldApiResult> Deletechannel()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var ct = App.HttpContext?.RequestAborted ?? default;
        var result = await RunAsync(async () =>
        {
            await _channelOperate.DeleteAsync(id, ct);
            return null;
        });
        return result;
    }

    /// <summary>提现关联打码。对应旧 washflowinfo。</summary>
    [AllowAnonymous]
    [OldApiPermission(WithdrawOrderPermissionId)]
    [HttpPost("/admin/money/withdraw/washflowinfo")]
    [HttpGet("/admin/money/withdraw/washflowinfo")]
    [NonUnify]
    [DisplayName("站点后台提现打码信息")]
    public async Task<OldApiResult> Washflowinfo()
    {
        var http = App.HttpContext;
        var form = new Dictionary<string, object?>
        {
            ["OrderID"] = OldApiRequestParam.Read(http, "OrderID"),
            ["UserID"] = OldApiRequestParam.Read(http, "UserID"),
            ["Currency"] = OldApiRequestParam.Read(http, "Currency"),
            ["ID"] = OldApiRequestParam.Read(http, "ID")
        };
        var ct = http?.RequestAborted ?? default;
        var result = await RunAsync(async () => await _orders.WashflowinfoAsync(form, ct));
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

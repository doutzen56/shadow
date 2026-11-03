using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台充值/入款兼容路由：/admin/money/recharge/*（路径与方法名 1:1 旧版）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money-recharge", Order = 32)]
public class MoneyRechargeAppService : IDynamicApiController, ITransient
{
    /// <summary>充值订单权限 GUID。</summary>
    public const string RechargeOrderPermissionId = "9686680019cc4822a180ce23d4179373";

    private readonly BankRechargeCheckService _bank;
    private readonly SitePaymentService _payment;
    private readonly SiteAdminAuthService _adminAuth;
    private readonly RechargeOrderCreateService _create;
    private readonly IBizDbAccessor _biz;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入线下入款审核、通道设定与当前管理员。
    /// </summary>
    public MoneyRechargeAppService(
        BankRechargeCheckService bank,
        SitePaymentService payment,
        SiteAdminAuthService adminAuth,
        RechargeOrderCreateService create,
        IBizDbAccessor biz,
        ICurrentSiteAccessor site)
    {
        _bank = bank;
        _payment = payment;
        _adminAuth = adminAuth;
        _create = create;
        _biz = biz;
        _site = site;
    }

    /// <summary>
    /// 线下入款审核列表。对应旧版 POST /admin/money/recharge/checklist。
    /// </summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/checklist")]
    [HttpGet("/admin/money/recharge/checklist")]
    [NonUnify]
    [DisplayName("站点后台入款审核列表")]
    public async Task<OldApiResult> Checklist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = ReadCheckListInput(http);
        var result = await RunAsync(async ct =>
        {
            var page = await _bank.PageAsync(input, pageIndex, pageSize, ct);
            return OldApiShowResult.OfPage(MapCheckRows(page.Items), page.RecordCount, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>入款单详情。对应旧 checkinfo。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/checkinfo")]
    [HttpGet("/admin/money/recharge/checkinfo")]
    [NonUnify]
    [DisplayName("站点后台入款详情")]
    public async Task<OldApiResult> Checkinfo()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct =>
        {
            var t = await _bank.InfoAsync(id, ct);
            var desc = t.Description ?? string.Empty;
            var parts = desc.Split('|');
            return new
            {
                t.ID,
                t.OrderID,
                t.PayID,
                t.UserID,
                t.UserName,
                t.CreateAt,
                t.CheckAt,
                Status = OldApiMoneyDisplay.FormatCheckStatus(t.Status),
                t.Money,
                t.Paid,
                withdrawCount = t.WithdrawCount,
                PayRemark = t.PayRemark ?? string.Empty,
                t.Name,
                PayOrderID = t.PayOrderID ?? string.Empty,
                Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
                Description = parts.Length > 0 ? parts[0].Trim() : string.Empty,
                DescriptionA = parts.Length > 1 ? parts[1].Trim() : string.Empty,
                Admin = t.AdminName ?? string.Empty
            };
        });
        return result;
    }

    /// <summary>入款通过。对应旧 checkagree。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/checkagree")]
    [NonUnify]
    [DisplayName("站点后台入款通过")]
    public async Task<OldApiResult> Checkagree()
    {
        var http = App.HttpContext;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(http?.RequestAborted ?? default);
        var input = ReadCheckInput(http, BankRechargeCheckService.StatusSuccess);
        var result = await RunAsync(async ct =>
        {
            await _bank.CheckAsync(input, admin?.ID ?? 0, ct);
            return null;
        });
        return result;
    }

    /// <summary>入款拒绝。对应旧 checkreject。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/checkreject")]
    [NonUnify]
    [DisplayName("站点后台入款拒绝")]
    public async Task<OldApiResult> Checkreject()
    {
        var http = App.HttpContext;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(http?.RequestAborted ?? default);
        var input = ReadCheckInput(http, BankRechargeCheckService.StatusFailed);
        var result = await RunAsync(async ct =>
        {
            await _bank.CheckAsync(input, admin?.ID ?? 0, ct);
            return null;
        });
        return result;
    }

    /// <summary>标记处理中。对应旧 checkprocessing。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/checkprocessing")]
    [NonUnify]
    [DisplayName("站点后台入款处理中")]
    public async Task<OldApiResult> Checkprocessing()
    {
        var http = App.HttpContext;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(http?.RequestAborted ?? default);
        // 处理中：只改状态，不走入账；复用 CheckAsync 会拒 3，故直接 Page+Update 走服务内路径不足时用 agree/reject 主路径外的轻量更新
        var id = OldApiRequestParam.ReadInt(http, "ID");
        var desc = OldApiRequestParam.Read(http, "Description");
        var result = await RunAsync(async ct =>
        {
            await _bank.MarkProcessingAsync(id, admin?.ID ?? 0, desc, ct);
            return null;
        });
        return result;
    }

    /// <summary>代客充值建单。对应旧 create。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/create")]
    [NonUnify]
    [DisplayName("站点后台代客充值")]
    public async Task<OldApiResult> Create()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            var userName = OldApiRequestParam.Read(http, "UserName");
            if (string.IsNullOrWhiteSpace(userName))
                throw Oops.Oh("#用户名不存在#");
            var payText = OldApiRequestParam.Read(http, "PayID");
            if (string.IsNullOrWhiteSpace(payText))
                throw Oops.Oh("#请选择充值渠道#");
            if (!int.TryParse(payText, out var payId) || payId <= 0)
                throw Oops.Oh("#请选择充值渠道#");
            _ = decimal.TryParse(OldApiRequestParam.Read(http, "Money"), out var money);
            var remark = $"{OldApiRequestParam.Read(http, "Description")} | {OldApiRequestParam.Read(http, "DescriptionA")}";
            var siteId = _site.RequireSiteId();
            var userIds = await _biz.Db.Queryable<SimpX.Net.Data.Entity.Users.User>()
                .Where(u => u.SiteID == siteId && u.UserName == userName.Trim())
                .Select(u => u.ID)
                .Take(1)
                .ToListAsync(ct);
            var userId = userIds.FirstOrDefault();
            if (userId <= 0)
                throw Oops.Oh("#用户名不存在#");

            var output = await _create.CreateAsync(new RechargeOrderCreateInput
            {
                UserID = userId,
                PayID = payId,
                Money = money,
                ActiveID = "0",
                Remark = remark,
                IsProxyRecharge = true,
                ClientIP = "0.0.0.0"
            }, ct);
            return !string.IsNullOrEmpty(output.OrderID);
        });
        return result;
    }

    /// <summary>充值通道设定列表。对应旧 settinglist。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/settinglist")]
    [HttpGet("/admin/money/recharge/settinglist")]
    [NonUnify]
    [DisplayName("站点后台充值通道设定")]
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

    /// <summary>充值通道分类列表（编辑弹层下拉）。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/settingcate")]
    [HttpGet("/admin/money/recharge/settingcate")]
    [NonUnify]
    [DisplayName("站点后台充值通道分类")]
    public Task<OldApiResult> Settingcate() =>
        RunAsync(async ct =>
        {
            var siteId = _site.RequireSiteId();
            var rows = await _biz.Db.Queryable<SimpX.Net.Data.Entity.Sites.SitePaymentCategory>()
                .Where(c => c.SiteID == siteId)
                .OrderBy(c => c.ID)
                .Select(c => new { c.ID, c.Name })
                .ToListAsync(ct);
            return OldApiShowResult.OfList(rows);
        });

    /// <summary>系统充值渠道列表（编辑弹层 SysID 下拉）。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/settingsys")]
    [HttpGet("/admin/money/recharge/settingsys")]
    [NonUnify]
    [DisplayName("站点后台系统充值渠道")]
    public Task<OldApiResult> Settingsys() =>
        RunAsync(async ct =>
        {
            var rows = await _biz.Db.Queryable<SimpX.Net.Data.Entity.Systems.SystemPayment>()
                .Where(p => p.IsOpen)
                .OrderBy(p => p.ID)
                .Select(p => new { p.ID, p.Name })
                .Take(500)
                .ToListAsync(ct);
            return OldApiShowResult.OfList(rows);
        });

    /// <summary>充值通道详情。对齐旧 settinginfo。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/settinginfo")]
    [HttpGet("/admin/money/recharge/settinginfo")]
    [NonUnify]
    [DisplayName("站点后台充值通道详情")]
    public Task<OldApiResult> Settinginfo() =>
        RunAsync(async ct => await _payment.InfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    /// <summary>保存充值通道。对齐旧 savesetting（主字段）。</summary>
    [AllowAnonymous]
    [OldApiPermission(RechargeOrderPermissionId)]
    [HttpPost("/admin/money/recharge/savesetting")]
    [NonUnify]
    [DisplayName("站点后台保存充值通道")]
    public Task<OldApiResult> Savesetting() =>
        RunAsync(async ct =>
        {
            var http = App.HttpContext;
            await _payment.SaveAsync(new SitePaymentSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                CateID = OldApiFormReader.ReadInt(http, "CateID", "Category"),
                Name = OldApiFormReader.ReadString(http, "Name") ?? string.Empty,
                Remark = OldApiFormReader.ReadString(http, "Remark") ?? string.Empty,
                SysID = OldApiFormReader.ReadInt(http, "SysID", "Payment"),
                MinMoney = decimal.TryParse(OldApiFormReader.ReadString(http, "MinMoney"), out var min) ? min : 0,
                MaxMoney = decimal.TryParse(OldApiFormReader.ReadString(http, "MaxMoney"), out var max) ? max : 0,
                MaxPay = decimal.TryParse(OldApiFormReader.ReadString(http, "MaxPay"), out var maxPay) ? maxPay : 0,
                Status = OldApiFormReader.ReadInt(http, "Status", "IsOpen"),
                Currency = OldApiFormReader.ReadInt(http, "Currency"),
                Sort = (short)OldApiFormReader.ReadInt(http, "Sort"),
                Fee = decimal.TryParse(OldApiFormReader.ReadString(http, "Fee"), out var fee) ? fee : 0,
                Cost = decimal.TryParse(OldApiFormReader.ReadString(http, "Cost"), out var cost) ? cost : 0,
                Setting = OldApiFormReader.ReadString(http, "Setting") ?? string.Empty
            }, ct);
            return (object?)null;
        });

    private static BankRechargeCheckListInput ReadCheckListInput(Microsoft.AspNetCore.Http.HttpContext http)
    {
        var input = new BankRechargeCheckListInput
        {
            Status = OldApiRequestParam.Read(http, "Status"),
            User = OldApiRequestParam.Read(http, "User"),
            Agent = OldApiRequestParam.Read(http, "Agent"),
            OrderID = OldApiRequestParam.Read(http, "OrderID"),
            PayID = OldApiRequestParam.ReadInt(http, "PayID"),
        };
        if (int.TryParse(OldApiRequestParam.Read(http, "Currency"), out var cur) && cur > 0)
            input.Currency = cur;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var startAt))
            input.StartAt = startAt;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var endAt))
            input.EndAt = endAt;
        return input;
    }

    private static BankRechargeCheckInput ReadCheckInput(Microsoft.AspNetCore.Http.HttpContext http, int status)
    {
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "Paid"), out var paid);
        return new BankRechargeCheckInput
        {
            ID = OldApiRequestParam.ReadInt(http, "ID"),
            Status = status,
            Paid = paid,
            SystemID = OldApiRequestParam.Read(http, "SystemID"),
            Description = OldApiRequestParam.Read(http, "Description"),
            DescriptionA = OldApiRequestParam.Read(http, "DescriptionA"),
        };
    }

    private static IEnumerable<object> MapCheckRows(IEnumerable<BankRechargeCheckListItemDto> items) =>
        items.Select(t => new
        {
            t.ID,
            t.OrderID,
            t.PayID,
            PayName = t.PayName ?? string.Empty,
            t.Name,
            t.UserID,
            t.UserName,
            t.CreateAt,
            t.CheckAt,
            Status = OldApiMoneyDisplay.FormatCheckStatus(t.Status),
            t.Money,
            t.Paid,
            Admin = t.AdminName ?? string.Empty,
            t.Description,
            Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency)
        });

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

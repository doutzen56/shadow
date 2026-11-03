using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Systems;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台充值订单只读。对齐旧 /admin/recharge/orderlist 主列表（不含代理树/分类汇总）。
/// </summary>
public sealed partial class RechargeOrderService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入库与定站。
    /// </summary>
    /// <param name="db">库</param>
    /// <param name="site">当前站</param>
    public RechargeOrderService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 分页列出当前站充值订单（排除检测失败单）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<RechargeOrderPageOutput> PageAsync(
        RechargeOrderPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new RechargeOrderPageInput();
        var siteId = _site.RequireSiteId();
        var page = input.Page < 1 ? 1 : input.Page;
        var size = input.PageSize is < 1 or > 200 ? 20 : input.PageSize;

        var query = _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId && !o.IsCheckFail && !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(input.OrderID))
            query = query.Where(o => o.ID == input.OrderID.Trim());
        if (!string.IsNullOrWhiteSpace(input.KeyWord))
        {
            var kw = input.KeyWord.Trim();
            query = query.Where(o =>
                (o.ID != null && o.ID.Contains(kw))
                || (o.SystemID != null && o.SystemID.Contains(kw)));
        }
        if (input.PayID > 0)
            query = query.Where(o => o.PayID == input.PayID);
        if (input.IsPay.HasValue)
            query = query.Where(o => o.IsPay == input.IsPay.Value);
        if (input.IsProxyRecharge.HasValue)
            query = query.Where(o => o.IsProxyRecharge == input.IsProxyRecharge.Value);
        if (input.Currency.HasValue)
            query = query.Where(o => o.Currency == input.Currency.Value);
        if (input.FromCurrency.HasValue)
            query = query.Where(o => o.FromCurrency == input.FromCurrency.Value);
        if (input.MoneyFrom.HasValue)
            query = query.Where(o => o.Money >= input.MoneyFrom.Value);
        if (input.MoneyTo.HasValue)
            query = query.Where(o => o.Money <= input.MoneyTo.Value);
        var byPayAt = string.Equals(input.DateType, "PayAt", StringComparison.OrdinalIgnoreCase);
        if (input.StartAt.HasValue && input.StartAt.Value != default)
        {
            if (byPayAt)
                query = query.Where(o => o.PayAt >= input.StartAt.Value);
            else
                query = query.Where(o => o.CreateAt >= input.StartAt.Value);
        }
        if (input.EndAt.HasValue && input.EndAt.Value != default)
        {
            var endExclusive = input.EndAt.Value.Date.AddDays(1);
            if (byPayAt)
                query = query.Where(o => o.PayAt < endExclusive);
            else
                query = query.Where(o => o.CreateAt < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            if (int.TryParse(name, out var uidExact))
                query = query.Where(o => o.UserID == uidExact);
            else
            {
                var uids = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(name))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
                if (uids.Count == 0)
                    return new RechargeOrderPageOutput { Items = [], Total = 0 };
                query = query.Where(o => uids.Contains(o.UserID));
            }
        }

        if (!string.IsNullOrWhiteSpace(input.Agent))
        {
            var agentName = input.Agent.Trim();
            var agentIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName.Contains(agentName))
                .Select(u => u.ID)
                .Take(500)
                .ToListAsync(cancellationToken);
            if (agentIds.Count == 0)
                return new RechargeOrderPageOutput { Items = [], Total = 0 };
            var childIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && agentIds.Contains(u.AgentID))
                .Select(u => u.ID)
                .ToListAsync(cancellationToken);
            if (childIds.Count == 0)
                return new RechargeOrderPageOutput { Items = [], Total = 0 };
            query = query.Where(o => childIds.Contains(o.UserID));
        }

        RefAsync<int> total = 0;
        var rows = await query
            .OrderBy(o => o.CreateAt, OrderByType.Desc)
            .ToPageListAsync(page, size, total, cancellationToken);

        var userIds = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        var agentIdMap = new Dictionary<int, int>();
        if (userIds.Count > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName, u.AgentID })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
            {
                nameMap[u.ID] = u.UserName ?? string.Empty;
                agentIdMap[u.ID] = u.AgentID;
            }
        }

        var parentIds = agentIdMap.Values.Where(id => id > 0).Distinct().ToList();
        var parentNameMap = new Dictionary<int, string>();
        if (parentIds.Count > 0)
        {
            var parents = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && parentIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var p in parents)
                parentNameMap[p.ID] = p.UserName ?? string.Empty;
        }

        // 支付渠道名 + 第三方分组名（对齐旧 Payment / ThirdParty）
        var payIds = rows.Select(r => r.PayID).Where(id => id > 0).Distinct().ToList();
        var paymentNameMap = new Dictionary<int, string>();
        var thirdPartyMap = new Dictionary<int, string>();
        if (payIds.Count > 0)
        {
            var sitePays = await _db.Queryable<SitePayment>()
                .Where(p => p.SiteID == siteId && payIds.Contains(p.ID))
                .Select(p => new { p.ID, p.Name, p.SysID })
                .ToListAsync(cancellationToken);
            foreach (var p in sitePays)
                paymentNameMap[p.ID] = p.Name ?? string.Empty;
            var sysIds = sitePays.Select(p => p.SysID).Where(id => id > 0).Distinct().ToList();
            if (sysIds.Count > 0)
            {
                var sysPays = await _db.Queryable<SystemPayment>()
                    .Where(s => sysIds.Contains(s.ID))
                    .Select(s => new { s.ID, s.GroupID })
                    .ToListAsync(cancellationToken);
                var groupIds = sysPays.Select(s => s.GroupID).Where(id => id > 0).Distinct().ToList();
                var groupNameMap = new Dictionary<int, string>();
                if (groupIds.Count > 0)
                {
                    var groups = await _db.Queryable<SystemPaymentGroup>()
                        .Where(g => groupIds.Contains(g.ID))
                        .Select(g => new { g.ID, g.Name })
                        .ToListAsync(cancellationToken);
                    foreach (var g in groups)
                        groupNameMap[g.ID] = g.Name ?? string.Empty;
                }
                var sysToGroup = sysPays.ToDictionary(s => s.ID, s => s.GroupID);
                foreach (var p in sitePays)
                {
                    if (sysToGroup.TryGetValue(p.SysID, out var gid) && groupNameMap.TryGetValue(gid, out var gn))
                        thirdPartyMap[p.ID] = gn;
                }
            }
        }

        var adminIds = rows.Select(r => r.AdminID).Where(id => id > 0).Distinct().ToList();
        var adminNameMap = new Dictionary<int, string>();
        if (adminIds.Count > 0)
        {
            var admins = await _db.Queryable<SiteAdmin>()
                .Where(a => a.SiteID == siteId && adminIds.Contains(a.ID))
                .Select(a => new { a.ID, a.AdminName })
                .ToListAsync(cancellationToken);
            foreach (var a in admins)
                adminNameMap[a.ID] = a.AdminName ?? string.Empty;
        }

        return new RechargeOrderPageOutput
        {
            Total = total,
            Items = rows.Select(o =>
            {
                nameMap.TryGetValue(o.UserID, out var un);
                agentIdMap.TryGetValue(o.UserID, out var aid);
                parentNameMap.TryGetValue(aid, out var an);
                paymentNameMap.TryGetValue(o.PayID, out var payName);
                thirdPartyMap.TryGetValue(o.PayID, out var third);
                adminNameMap.TryGetValue(o.AdminID, out var adminName);
                OldApiMoneyDisplay.ParseRechargeRemark(o.Remark, o.Paid, o.Currency, out var displayRemark, out var realPaid);
                return new RechargeOrderListItemDto
                {
                    OrderID = (o.ID ?? string.Empty).Trim(),
                    UserID = o.UserID,
                    UserName = un ?? string.Empty,
                    Money = o.Money,
                    Paid = o.Paid,
                    Fee = o.Fee,
                    PayID = o.PayID,
                    IsPay = o.IsPay,
                    CreateAt = o.CreateAt,
                    PayAt = o.PayAt,
                    Currency = o.Currency,
                    Remark = displayRemark,
                    SystemID = (o.SystemID ?? string.Empty).Trim(),
                    Cost = o.Cost,
                    Count = o.Count,
                    IsProxyRecharge = o.IsProxyRecharge,
                    AgentName = an ?? string.Empty,
                    Admin = !string.IsNullOrEmpty(adminName)
                        ? adminName
                        : (o.AdminID > 0 ? o.AdminID.ToString() : string.Empty),
                    DealTime = FormatDealTime(o.IsPay, o.CreateAt, o.PayAt),
                    Payment = payName ?? string.Empty,
                    ThirdParty = third ?? string.Empty,
                    RealPaid = realPaid
                };
            }).ToList()
        };
    }

    /// <summary>对齐旧 DealTime：到账耗时 H:mm:ss。</summary>
    private static string FormatDealTime(bool isPay, DateTime createAt, DateTime payAt)
    {
        if (!isPay || payAt == default || createAt == default) return string.Empty;
        if (payAt.Year <= 1900) return string.Empty;
        var span = payAt - createAt;
        if (span < TimeSpan.Zero) return string.Empty;
        return $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}";
    }

    // --- merged from RechargeOrderService.WaveGaps.cs ---


    /// <summary>
    /// 对齐旧 exportrechargeorder：返回与列表同结构。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ExportrechargeorderAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ExportrechargeorderCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 orderinfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> OrderinfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return OrderinfoCoreAsync(input, cancellationToken);
    }

    /// <summary>导出：放大页。</summary>
    private async Task<object?> ExportrechargeorderCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        var pageInput = new RechargeOrderPageInput
        {
            Page = 1,
            PageSize = 200,
            OrderID = ReadString(input, "OrderID", "ID", "Id"),
            User = ReadString(input, "User"),
            PayID = ReadInt(input, "PayID"),
            Currency = ReadNullableInt(input, "Currency")
        };
        return await PageAsync(pageInput, cancellationToken);
    }

    /// <summary>充值订单详情。</summary>
    private async Task<object?> OrderinfoCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        var id = ReadString(input, "OrderID", "ID", "Id", "EventID");
        if (string.IsNullOrWhiteSpace(id))
            throw Oops.Oh("#订单不存在#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId && o.ID == id.Trim() && !o.IsDeleted)
            .Take(1)
            .ToListAsync(cancellationToken);
        var o = rows.FirstOrDefault() ?? throw Oops.Oh("#订单不存在#");
        var name = string.Empty;
        if (o.UserID > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.ID == o.UserID)
                .Select(u => u.UserName)
                .Take(1)
                .ToListAsync(cancellationToken);
            name = users.FirstOrDefault() ?? string.Empty;
        }

        var paymentName = string.Empty;
        if (o.PayID > 0)
        {
            var pays = await _db.Queryable<SitePayment>()
                .Where(p => p.SiteID == siteId && p.ID == o.PayID)
                .Select(p => p.Name)
                .Take(1)
                .ToListAsync(cancellationToken);
            paymentName = pays.FirstOrDefault() ?? string.Empty;
        }

        OldApiMoneyDisplay.ParseRechargeRemark(o.Remark, o.Paid, o.Currency, out var displayRemark, out var realPaid);
        return new
        {
            ID = (o.ID ?? string.Empty).Trim(),
            OrderID = (o.ID ?? string.Empty).Trim(),
            o.UserID,
            UserName = name,
            o.Money,
            o.Paid,
            o.Fee,
            o.Cost,
            o.PayID,
            Payment = paymentName,
            o.IsPay,
            o.CreateAt,
            o.PayAt,
            Currency = OldApiMoneyDisplay.FormatCurrency(o.Currency),
            SystemID = (o.SystemID ?? string.Empty).Trim(),
            Remark = displayRemark,
            RealPaid = realPaid,
            o.IsCheckFail,
            o.IsDeleted,
            o.IsProxyRecharge,
            o.Count,
            o.AdminID
        };
    }

    private static int ReadInt(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var n)) return n;
        }
        return 0;
    }

    private static int? ReadNullableInt(Dictionary<string, object?> input, params string[] keys)
    {
        var n = ReadInt(input, keys);
        return n == 0 ? null : n;
    }

    private static string ReadString(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            var s = v.ToString();
            if (!string.IsNullOrWhiteSpace(s))
                return s.Trim();
        }
        return string.Empty;
    }
}

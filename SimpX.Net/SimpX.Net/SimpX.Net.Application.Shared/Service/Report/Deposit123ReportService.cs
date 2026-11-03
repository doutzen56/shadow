using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 存款一二三只读。对齐旧 report/user/deposit（首充/二充/三充）。
/// </summary>
public sealed class Deposit123ReportService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public Deposit123ReportService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 按会员+到账日汇总首二三次充值（最多 500 行）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<Deposit123ItemDto>> ListAsync(
        Deposit123ListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new Deposit123ListInput();
        var siteId = _site.RequireSiteId();
        var start = input.StartAt?.Date ?? DateTime.Today.AddDays(-7);
        var end = (input.EndAt?.Date ?? DateTime.Today).AddDays(1);

        var query = _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId
                && !o.IsDeleted
                && o.IsPay
                && o.Count >= 1
                && o.Count <= 3
                && o.PayAt >= start
                && o.PayAt < end);
        if (input.Currency >= 0)
            query = query.Where(o => o.Currency == input.Currency);

        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var key = input.User.Trim();
            var belong = input.BelongUser is "1" or "true" or "True";
            List<int> userIds;
            if (int.TryParse(key, out var uid))
                userIds = [uid];
            else
            {
                userIds = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(key))
                    .Select(u => u.ID).Take(50).ToListAsync(cancellationToken);
                if (userIds.Count == 0)
                    return [];
            }

            if (belong)
            {
                var roots = userIds.ToList();
                var down = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && roots.Contains(u.AgentID))
                    .Select(u => u.ID)
                    .Take(2000)
                    .ToListAsync(cancellationToken);
                userIds = roots.Concat(down).Distinct().ToList();
            }

            query = query.Where(o => userIds.Contains(o.UserID));
        }

        var orders = await query
            .Select(o => new { o.UserID, o.PayAt, o.Count, o.Money, o.Paid, o.Currency })
            .Take(5000)
            .ToListAsync(cancellationToken);

        var grouped = orders
            .GroupBy(o => new { o.UserID, Date = o.PayAt.Date, o.Currency })
            .Select(g => new Deposit123ItemDto
            {
                UserID = g.Key.UserID,
                Date = g.Key.Date,
                Currency = g.Key.Currency,
                Money1 = g.Where(x => x.Count == 1).Sum(x => x.Money),
                Recharge1 = g.Where(x => x.Count == 1).Sum(x => x.Paid),
                Money2 = g.Where(x => x.Count == 2).Sum(x => x.Money),
                Recharge2 = g.Where(x => x.Count == 2).Sum(x => x.Paid),
                Money3 = g.Where(x => x.Count == 3).Sum(x => x.Money),
                Recharge3 = g.Where(x => x.Count == 3).Sum(x => x.Paid)
            })
            .ToList();

        if (input.RechargeCount == 1)
            grouped = grouped.Where(x => x.Recharge1 > 0).ToList();
        else if (input.RechargeCount == 2)
            grouped = grouped.Where(x => x.Recharge2 > 0).ToList();
        else if (input.RechargeCount == 3)
            grouped = grouped.Where(x => x.Recharge3 > 0).ToList();

        grouped = grouped.OrderByDescending(x => x.Date).Take(500).ToList();

        var ids = grouped.Select(x => x.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var names = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName }).ToListAsync(cancellationToken);
            foreach (var n in names)
                nameMap[n.ID] = n.UserName;
        }

        foreach (var row in grouped)
            row.UserName = nameMap.TryGetValue(row.UserID, out var un) ? un : string.Empty;
        return grouped;
    }
}

/// <summary>存款一二三筛选。</summary>
public sealed class Deposit123ListInput
{
    /// <summary>开始日。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束日。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>用户编号或用户名。</summary>
    public string User { get; set; }

    /// <summary>币种；-1=全部。</summary>
    public int Currency { get; set; } = -1;

    /// <summary>只看第几次充值；0=全部，1/2/3=首二三次。</summary>
    public int RechargeCount { get; set; }

    /// <summary>包含直属下级；传 1/true。</summary>
    public string BelongUser { get; set; }
}

/// <summary>存款一二三行。</summary>
public sealed class Deposit123ItemDto
{
    /// <summary>到账日。</summary>
    public DateTime Date { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>首充提交额。</summary>
    public decimal Money1 { get; set; }

    /// <summary>首充到账额。</summary>
    public decimal Recharge1 { get; set; }

    /// <summary>二充提交额。</summary>
    public decimal Money2 { get; set; }

    /// <summary>二充到账额。</summary>
    public decimal Recharge2 { get; set; }

    /// <summary>三充提交额。</summary>
    public decimal Money3 { get; set; }

    /// <summary>三充到账额。</summary>
    public decimal Recharge3 { get; set; }
}

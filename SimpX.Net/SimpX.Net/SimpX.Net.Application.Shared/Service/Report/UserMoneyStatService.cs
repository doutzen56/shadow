using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员资金日统计汇总（只读）。对齐旧 /report/user/statistics 表格面的简化版。
/// </summary>
public sealed class UserMoneyStatService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public UserMoneyStatService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 按会员汇总区间内金额与笔数。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<UserMoneyStatItemDto>> ListAsync(
        UserMoneyStatListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new UserMoneyStatListInput();
        var siteId = _site.RequireSiteId();
        var start = input.StartAt?.Date ?? DateTime.Today.AddDays(-7);
        var end = (input.EndAt?.Date ?? DateTime.Today).AddDays(1);

        var query = _db.Queryable<UserDateMoney>()
            .Where(m => m.SiteID == siteId && m.Date >= start && m.Date < end);
        if (input.Currency >= 0)
            query = query.Where(m => m.Currency == input.Currency);
        if (input.Type.HasValue)
            query = query.Where(m => m.Type == input.Type.Value);

        int? filterUserId = null;
        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var key = input.User.Trim();
            if (int.TryParse(key, out var uid))
                filterUserId = uid;
            else
            {
                var users = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(key)).Take(50)
                    .Select(u => u.ID).ToListAsync(cancellationToken);
                if (users.Count == 0)
                    return [];
                query = query.Where(m => users.Contains(m.UserID));
            }
        }

        if (filterUserId.HasValue)
            query = query.Where(m => m.UserID == filterUserId.Value);

        var grouped = await query
            .GroupBy(m => new { m.UserID, m.Currency })
            .Select(m => new
            {
                m.UserID,
                m.Currency,
                Money = SqlFunc.AggregateSum(m.Money),
                Count = SqlFunc.AggregateSum(m.Count)
            })
            .Take(500)
            .ToListAsync(cancellationToken);

        var ids = grouped.Select(g => g.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var names = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName }).ToListAsync(cancellationToken);
            foreach (var n in names)
                nameMap[n.ID] = n.UserName;
        }

        return grouped.Select(g => new UserMoneyStatItemDto
        {
            UserID = g.UserID,
            UserName = nameMap.TryGetValue(g.UserID, out var un) ? un : string.Empty,
            Currency = g.Currency,
            Money = g.Money,
            Count = g.Count
        }).OrderByDescending(x => x.Money).ToList();
    }
}

/// <summary>会员统计筛选。</summary>
public sealed class UserMoneyStatListInput
{
    /// <summary>开始日。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束日。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>用户编号或用户名。</summary>
    public string User { get; set; }

    /// <summary>币种；-1=全部。</summary>
    public int Currency { get; set; } = -1;

    /// <summary>资金类型；null=全部。</summary>
    public int? Type { get; set; }
}

/// <summary>会员统计行。</summary>
public sealed class UserMoneyStatItemDto
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>金额合计。</summary>
    public decimal Money { get; set; }

    /// <summary>笔数合计。</summary>
    public int Count { get; set; }
}

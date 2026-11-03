using SimpX.Net.Data.Entity.Reports;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 存款统计只读（按日汇总充值类型）。
/// </summary>
public sealed class SiteDepositStatService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteDepositStatService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 按日汇总充值金额与笔数。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteDepositStatItemDto>> ListAsync(
        SiteFundDailyListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteFundDailyListInput();
        var siteId = _site.RequireSiteId();
        var start = input.StartAt?.Date ?? DateTime.Today.AddDays(-7);
        var end = (input.EndAt?.Date ?? DateTime.Today).AddDays(1);

        var query = _db.Queryable<UserDateMoney>()
            .Where(m => m.SiteID == siteId
                && m.Date >= start
                && m.Date < end
                && m.Type == SiteFundDailyStatService.TypeRecharge);
        if (input.Currency >= 0)
            query = query.Where(m => m.Currency == input.Currency);

        var rows = await query
            .GroupBy(m => new { m.Date, m.Currency })
            .Select(m => new
            {
                m.Date,
                m.Currency,
                Money = SqlFunc.AggregateSum(m.Money),
                Count = SqlFunc.AggregateSum(m.Count),
                UserCount = SqlFunc.AggregateDistinctCount(m.UserID)
            })
            .ToListAsync(cancellationToken);

        return rows.OrderBy(r => r.Date).Select(r => new SiteDepositStatItemDto
        {
            Date = r.Date,
            Currency = r.Currency,
            Money = r.Money,
            Count = r.Count,
            UserCount = r.UserCount
        }).ToList();
    }
}

/// <summary>存款统计行。</summary>
public sealed class SiteDepositStatItemDto
{
    /// <summary>业务日。</summary>
    public DateTime Date { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>充值金额。</summary>
    public decimal Money { get; set; }

    /// <summary>笔数。</summary>
    public int Count { get; set; }

    /// <summary>人数。</summary>
    public int UserCount { get; set; }
}

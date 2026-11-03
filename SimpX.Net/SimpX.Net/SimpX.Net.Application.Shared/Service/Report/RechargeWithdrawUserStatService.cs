using SimpX.Net.Data.Entity.Reports;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充提用户统计只读。对齐旧 rechargeusers 简化版。
/// </summary>
public sealed class RechargeWithdrawUserStatService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public RechargeWithdrawUserStatService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 区间内充提人数与首二三笔人数。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>汇总</returns>
    public async Task<RechargeWithdrawUserStatDto> GetAsync(
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
                && (m.Type == SiteFundDailyStatService.TypeRecharge
                    || m.Type == SiteFundDailyStatService.TypeWithdraw));
        if (input.Currency >= 0)
            query = query.Where(m => m.Currency == input.Currency);

        var rows = await query
            .Select(m => new { m.UserID, m.Type, m.Count })
            .ToListAsync(cancellationToken);

        var recharge = rows.Where(r => r.Type == SiteFundDailyStatService.TypeRecharge)
            .GroupBy(r => r.UserID)
            .Select(g => g.Sum(x => x.Count))
            .ToList();
        var withdraw = rows.Where(r => r.Type == SiteFundDailyStatService.TypeWithdraw)
            .GroupBy(r => r.UserID)
            .Select(g => g.Sum(x => x.Count))
            .ToList();

        return new RechargeWithdrawUserStatDto
        {
            RechargeUserCount = recharge.Count,
            FirRechargeUserCount = recharge.Count(c => c == 1),
            SecRechargeUserCount = recharge.Count(c => c == 2),
            ThrRechargeUserCount = recharge.Count(c => c == 3),
            WithDrawUserCount = withdraw.Count,
            FirWithDrawUserCount = withdraw.Count(c => c == 1),
            SecWithDrawUserCount = withdraw.Count(c => c == 2),
            ThrWithDrawUserCount = withdraw.Count(c => c == 3)
        };
    }
}

/// <summary>充提用户统计。</summary>
public sealed class RechargeWithdrawUserStatDto
{
    /// <summary>充值人数。</summary>
    public int RechargeUserCount { get; set; }

    /// <summary>首充人数（区间内合计 1 笔）。</summary>
    public int FirRechargeUserCount { get; set; }

    /// <summary>二充人数。</summary>
    public int SecRechargeUserCount { get; set; }

    /// <summary>三充人数。</summary>
    public int ThrRechargeUserCount { get; set; }

    /// <summary>提现人数。</summary>
    public int WithDrawUserCount { get; set; }

    /// <summary>首提人数。</summary>
    public int FirWithDrawUserCount { get; set; }

    /// <summary>二提人数。</summary>
    public int SecWithDrawUserCount { get; set; }

    /// <summary>三提人数。</summary>
    public int ThrWithDrawUserCount { get; set; }
}

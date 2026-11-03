using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台首页汇总卡片（只读）。
/// </summary>
public sealed class HomeDashboardService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public HomeDashboardService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 今日注册、充值、提现、投注简要。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>汇总</returns>
    public async Task<HomeDashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var registerCount = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.CreateAt >= today && u.CreateAt < tomorrow)
            .CountAsync(cancellationToken);

        var moneyRows = await _db.Queryable<UserDateMoney>()
            .Where(m => m.SiteID == siteId && m.Date >= today && m.Date < tomorrow
                && (m.Type == SiteFundDailyStatService.TypeRecharge
                    || m.Type == SiteFundDailyStatService.TypeWithdraw))
            .Select(m => new { m.Type, m.Money })
            .ToListAsync(cancellationToken);

        var bet = await _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= today && g.Date < tomorrow)
            .Select(g => new { g.BetMoney, g.Money })
            .ToListAsync(cancellationToken);

        return new HomeDashboardDto
        {
            Date = today,
            RegisterCount = registerCount,
            RechargeMoney = moneyRows.Where(x => x.Type == SiteFundDailyStatService.TypeRecharge).Sum(x => x.Money),
            WithdrawMoney = moneyRows.Where(x => x.Type == SiteFundDailyStatService.TypeWithdraw).Sum(x => x.Money),
            BetMoney = bet.Sum(x => x.BetMoney),
            GameMoney = bet.Sum(x => x.Money)
        };
    }
}

/// <summary>首页汇总。</summary>
public sealed class HomeDashboardDto
{
    /// <summary>业务日。</summary>
    public DateTime Date { get; set; }

    /// <summary>今日注册。</summary>
    public int RegisterCount { get; set; }

    /// <summary>今日充值。</summary>
    public decimal RechargeMoney { get; set; }

    /// <summary>今日提现。</summary>
    public decimal WithdrawMoney { get; set; }

    /// <summary>今日投注。</summary>
    public decimal BetMoney { get; set; }

    /// <summary>今日游戏盈亏。</summary>
    public decimal GameMoney { get; set; }
}

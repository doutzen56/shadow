using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点币种折算到基础币（对齐旧 ExchangeRateAgent.GetbaseCurrency + Point）。
/// </summary>
public static class SiteCurrencyFx
{
    /// <summary>
    /// 取站点基础币。对齐旧 SiteCaching.GetSiteBaseCurrency：用 Site.Currency。
    /// </summary>
    public static async Task<int> GetBaseCurrencyAsync(
        ISqlSugarClient db,
        int siteId,
        CancellationToken cancellationToken = default)
    {
        var sites = await db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Select(s => s.Currency)
            .Take(1)
            .ToListAsync(cancellationToken);
        return sites.FirstOrDefault();
    }

    /// <summary>
    /// 加载 BaseCurrency=base 的兑换点数字典（ExchangeCurrency → Point）。
    /// 对齐旧 loglist：不过滤 Status；ExchangeCurrency=0 也是合法币种，不能跳过。
    /// </summary>
    public static async Task<Dictionary<int, decimal>> LoadPointsAsync(
        ISqlSugarClient db,
        int siteId,
        int baseCurrency,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.Queryable<SiteExchangeRate>()
            .Where(r => r.SiteID == siteId && r.BaseCurrency == baseCurrency)
            .Select(r => new { r.ExchangeCurrency, r.Point })
            .ToListAsync(cancellationToken);
        var map = new Dictionary<int, decimal>();
        foreach (var r in rows)
        {
            // Point<=0 时旧站 First().Point 仍会除；这里用 1 避免除零
            var point = r.Point == 0 ? 1m : r.Point;
            map[r.ExchangeCurrency] = point;
        }

        return map;
    }

    /// <summary>
    /// 折算到基础币。对齐旧：rate==0 时 money/Point；rateLocked（带了 Currency 筛选）时除以 1。
    /// 币种等于基础币时除以 1；否则取 ExchangeCurrency 对应 Point（含币种 0）。
    /// </summary>
    public static decimal ToBase(
        decimal money,
        int currency,
        int baseCurrency,
        IReadOnlyDictionary<int, decimal> points,
        bool rateLocked)
    {
        if (rateLocked || currency == baseCurrency)
            return money;
        if (!points.TryGetValue(currency, out var point) || point == 0)
            return money;
        return money / point;
    }
}

using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台打赏报表只读。对齐旧版 rewarddate 聚合字段；数据源用 SQL game_RewardLog（非 ES）。
/// </summary>
public sealed class TipReportService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。打赏记录在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public TipReportService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 打赏汇总列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行与合计</returns>
    public async Task<TipReportListOutput> ListAsync(
        TipReportListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();

        var startAt = DateTime.Now.Date;
        if (!string.IsNullOrWhiteSpace(input.StartAt) && DateTime.TryParse(input.StartAt.Trim(), out var sa))
            startAt = sa.Date;
        var endExclusive = startAt.AddDays(1);
        if (!string.IsNullOrWhiteSpace(input.EndAt) && DateTime.TryParse(input.EndAt.Trim(), out var ea))
            endExclusive = ea.Date.AddDays(1);

        var query = _db.Queryable<RewardLog>()
            .Where(t => t.SiteID == siteId && t.RewardAt >= startAt && t.RewardAt < endExclusive);

        var isChangeCurrency = true;
        List<SiteExchangeRate> siteCurrencys = new();
        if (!string.IsNullOrWhiteSpace(input.Currency) && int.TryParse(input.Currency.Trim(), out var currency))
        {
            query = query.Where(t => t.Currency == currency);
            isChangeCurrency = false;
        }
        else
        {
            var baseCurrency = await ResolveBaseCurrencyAsync(siteId, cancellationToken);
            siteCurrencys = await _db.Queryable<SiteExchangeRate>()
                .Where(r => r.SiteID == siteId && r.BaseCurrency == baseCurrency)
                .ToListAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(input.TradeType) && int.TryParse(input.TradeType.Trim(), out var tradeType))
            query = query.Where(t => t.TradeType == tradeType);

        var gameTypes = ParseIntCsv(input.GameType);
        if (gameTypes.Count > 0)
        {
            query = query.Where(t => gameTypes.Contains(t.Type));
        }
        else
        {
            var settingsQ = _db.Queryable<GameSetting>().Where(g => g.SiteID == siteId);
            if (!string.IsNullOrEmpty(input.IsOpen) &&
                (input.IsOpen == "1" || input.IsOpen.Equals("true", StringComparison.OrdinalIgnoreCase)))
                settingsQ = settingsQ.Where(g => g.IsOpen);
            var openTypes = await settingsQ.Select(g => g.Game).ToListAsync(cancellationToken);
            if (openTypes.Count > 0)
                query = query.Where(t => openTypes.Contains(t.Type));
        }

        var rows = await query.ToListAsync(cancellationToken);
        var gameSettings = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var rateMap = gameSettings.ToDictionary(g => string.Concat(g.SiteID, g.Game), g => g.Rate);
        var nameMap = gameSettings
            .Where(g => !string.IsNullOrEmpty(g.Name))
            .GroupBy(g => g.Game)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var billlist = rows
            .GroupBy(t => new { t.Type, t.Currency, t.SiteID, t.TradeType })
            .Select(g => new
            {
                g.Key.Type,
                g.Key.Currency,
                g.Key.SiteID,
                g.Key.TradeType,
                Money = g.Sum(x => x.TradeAmount),
                Members = g.Select(x => x.UserID).Distinct().Count()
            })
            .OrderBy(t => t.Type)
            .ToList();

        TipReportSummaryDto summary = null;
        if (billlist.Count > 0)
        {
            var converted = billlist.Select(t =>
            {
                var rate = !isChangeCurrency
                    ? 1m
                    : siteCurrencys.FirstOrDefault(p => p.ExchangeCurrency == t.Currency)?.Point ?? 1m;
                if (rate == 0m) rate = 1m;
                return new
                {
                    Money = t.Money / rate,
                    t.Members
                };
            }).ToList();

            summary = new TipReportSummaryDto
            {
                Money = Math.Round(converted.Sum(p => p.Money), 2),
                Members = converted.Sum(p => p.Members)
            };
        }

        var missing = billlist.Select(b => b.Type).Distinct().Where(t => !nameMap.ContainsKey(t)).ToList();
        if (missing.Count > 0)
        {
            var sys = await _db.Queryable<SystemGameSetting>()
                .Where(s => missing.Contains(s.Type))
                .ToListAsync(cancellationToken);
            foreach (var s in sys)
            {
                if (!nameMap.ContainsKey(s.Type) && !string.IsNullOrEmpty(s.Name))
                    nameMap[s.Type] = s.Name;
            }
        }

        var data = billlist.Select(t =>
        {
            rateMap.TryGetValue(string.Concat(t.SiteID, t.Type), out var rate);
            return new TipReportListItemDto
            {
                Type = t.Type,
                Game = nameMap.TryGetValue(t.Type, out var n) && !string.IsNullOrEmpty(n) ? n : t.Type.ToString(),
                Members = t.Members,
                Money = Math.Round(t.Money, 2),
                Rate = rate,
                TradeType = t.TradeType,
                Currency = t.Currency
            };
        }).ToList();

        return new TipReportListOutput { Data = data, Summary = summary };
    }

    /// <summary>
    /// 基准币。
    /// </summary>
    private async Task<int> ResolveBaseCurrencyAsync(int siteId, CancellationToken cancellationToken)
    {
        var baseRow = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.SiteID == siteId && r.IsBase)
            .OrderBy(r => r.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        if (baseRow.Count > 0)
            return baseRow[0].BaseCurrency;
        var any = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.SiteID == siteId)
            .OrderBy(r => r.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        return any.Count > 0 ? any[0].BaseCurrency : 0;
    }

    /// <summary>
    /// 解析逗号分隔整型。
    /// </summary>
    private static List<int> ParseIntCsv(string text)
    {
        var list = new List<int>();
        if (string.IsNullOrWhiteSpace(text))
            return list;
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var v))
                list.Add(v);
        }

        return list;
    }
}

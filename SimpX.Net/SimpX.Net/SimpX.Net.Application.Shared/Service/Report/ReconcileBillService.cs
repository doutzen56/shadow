using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台对账报表只读。对齐旧版 /admin/report/bill/billdate 核心聚合；不写库。
/// GameUETimeZone 过滤本波不追求完整等价。
/// </summary>
public sealed class ReconcileBillService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。对账数据在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public ReconcileBillService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 对账汇总列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行与合计</returns>
    public async Task<ReconcileBillListOutput> ListAsync(
        ReconcileBillListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();

        var query = _db.Queryable<UserDateGameView>().Where(t => t.SiteID == siteId);

        var gameTypes = ParseIntCsv(input.GameType);
        if (gameTypes.Count > 0)
        {
            query = query.Where(t => gameTypes.Contains(t.Type));
        }
        else
        {
            var settingsQ = _db.Queryable<GameSetting>().Where(g => g.SiteID == siteId);
            if (!string.IsNullOrEmpty(input.IsOpen) && (input.IsOpen == "1" || input.IsOpen.Equals("true", StringComparison.OrdinalIgnoreCase)))
                settingsQ = settingsQ.Where(g => g.IsOpen);
            var openTypes = await settingsQ.Select(g => g.Game).ToListAsync(cancellationToken);
            if (openTypes.Count > 0)
                query = query.Where(t => openTypes.Contains(t.Type));
        }

        if (!string.IsNullOrWhiteSpace(input.StartAt) && DateTime.TryParse(input.StartAt.Trim(), out var startAt))
            query = query.Where(t => t.Date >= startAt.Date);

        if (!string.IsNullOrWhiteSpace(input.EndAt) && DateTime.TryParse(input.EndAt.Trim(), out var endAt))
        {
            var endExclusive = endAt.Date.AddDays(1);
            query = query.Where(t => t.Date < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(input.Category) && int.TryParse(input.Category.Trim(), out var category))
            query = query.Where(t => t.Category == category);

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

        var timeType = 1;
        if (!string.IsNullOrWhiteSpace(input.TimeType) && int.TryParse(input.TimeType.Trim(), out var tt))
            timeType = tt;
        query = query.Where(t => t.TimeType == timeType);

        var dataList = await query.ToListAsync(cancellationToken);

        var gameSettings = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var rateMap = gameSettings.ToDictionary(g => string.Concat(g.SiteID, g.Game), g => g.Rate);
        var nameMap = gameSettings
            .Where(g => !string.IsNullOrEmpty(g.Name))
            .GroupBy(g => g.Game)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var billlist = dataList
            .GroupBy(t => new { t.SiteID, t.Type, t.Currency })
            .Select(t => new
            {
                t.Key.SiteID,
                t.Key.Type,
                t.Key.Currency,
                OrderCount = (int?)t.Sum(p => p.OrderCount),
                BetMoney = t.Sum(p => p.BetMoney),
                BetAmount = t.Sum(p => p.BetAmount),
                Money = t.Sum(p => p.Money),
                JackPot = t.Sum(p => p.JackPot),
                JackBetCount = t.Sum(p => p.JackPotCount),
                Members = t.Select(p => p.UserID).Distinct().Count()
            })
            .OrderBy(t => t.Type)
            .ToList();

        ReconcileBillSummaryDto summary = null;
        if (billlist.Count > 0)
        {
            var converted = billlist.GroupBy(t => new { t.Currency, t.Type, t.SiteID }).Select(t =>
            {
                var rate = !isChangeCurrency
                    ? 1m
                    : siteCurrencys.FirstOrDefault(p => p.ExchangeCurrency == t.Key.Currency)?.Point ?? 1m;
                if (rate == 0m) rate = 1m;
                rateMap.TryGetValue(string.Concat(t.Key.SiteID, t.Key.Type), out var rateGame);
                return new
                {
                    BetMoney = t.Sum(p => p.BetMoney / rate),
                    BetAmount = t.Sum(p => p.BetAmount / rate),
                    Money = t.Sum(p => p.Money / rate),
                    JackPot = t.Sum(p => p.JackPot),
                    JackPotCount = t.Sum(p => p.JackBetCount),
                    PayMoney = t.Sum(p =>
                    {
                        var raw = p.Money + p.JackPot;
                        return (raw > 0m ? 0m : Math.Abs(raw * rateGame)) / rate;
                    }),
                    OrderCount = t.Sum(p => p.OrderCount),
                    Members = t.Sum(p => p.Members)
                };
            }).ToList();

            summary = new ReconcileBillSummaryDto
            {
                BetMoney = Math.Round(converted.Sum(p => p.BetMoney), 2),
                BetAmount = Math.Round(converted.Sum(p => p.BetAmount), 2),
                Money = Math.Round(converted.Sum(p => p.Money), 2),
                PayMoney = Math.Round(converted.Sum(p => p.PayMoney), 2),
                JackPot = converted.Sum(p => p.JackPot),
                JackPotCount = converted.Sum(p => p.JackPotCount),
                OrderCount = converted.Sum(p => p.OrderCount),
                Members = converted.Sum(p => p.Members)
            };
        }

        var missingNames = billlist.Select(b => b.Type).Distinct().Where(t => !nameMap.ContainsKey(t)).ToList();
        if (missingNames.Count > 0)
        {
            var sys = await _db.Queryable<SystemGameSetting>()
                .Where(s => missingNames.Contains(s.Type))
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
            var payRaw = t.Money + t.JackPot;
            return new ReconcileBillListItemDto
            {
                Type = t.Type,
                Game = nameMap.TryGetValue(t.Type, out var n) && !string.IsNullOrEmpty(n) ? n : t.Type.ToString(),
                BetMoney = Math.Round(t.BetMoney, 2),
                BetAmount = Math.Round(t.BetAmount, 2),
                OrderCount = t.OrderCount,
                Members = t.Members,
                Money = Math.Round(t.Money, 2),
                Rate = rate,
                Pay = payRaw > 0m ? 0m : Math.Round(Math.Abs(payRaw * rate), 2),
                Currency = t.Currency,
                JackPot = t.JackPot,
                JackPotCount = t.JackBetCount
            };
        }).ToList();

        return new ReconcileBillListOutput { Data = data, Summary = summary };
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

using System.Text.Json;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台交收表只读。对齐旧版 /admin/report/bill/list 聚合；不写库。
/// </summary>
public sealed class SettlementBillService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。交收数据在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SettlementBillService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 交收汇总列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行与合计</returns>
    public async Task<SettlementBillListOutput> ListAsync(
        SettlementBillListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();

        var query = _db.Queryable<UserDateGameView>().Where(t => t.SiteID == siteId);

        if (!string.IsNullOrWhiteSpace(input.Game) && int.TryParse(input.Game.Trim(), out var gameType))
            query = query.Where(t => t.Type == gameType);

        if (!string.IsNullOrWhiteSpace(input.Month))
        {
            var monthText = input.Month.Trim() + "-1";
            if (DateTime.TryParse(monthText, out var startAt))
            {
                var endAt = startAt.AddMonths(1);
                query = query.Where(t => t.Date >= startAt && t.Date < endAt);
            }
        }

        if (!string.IsNullOrWhiteSpace(input.Category) && int.TryParse(input.Category.Trim(), out var category))
            query = query.Where(t => t.Category == category);

        var isAll = true;
        List<SiteExchangeRate> siteCurrencys = new();
        if (!string.IsNullOrWhiteSpace(input.Currency) && int.TryParse(input.Currency.Trim(), out var currency))
        {
            query = query.Where(t => t.Currency == currency);
            isAll = false;
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
        var rateMap = await BuildRateMapAsync(siteId, cancellationToken);

        var fallbackRate = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var fallbackMap = fallbackRate.ToDictionary(g => g.Game, g => g.Rate);

        var billlist = dataList
            .GroupBy(t => new { t.SiteID, t.Type, t.Category, t.Currency })
            .Select(t =>
            {
                var key = string.Concat(t.Key.SiteID, t.Key.Type, t.Key.Category, t.Key.Currency);
                if (!rateMap.TryGetValue(key, out var rate))
                    fallbackMap.TryGetValue(t.Key.Type, out rate);
                return new
                {
                    t.Key.SiteID,
                    t.Key.Type,
                    t.Key.Category,
                    t.Key.Currency,
                    Rate = rate,
                    OrderCount = (int?)t.Sum(p => p.OrderCount),
                    BetMoney = t.Sum(p => p.BetMoney),
                    BetAmount = t.Sum(p => p.BetAmount),
                    Money = t.Sum(p => p.Money),
                    JackPot = t.Sum(p => p.JackPot),
                    PayMoney = t.Sum(p => p.Money + p.JackPot),
                    JackPotCount = t.Sum(p => p.JackPotCount)
                };
            })
            .ToList();

        var billGroup = billlist
            .GroupBy(t => new { t.SiteID, t.Type, t.Currency, t.Rate })
            .Select(t => new
            {
                t.Key.SiteID,
                t.Key.Type,
                t.Key.Currency,
                t.Key.Rate,
                Category = string.Join(",", t.Select(p => p.Category.ToString())),
                OrderCount = t.Sum(p => p.OrderCount),
                BetMoney = t.Sum(p => p.BetMoney),
                BetAmount = t.Sum(p => p.BetAmount),
                Money = t.Sum(p => p.Money),
                JackPot = t.Sum(p => p.JackPot),
                PayMoney = t.Sum(p => p.PayMoney) > 0m ? 0m : Math.Abs(t.Sum(p => p.PayMoney) * t.Key.Rate),
                JackPotCount = t.Sum(p => p.JackPotCount)
            })
            .OrderBy(t => t.Type)
            .ToList();

        SettlementBillSummaryDto summary = null;
        if (billGroup.Count > 0)
        {
            var byCur = billGroup.GroupBy(t => t.Currency).Select(t =>
            {
                decimal rate = !isAll ? 1m : siteCurrencys.FirstOrDefault(p => p.ExchangeCurrency == t.Key)?.Point ?? 1m;
                if (rate == 0m) rate = 1m;
                return new
                {
                    BetMoney = t.Sum(p => p.BetMoney / rate),
                    BetAmount = t.Sum(p => p.BetAmount / rate),
                    Money = t.Sum(p => p.Money / rate),
                    JackPot = t.Sum(p => p.JackPot / rate),
                    PayMoney = t.Sum(p => p.PayMoney / rate),
                    OrderCount = t.Sum(p => p.OrderCount),
                    JackPotCount = t.Sum(p => p.JackPotCount / rate)
                };
            }).ToList();

            summary = new SettlementBillSummaryDto
            {
                BetMoney = Math.Round(byCur.Sum(p => p.BetMoney), 2),
                BetAmount = Math.Round(byCur.Sum(p => p.BetAmount), 2),
                Money = Math.Round(byCur.Sum(p => p.Money), 2),
                JackPot = byCur.Sum(p => p.JackPot),
                PayMoney = Math.Round(byCur.Sum(p => p.PayMoney), 2),
                OrderCount = byCur.Sum(p => p.OrderCount),
                JackPotCount = byCur.Sum(p => p.JackPotCount)
            };
        }

        var nameMap = await LoadGameNamesAsync(siteId, billGroup.Select(b => b.Type).Distinct().ToList(), cancellationToken);

        var data = billGroup.Select(t => new SettlementBillListItemDto
        {
            Type = t.Type,
            Game = nameMap.TryGetValue(t.Type, out var n) && !string.IsNullOrEmpty(n) ? n : t.Type.ToString(),
            BetMoney = Math.Round(t.BetMoney, 2),
            BetAmount = Math.Round(t.BetAmount, 2),
            Money = Math.Round(t.Money, 2),
            JackPot = t.JackPot,
            Rate = t.Rate,
            Pay = Math.Round(t.PayMoney, 2),
            Currency = t.Currency,
            Category = t.Category,
            JackPotCount = t.JackPotCount
        }).ToList();

        return new SettlementBillListOutput { Data = data, Summary = summary };
    }

    /// <summary>
    /// 构建 SiteID+Type+Category+Currency → Rate。
    /// </summary>
    private async Task<Dictionary<string, decimal>> BuildRateMapAsync(int siteId, CancellationToken cancellationToken)
    {
        var settings = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var sysRows = await _db.Queryable<SystemGameSetting>().ToListAsync(cancellationToken);
        var sysMap = sysRows.ToDictionary(s => s.Type, s => s);

        var dic = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var item in settings)
        {
            if (!sysMap.TryGetValue(item.Game, out var sys))
                continue;

            var categories = ParseIntCsv(sys.GameCategorys);
            var currencies = ParseIntCsv(sys.Currencys);
            var details = ParseDetailRates(item.DetailRate);

            foreach (var cat in categories)
            {
                foreach (var cur in currencies)
                {
                    var rate = item.Rate;
                    var catDetails = details.Where(d => d.Category == cat).ToList();
                    if (catDetails.Count > 0)
                    {
                        var allCur = catDetails.FirstOrDefault(d => d.Currency == -1);
                        if (allCur != null)
                            rate = allCur.Rate;
                        else
                        {
                            var exact = catDetails.FirstOrDefault(d => d.Currency == cur);
                            if (exact != null)
                                rate = exact.Rate;
                        }
                    }

                    dic[string.Concat(item.SiteID, item.Game, cat, cur)] = rate;
                }
            }
        }

        return dic;
    }

    /// <summary>
    /// 游戏展示名。
    /// </summary>
    private async Task<Dictionary<int, string>> LoadGameNamesAsync(
        int siteId,
        List<int> types,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, string>();
        if (types == null || types.Count == 0)
            return map;

        var siteRows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && types.Contains(g.Game))
            .ToListAsync(cancellationToken);
        foreach (var g in siteRows)
        {
            if (!string.IsNullOrEmpty(g.Name))
                map[g.Game] = g.Name;
        }

        var missing = types.Where(t => !map.ContainsKey(t)).ToList();
        if (missing.Count == 0)
            return map;

        var sys = await _db.Queryable<SystemGameSetting>()
            .Where(s => missing.Contains(s.Type))
            .ToListAsync(cancellationToken);
        foreach (var s in sys)
        {
            if (!map.ContainsKey(s.Type) && !string.IsNullOrEmpty(s.Name))
                map[s.Type] = s.Name;
        }

        return map;
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
    /// 解析逗号分隔整型（枚举名无法解析时跳过）。
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

    /// <summary>
    /// 解析 DetailRate JSON。
    /// </summary>
    private static List<DetailRateRow> ParseDetailRates(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<DetailRateRow>();
        try
        {
            return JsonSerializer.Deserialize<List<DetailRateRow>>(json) ?? new List<DetailRateRow>();
        }
        catch (JsonException)
        {
            return new List<DetailRateRow>();
        }
    }

    /// <summary>
    /// DetailRate 一项。
    /// </summary>
    private sealed class DetailRateRow
    {
        /// <summary>分类。</summary>
        public int Category { get; set; }

        /// <summary>币种；-1 表示全币种。</summary>
        public int Currency { get; set; }

        /// <summary>点位。</summary>
        public decimal Rate { get; set; }
    }
}

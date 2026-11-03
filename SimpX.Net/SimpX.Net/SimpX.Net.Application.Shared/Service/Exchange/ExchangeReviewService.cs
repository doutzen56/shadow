using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台兑币审核与汇兑总账。对齐旧版 /admin/exchange/review/list|log|check|summary。
/// </summary>
public sealed class ExchangeReviewService : ITransient
{
    /// <summary>待处理。</summary>
    public const int StatusNone = 0;

    /// <summary>失败。</summary>
    public const int StatusFailed = 1;

    /// <summary>成功。</summary>
    public const int StatusSuccess = 2;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;

    /// <summary>
    /// 注入业务库、当前站与资金账变。兑币订单在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="money">资金账变</param>
    public ExchangeReviewService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IMoneyLedger money)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
    }

    /// <summary>
    /// 待审核列表（Status=0）。对齐旧 list。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<ExchangeReviewListItemDto>> ListPendingAsync(
        ExchangeReviewListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<CurrencyExchangeOrder>()
            .Where(p => p.SiteID == siteId && p.Status == StatusNone);

        if (input.StartAt.HasValue && input.StartAt.Value != default)
            query = query.Where(p => p.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue && input.EndAt.Value != default)
            query = query.Where(p => p.CreateAt <= input.EndAt.Value);
        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(p => p.UserName == input.UserName.Trim());
        if (TryParseCurrency(input.Currency, out var currency))
            query = query.Where(p => p.BeforeCurrency == currency);

        var list = await query
            .OrderBy(p => p.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return list.Select(MapPending).ToList();
    }

    /// <summary>
    /// 已审核列表（Status≠0）。对齐旧 log。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<ExchangeReviewListItemDto>> ListLogAsync(
        ExchangeReviewLogInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<CurrencyExchangeOrder>()
            .Where(p => p.SiteID == siteId && p.Status != StatusNone);

        var byCreate = input.TimeType == 1;
        if (input.StartAt.HasValue && input.StartAt.Value != default)
        {
            query = byCreate
                ? query.Where(p => p.CreateAt >= input.StartAt.Value)
                : query.Where(p => p.CheckTime >= input.StartAt.Value);
        }

        if (input.EndAt.HasValue && input.EndAt.Value != default)
        {
            query = byCreate
                ? query.Where(p => p.CreateAt <= input.EndAt.Value)
                : query.Where(p => p.CheckTime <= input.EndAt.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(p => p.UserName == input.UserName.Trim());
        if (TryParseCurrency(input.Currency, out var currency))
            query = query.Where(p => p.BeforeCurrency == currency);

        var list = await query
            .OrderBy(p => p.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var nameMap = await LoadCheckerNamesAsync(siteId, list, cancellationToken);
        return list.Select(p => MapLog(p, nameMap)).ToList();
    }

    /// <summary>
    /// 汇兑总账。对齐旧 summary：只统计成功单，按日+兑入币种汇总金额与人数。
    /// </summary>
    /// <param name="input">日期区间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按日与合计</returns>
    public async Task<ExchangeSummaryOutput> SummaryAsync(
        ExchangeSummaryInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var startAt = (input.StartAt ?? DateTime.Now).Date;
        var endExclusive = (input.EndAt ?? DateTime.Now).Date.AddDays(1);

        var openCurrencies = await _db.Queryable<SiteCurrency>()
            .Where(c => c.SiteID == siteId && c.IsOpen)
            .OrderBy(c => c.Sort)
            .Select(c => c.Currency)
            .ToListAsync(cancellationToken);

        var orders = await _db.Queryable<CurrencyExchangeOrder>()
            .Where(t => t.SiteID == siteId
                && t.CreateAt >= startAt
                && t.CreateAt < endExclusive
                && t.Status == StatusSuccess)
            .ToListAsync(cancellationToken);

        var groups = orders
            .GroupBy(t => new { Date = t.CreateAt.Date, t.AfterCurrency })
            .Select(g => new
            {
                g.Key.Date,
                g.Key.AfterCurrency,
                Money = g.Sum(x => x.AfterMoney),
                Count = g.Select(x => x.UserID).Distinct().Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        const string totalKey = "TOTAL";
        const string noFeeKey = "NOFEE";
        var total = openCurrencies.ToDictionary(c => c.ToString(), _ => 0m);
        foreach (var c in openCurrencies)
            total[c + "Count"] = 0m;
        total[totalKey] = 0m;
        total[noFeeKey] = 0m;

        var dic = new SortedDictionary<DateTime, Dictionary<string, decimal>>();
        foreach (var item in groups)
        {
            if (!dic.TryGetValue(item.Date, out var day))
            {
                day = new Dictionary<string, decimal>();
                dic[item.Date] = day;
            }

            var curKey = item.AfterCurrency.ToString();
            day[curKey] = item.Money;
            day[curKey + "Count"] = item.Count;
            if (!day.ContainsKey(totalKey))
                day[totalKey] = 0m;
            if (!day.ContainsKey(noFeeKey))
                day[noFeeKey] = 0m;
            day[totalKey] += item.Money;
            total[totalKey] += item.Money;

            if (!total.ContainsKey(curKey))
                total[curKey] = 0m;
            if (!total.ContainsKey(curKey + "Count"))
                total[curKey + "Count"] = 0m;
            total[curKey] += item.Money;
            total[curKey + "Count"] += item.Count;
        }

        return new ExchangeSummaryOutput
        {
            Items = dic.Select(kv => new ExchangeSummaryDayDto
            {
                Date = kv.Key,
                Data = kv.Value
            }).ToList(),
            Total = total
        };
    }

    /// <summary>
    /// 审核兑币。对齐旧 check：先解锁，再更新状态；成功则兑出/兑入两笔账变。
    /// </summary>
    /// <param name="input">审核入参</param>
    /// <param name="checkerId">审核人站点管理员编号；无会话传 0</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task CheckAsync(
        ExchangeReviewCheckInput input,
        int checkerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#错误#");
        if (input.Status is not (StatusFailed or StatusSuccess))
            throw Oops.Oh("#状态错误#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<CurrencyExchangeOrder>()
            .Where(p => p.SiteID == siteId && p.ID == input.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var model = rows.FirstOrDefault()
            ?? throw Oops.Oh("#错误#");
        if (model.Status != StatusNone)
            throw Oops.Oh("#订单已审核#");

        var orderId = model.OrderID ?? string.Empty;
        if (orderId.Length < 9)
            throw Oops.Oh("#订单号错误#");
        var sourceId = int.Parse(orderId.Substring(orderId.Length - 9, 9));

        model.Status = input.Status;
        model.Remark = input.Remark ?? string.Empty;
        model.CheckTime = DateTime.Now;
        model.Checker = checkerId;

        await _db.Ado.BeginTranAsync();
        try
        {
            await _money.UnlockReviewAsync(
                siteId,
                model.UserID,
                sourceId,
                "unlock Currency Money",
                model.BeforeCurrency,
                cancellationToken);

            var updated = await _db.Updateable(model)
                .UpdateColumns(o => new { o.Status, o.Remark, o.CheckTime, o.Checker })
                .ExecuteCommandAsync(cancellationToken);
            if (updated != 1)
                throw Oops.Oh("#更新失败#");

            if (input.Status == StatusSuccess)
            {
                await _money.AddMoneyLogAsync(
                    siteId,
                    model.UserID,
                    model.Money * -1m,
                    MoneyLedgerService.MoneyTypeExchangeOut,
                    orderId,
                    $"{model.BeforeCurrency} to {model.AfterCurrency}",
                    model.BeforeCurrency,
                    cancellationToken);
                await _money.AddMoneyLogAsync(
                    siteId,
                    model.UserID,
                    model.AfterMoney,
                    MoneyLedgerService.MoneyTypeExchangeIn,
                    orderId,
                    $"{model.AfterCurrency} from {model.BeforeCurrency}",
                    model.AfterCurrency,
                    cancellationToken);
            }

            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// 加载审核人姓名。
    /// </summary>
    private async Task<Dictionary<int, string>> LoadCheckerNamesAsync(
        int siteId,
        List<CurrencyExchangeOrder> list,
        CancellationToken cancellationToken)
    {
        var checkerIds = list
            .Where(p => p.Checker.HasValue && p.Checker.Value > 0)
            .Select(p => p.Checker!.Value)
            .Distinct()
            .ToList();
        var nameMap = new Dictionary<int, string>();
        if (checkerIds.Count == 0)
            return nameMap;

        var admins = await _db.Queryable<SiteAdmin>()
            .Where(a => a.SiteID == siteId && checkerIds.Contains(a.ID))
            .Select(a => new { a.ID, a.AdminName })
            .ToListAsync(cancellationToken);
        foreach (var a in admins)
            nameMap[a.ID] = a.AdminName ?? string.Empty;
        return nameMap;
    }

    /// <summary>
    /// 待审行映射。
    /// </summary>
    private static ExchangeReviewListItemDto MapPending(CurrencyExchangeOrder p) => new()
    {
        ID = p.ID,
        OrderID = p.OrderID ?? string.Empty,
        CreateAt = p.CreateAt,
        UserName = p.UserName ?? string.Empty,
        BeforeCurrency = p.BeforeCurrency,
        BeforeMoney = p.BeforeMoney,
        AfterCurrency = p.AfterCurrency,
        AfterMoney = p.AfterMoney,
        ExchangeMoney = p.ExchangeMoney,
        Money = p.Money,
        Status = p.Status,
        Point = p.Point
    };

    /// <summary>
    /// 已审行映射。
    /// </summary>
    private static ExchangeReviewListItemDto MapLog(
        CurrencyExchangeOrder p,
        IReadOnlyDictionary<int, string> nameMap)
    {
        var checker = string.Empty;
        if (p.Checker.HasValue && p.Checker.Value > 0)
            nameMap.TryGetValue(p.Checker.Value, out checker);
        return new ExchangeReviewListItemDto
        {
            ID = p.ID,
            OrderID = p.OrderID ?? string.Empty,
            CreateAt = p.CreateAt,
            UserName = p.UserName ?? string.Empty,
            BeforeCurrency = p.BeforeCurrency,
            BeforeMoney = p.BeforeMoney,
            AfterCurrency = p.AfterCurrency,
            AfterMoney = p.AfterMoney,
            ExchangeMoney = p.ExchangeMoney,
            Money = p.Money,
            Status = p.Status,
            Point = p.Point,
            CheckTime = p.CheckTime,
            Checker = checker ?? string.Empty
        };
    }

    /// <summary>
    /// 解析币种筛选项。
    /// </summary>
    private static bool TryParseCurrency(string raw, out int currency)
    {
        currency = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        return int.TryParse(raw.Trim(), out currency);
    }
}

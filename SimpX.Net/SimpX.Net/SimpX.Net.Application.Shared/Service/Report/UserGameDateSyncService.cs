using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 同步用户游戏日报表。对齐旧 SyncUserGameDateJob → SaveUserDateGameFromES / Battle。
/// </summary>
public interface IUserGameDateSyncService
{
    /// <summary>
    /// 跑一轮：BJ 主表 + Proxy + EST/UTC 时区表 + Battle；含跨天回补。
    /// </summary>
    Task<UserGameDateSyncResult> SyncAsync(CancellationToken cancellationToken = default);
}

/// <summary>同步结果摘要。</summary>
public sealed class UserGameDateSyncResult
{
    /// <summary>各表写入合计（upsert 影响行近似）。</summary>
    public int Upserted { get; set; }

    /// <summary>主表。</summary>
    public int MainUpserted { get; set; }

    /// <summary>代理表。</summary>
    public int ProxyUpserted { get; set; }

    /// <summary>时区表。</summary>
    public int TimeZoneUpserted { get; set; }

    /// <summary>对战表。</summary>
    public int BattleUpserted { get; set; }

    /// <summary>ES 未配置。</summary>
    public bool NotConfigured { get; set; }

    /// <summary>说明。</summary>
    public string Message { get; set; }
}

/// <summary>
/// 默认实现：Schedule 不定站，全站桶直接落库。
/// 不挂 ITransient：依赖 <see cref="IUserGameDateEsAggregate"/>，只在 Schedule 显式注册。
/// </summary>
public sealed class UserGameDateSyncService : IUserGameDateSyncService
{
    /// <summary>信誉网会员。</summary>
    public const int UserTypeProxyMember = 2;

    /// <summary>信誉网代理。</summary>
    public const int UserTypeProxyAgent = 4;

    private readonly ISqlSugarClient _db;
    private readonly IUserGameDateEsAggregate _es;
    private readonly ILogger<UserGameDateSyncService> _logger;

    public UserGameDateSyncService(
        IBizDbAccessor biz,
        IUserGameDateEsAggregate es,
        ILogger<UserGameDateSyncService> logger)
    {
        _db = biz.Db;
        _es = es;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserGameDateSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var result = new UserGameDateSyncResult();
        var (bjStart, bjEnd, _) = ReportGameDateTimeZones.Beijing();
        var (estStart, estEnd, _) = ReportGameDateTimeZones.Est();
        var (utcStart, utcEnd, _) = ReportGameDateTimeZones.Utc();

        // 探活：未配置 ES 直接返回
        var probe = await _es.AggregateAsync(bjStart, bjEnd, null, cancellationToken);
        if (probe.NotConfigured)
        {
            result.NotConfigured = true;
            result.Message = "ES 业务检索未配置，跳过用户游戏日结";
            return result;
        }

        var proxyIds = await LoadProxyUserIdsAsync(cancellationToken);

        // —— 当日 ——
        // BJ 非代理 → 主表
        result.MainUpserted += await SyncMainAsync(bjStart, bjEnd, proxyIds, isProxy: false, cancellationToken);
        // BJ 对战（旧 Job 当日误传 end=start 导致空窗；这里按 [start,end) 真同步）
        result.BattleUpserted += await SyncBattleAsync(bjStart, bjEnd, cancellationToken);
        // EST 代理 → Proxy 表
        result.ProxyUpserted += await SyncProxyAsync(estStart, estEnd, proxyIds, cancellationToken);
        // EST 非代理 → 时区表
        result.TimeZoneUpserted += await SyncTimeZoneAsync(
            estStart, estEnd, proxyIds, ReportGameDateTimeZone.EST, cancellationToken);
        // UTC 非代理 → 时区表
        result.TimeZoneUpserted += await SyncTimeZoneAsync(
            utcStart, utcEnd, proxyIds, ReportGameDateTimeZone.UTC, cancellationToken);

        // —— 跨天回补 ——
        var beijingLocal = DateTime.UtcNow.AddHours(8);
        var easternLocal = DateTime.UtcNow.AddHours(-4);
        var utcLocal = DateTime.UtcNow;

        if (ReportGameDateTimeZones.InCrossDayWindow(beijingLocal))
        {
            result.MainUpserted += await SyncMainAsync(
                bjStart.AddDays(-1), bjEnd.AddDays(-1), proxyIds, isProxy: false, cancellationToken);
            result.BattleUpserted += await SyncBattleAsync(
                bjStart.AddDays(-1), bjEnd.AddDays(-1), cancellationToken);
        }

        if (ReportGameDateTimeZones.InCrossDayWindow(easternLocal))
        {
            result.ProxyUpserted += await SyncProxyAsync(
                estStart.AddDays(-1), estEnd.AddDays(-1), proxyIds, cancellationToken);
            result.TimeZoneUpserted += await SyncTimeZoneAsync(
                estStart.AddDays(-1), estEnd.AddDays(-1), proxyIds, ReportGameDateTimeZone.EST, cancellationToken);
            result.BattleUpserted += await SyncBattleAsync(
                estStart.AddDays(-1), estEnd.AddDays(-1), cancellationToken);
        }

        if (ReportGameDateTimeZones.InCrossDayWindow(utcLocal))
        {
            result.TimeZoneUpserted += await SyncTimeZoneAsync(
                utcStart.AddDays(-1), utcEnd.AddDays(-1), proxyIds, ReportGameDateTimeZone.UTC, cancellationToken);
            result.BattleUpserted += await SyncBattleAsync(
                utcStart.AddDays(-1), utcEnd.AddDays(-1), cancellationToken);
        }

        result.Upserted = result.MainUpserted + result.ProxyUpserted + result.TimeZoneUpserted + result.BattleUpserted;
        result.Message =
            $"用户游戏日结 main≈{result.MainUpserted} proxy≈{result.ProxyUpserted} tz≈{result.TimeZoneUpserted} battle≈{result.BattleUpserted}";
        return result;
    }

    private async Task<int> SyncMainAsync(
        DateTime startBj,
        DateTime endBj,
        HashSet<int> proxyIds,
        bool isProxy,
        CancellationToken cancellationToken)
    {
        var agg = await _es.AggregateAsync(startBj, endBj, null, cancellationToken);
        if (agg.NotConfigured || agg.QueryFailed || agg.Buckets == null || agg.Buckets.Count == 0)
        {
            if (agg.QueryFailed)
                _logger.LogWarning("主表日结 ES 失败：{Msg}", agg.Message);
            return 0;
        }

        var reportDate = startBj.Date;
        var rows = FilterBuckets(agg.Buckets, proxyIds, keepProxy: isProxy)
            .Select(b => ToMainRow(b, reportDate))
            .GroupBy(r => new { r.SiteID, r.UserID, r.Date, r.Type, r.Category, r.Currency })
            .Select(g =>
            {
                var first = g.First();
                first.BetMoney = g.Sum(x => x.BetMoney);
                first.Money = g.Sum(x => x.Money);
                return first;
            })
            .ToList();
        return await UpsertBatchesAsync(rows, r => new { r.SiteID, r.UserID, r.Date, r.Type, r.Category, r.Currency }, cancellationToken);
    }

    private async Task<int> SyncProxyAsync(
        DateTime startBj,
        DateTime endBj,
        HashSet<int> proxyIds,
        CancellationToken cancellationToken)
    {
        var agg = await _es.AggregateAsync(startBj, endBj, null, cancellationToken);
        if (agg.NotConfigured || agg.QueryFailed || agg.Buckets == null || agg.Buckets.Count == 0)
            return 0;

        var reportDate = startBj.Date;
        var rows = FilterBuckets(agg.Buckets, proxyIds, keepProxy: true)
            .Select(b => new UserDateGameProxy
            {
                SiteID = b.SiteID,
                UserID = b.UserID,
                Date = reportDate,
                Type = b.Type,
                Category = b.Category,
                Currency = b.Currency,
                BetMoney = EffectiveBet(b),
                Money = b.Money,
                OrderCount = b.OrderCount
            })
            .GroupBy(r => new { r.SiteID, r.UserID, r.Date, r.Type, r.Category, r.Currency })
            .Select(g =>
            {
                var first = g.First();
                first.BetMoney = g.Sum(x => x.BetMoney);
                first.Money = g.Sum(x => x.Money);
                first.OrderCount = g.Sum(x => x.OrderCount);
                return first;
            })
            .ToList();
        return await UpsertBatchesAsync(rows, r => new { r.SiteID, r.UserID, r.Date, r.Type, r.Category, r.Currency }, cancellationToken);
    }

    private async Task<int> SyncTimeZoneAsync(
        DateTime startBj,
        DateTime endBj,
        HashSet<int> proxyIds,
        ReportGameDateTimeZone zone,
        CancellationToken cancellationToken)
    {
        var typeNames = await LoadGameTypeNamesAsync(zone, cancellationToken);
        var agg = await _es.AggregateAsync(startBj, endBj, typeNames, cancellationToken);
        if (agg.NotConfigured || agg.QueryFailed || agg.Buckets == null || agg.Buckets.Count == 0)
            return 0;

        var reportDate = startBj.Date;
        var timeType = ReportGameDateTimeZones.ToTimeType(zone);
        var rows = FilterBuckets(agg.Buckets, proxyIds, keepProxy: false)
            .Select(b => new UserDateGameUETimeZone
            {
                SiteID = b.SiteID,
                UserID = b.UserID,
                Date = reportDate,
                Type = b.Type,
                Category = b.Category,
                Currency = b.Currency,
                TimeType = timeType,
                BetMoney = EffectiveBet(b),
                Money = b.Money,
                OrderCount = b.OrderCount
            })
            .GroupBy(r => new { r.SiteID, r.UserID, r.Date, r.Type, r.Category, r.Currency, r.TimeType })
            .Select(g =>
            {
                var first = g.First();
                first.BetMoney = g.Sum(x => x.BetMoney);
                first.Money = g.Sum(x => x.Money);
                first.OrderCount = g.Sum(x => x.OrderCount);
                return first;
            })
            .ToList();
        return await UpsertBatchesAsync(
            rows,
            r => new { r.SiteID, r.UserID, r.Date, r.Type, r.Category, r.Currency, r.TimeType },
            cancellationToken);
    }

    private async Task<int> SyncBattleAsync(DateTime startBj, DateTime endBj, CancellationToken cancellationToken)
    {
        var agg = await _es.AggregateBattleAsync(startBj, endBj, cancellationToken);
        if (agg.NotConfigured || agg.QueryFailed || agg.Buckets == null || agg.Buckets.Count == 0)
            return 0;

        var reportDate = startBj.Date;
        var rows = agg.Buckets
            .Where(b => b.UserID > 0 && b.SiteID > 0)
            .Select(b => new UserDateGameBattle
            {
                SiteID = b.SiteID,
                UserID = b.UserID,
                Date = reportDate,
                Type = b.Type,
                Category = b.Category > 0 ? b.Category : GameOrderSummaryService.CategoryFantasy,
                Currency = b.Currency,
                BetMoney = EffectiveBet(b),
                Money = b.Money,
                OrderCount = b.OrderCount
            })
            .GroupBy(r => new { r.SiteID, r.UserID, r.Date, r.Type, r.Category, r.Currency })
            .Select(g =>
            {
                var first = g.First();
                first.BetMoney = g.Sum(x => x.BetMoney);
                first.Money = g.Sum(x => x.Money);
                first.OrderCount = g.Sum(x => x.OrderCount);
                return first;
            })
            .ToList();
        return await UpsertBatchesAsync(rows, r => new { r.SiteID, r.UserID, r.Date, r.Type, r.Category, r.Currency }, cancellationToken);
    }

    private async Task<IReadOnlyCollection<string>> LoadGameTypeNamesAsync(
        ReportGameDateTimeZone zone,
        CancellationToken cancellationToken)
    {
        var hour = ReportGameDateTimeZones.GameTypeHour(zone);
        if (hour <= 0)
            return Array.Empty<string>();
        try
        {
            var names = await _db.Queryable<GameUETimeZone>()
                .Where(s => s.Hour == hour)
                .Select(s => s.GameName)
                .ToListAsync(cancellationToken);
            return names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读 game_UETimeZone 失败 Hour={Hour}", hour);
            return Array.Empty<string>();
        }
    }

    private async Task<HashSet<int>> LoadProxyUserIdsAsync(CancellationToken cancellationToken)
    {
        var ids = await _db.Queryable<User>()
            .Where(u => u.Type == UserTypeProxyMember || u.Type == UserTypeProxyAgent)
            .Select(u => u.ID)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    private static IEnumerable<UserGameDateEsBucket> FilterBuckets(
        IEnumerable<UserGameDateEsBucket> buckets,
        HashSet<int> proxyIds,
        bool keepProxy) =>
        buckets.Where(b => b.UserID > 0 && b.SiteID > 0 && (keepProxy ? proxyIds.Contains(b.UserID) : !proxyIds.Contains(b.UserID)));

    private static decimal EffectiveBet(UserGameDateEsBucket b) =>
        b.BetAmount != 0 ? b.BetAmount : b.BetMoney;

    private static UserDateGame ToMainRow(UserGameDateEsBucket b, DateTime reportDate) => new()
    {
        SiteID = b.SiteID,
        UserID = b.UserID,
        Date = reportDate,
        Type = b.Type,
        Category = b.Category,
        Currency = b.Currency,
        BetMoney = EffectiveBet(b),
        Money = b.Money
    };

    private async Task<int> UpsertBatchesAsync<T>(
        List<T> rows,
        System.Linq.Expressions.Expression<Func<T, object>> whereColumns,
        CancellationToken cancellationToken) where T : class, new()
    {
        if (rows == null || rows.Count == 0)
            return 0;
        const int batchSize = 100;
        var affected = 0;
        for (var i = 0; i < rows.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = rows.Skip(i).Take(batchSize).ToList();
            try
            {
                affected += await _db.Storageable(batch).WhereColumns(whereColumns).ExecuteCommandAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "upsert {Type} 失败 batch={I}", typeof(T).Name, i / batchSize);
            }
        }

        return affected;
    }
}

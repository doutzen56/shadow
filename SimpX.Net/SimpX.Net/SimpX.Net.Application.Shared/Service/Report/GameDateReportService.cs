using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏日报表简化表。对齐旧 chart 类页面的表格面。
/// </summary>
public sealed partial class GameDateReportService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public GameDateReportService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 按日+游戏类型汇总投注与盈亏。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<GameDateReportItemDto>> ListAsync(
        GameDateReportListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new GameDateReportListInput();
        var siteId = _site.RequireSiteId();
        var start = input.StartAt?.Date ?? DateTime.Today.AddDays(-7);
        var end = (input.EndAt?.Date ?? DateTime.Today).AddDays(1);

        var query = _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= start && g.Date < end);
        if (input.Currency >= 0)
            query = query.Where(g => g.Currency == input.Currency);
        if (input.Type > 0)
            query = query.Where(g => g.Type == input.Type);
        if (input.Category > 0)
            query = query.Where(g => g.Category == input.Category);
        if (input.UserID > 0)
            query = query.Where(g => g.UserID == input.UserID);

        var rows = await query
            .GroupBy(g => new { g.Date, g.Type, g.Category, g.Currency })
            .Select(g => new
            {
                g.Date,
                g.Type,
                g.Category,
                g.Currency,
                BetMoney = SqlFunc.AggregateSum(g.BetMoney),
                Money = SqlFunc.AggregateSum(g.Money)
            })
            .Take(500)
            .ToListAsync(cancellationToken);

        return rows.OrderByDescending(r => r.Date).Select(r => new GameDateReportItemDto
        {
            Date = r.Date,
            Type = r.Type,
            Category = r.Category,
            Currency = r.Currency,
            BetMoney = r.BetMoney,
            Money = r.Money
        }).ToList();
    }

    /// <summary>
    /// 注册留存简化：按注册日统计人数，以及区间内有充值的人数。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<RegisterRetainItemDto>> ListRegisterRetainAsync(
        SiteFundDailyListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteFundDailyListInput();
        var siteId = _site.RequireSiteId();
        var start = input.StartAt?.Date ?? DateTime.Today.AddDays(-7);
        var end = (input.EndAt?.Date ?? DateTime.Today).AddDays(1);

        var regs = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.CreateAt >= start && u.CreateAt < end)
            .Select(u => new { u.ID, u.CreateAt })
            .ToListAsync(cancellationToken);

        var userIds = regs.Select(r => r.ID).ToList();
        var rechargeUsers = new HashSet<int>();
        if (userIds.Count > 0)
        {
            var paid = await _db.Queryable<UserDateMoney>()
                .Where(m => m.SiteID == siteId
                    && m.Type == SiteFundDailyStatService.TypeRecharge
                    && userIds.Contains(m.UserID))
                .Select(m => m.UserID)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in paid)
                rechargeUsers.Add(id);
        }

        return regs.GroupBy(r => r.CreateAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new RegisterRetainItemDto
            {
                Date = g.Key,
                RegisterCount = g.Count(),
                RechargeUserCount = g.Count(x => rechargeUsers.Contains(x.ID))
            }).ToList();
    }

    // --- merged from GameDateReportService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 agentstatistics。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> AgentstatisticsAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return AgentstatisticsCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 fields。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> FieldsAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return FieldsCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 info。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> InfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return InfoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 siteview。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SiteviewAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SiteviewCoreAsync(input, cancellationToken);
    }


    /// <summary>代理按日统计。数据在 rpt_AgentStatistics。</summary>
    private async Task<object?> AgentstatisticsCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var (start, end) = ReadRange(input);
        var query = _db.Queryable<ReportAgentStatis>()
            .Where(a => a.SiteID == siteId && a.Date >= start && a.Date < end);
        if (TryReadInt(input, out var userId, "UserID", "UserId") && userId > 0)
            query = query.Where(a => a.UserID == userId);
        if (TryReadInt(input, out var currency, "Currency"))
            query = query.Where(a => a.Currency == currency);
        var rows = await query
            .OrderBy(a => a.Date, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.UserID).Distinct().ToList();
        var names = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
                names[u.ID] = u.UserName ?? string.Empty;
        }

        return rows.Select(a => new
        {
            a.Date,
            a.UserID,
            UserName = names.TryGetValue(a.UserID, out var n) ? n : string.Empty,
            a.Currency,
            a.GameMoney,
            a.MoneyEvent,
            a.MoneyRebate,
            a.RechargeFee,
            a.WithdrawFee
        }).ToList();
    }

    /// <summary>游戏日报列。页面用来画表头，不再返回空数组。</summary>
    private Task<object?> FieldsCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        _ = cancellationToken;
        return Task.FromResult<object?>(new[]
        {
            new { Field = "Date", Title = "日期" },
            new { Field = "Type", Title = "游戏类型" },
            new { Field = "Category", Title = "游戏分类" },
            new { Field = "Currency", Title = "币种" },
            new { Field = "BetMoney", Title = "投注" },
            new { Field = "Money", Title = "盈亏" }
        });
    }

    /// <summary>某个会员某一天的游戏汇总。没有 UserID 时返回该日全站合计。</summary>
    private async Task<object?> InfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var day = (ReadDate(input, "Date", "StartAt") ?? DateTime.Today).Date;
        var next = day.AddDays(1);
        var userId = 0;
        var query = _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= day && g.Date < next);
        if (TryReadInt(input, out userId, "UserID", "UserId", "ID", "Id") && userId > 0)
            query = query.Where(g => g.UserID == userId);
        var rows = await query.ToListAsync(cancellationToken);
        return new
        {
            Date = day,
            UserID = userId,
            BetMoney = rows.Sum(g => g.BetMoney),
            Money = rows.Sum(g => g.Money),
            Items = rows.Select(g => new
            {
                g.UserID,
                g.Type,
                g.Category,
                g.Currency,
                g.BetMoney,
                g.Money
            }).ToList()
        };
    }

    /// <summary>全站区间投注与盈亏。</summary>
    private async Task<object?> SiteviewCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var (start, end) = ReadRange(input);
        var rows = await _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= start && g.Date < end)
            .ToListAsync(cancellationToken);
        return new
        {
            StartAt = start,
            EndAt = end,
            BetMoney = rows.Sum(g => g.BetMoney),
            Money = rows.Sum(g => g.Money),
            UserCount = rows.Select(g => g.UserID).Distinct().Count()
        };
    }


    /// <summary>表单里明确带了整数才当筛选。</summary>
    /// <param name="input">表单</param>
    /// <param name="value">读到的值</param>
    /// <param name="keys">候选键</param>
    /// <returns>是否读到</returns>
    private static bool TryReadInt(Dictionary<string, object?> input, out int value, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is int i)
            {
                value = i;
                return true;
            }

            if (int.TryParse(v.ToString(), out var n))
            {
                value = n;
                return true;
            }
        }

        value = 0;
        return false;
    }

    /// <summary>按键读时间。</summary>
    private static DateTime? ReadDate(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is DateTime dt)
                return dt;
            if (DateTime.TryParse(v.ToString(), out var parsed))
                return parsed;
        }

        return null;
    }

    /// <summary>默认近 7 天，结束日含当天。</summary>
    private static (DateTime Start, DateTime EndExclusive) ReadRange(Dictionary<string, object?> input)
    {
        var start = (ReadDate(input, "StartAt", "Date") ?? DateTime.Today.AddDays(-6)).Date;
        var endDay = (ReadDate(input, "EndAt", "EndDate") ?? DateTime.Today).Date;
        if (endDay < start)
            endDay = start;
        return (start, endDay.AddDays(1));
    }
}

/// <summary>游戏日报筛选。</summary>
public sealed class GameDateReportListInput
{
    /// <summary>开始。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>币种；-1=全部。</summary>
    public int Currency { get; set; } = -1;

    /// <summary>游戏类型；0=全部。</summary>
    public int Type { get; set; }

    /// <summary>分类；0=全部。</summary>
    public int Category { get; set; }

    /// <summary>会员；0=全部。</summary>
    public int UserID { get; set; }
}

/// <summary>游戏日报行。</summary>
public sealed class GameDateReportItemDto
{
    /// <summary>业务日。</summary>
    public DateTime Date { get; set; }

    /// <summary>游戏类型。</summary>
    public int Type { get; set; }

    /// <summary>分类。</summary>
    public int Category { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>投注。</summary>
    public decimal BetMoney { get; set; }

    /// <summary>盈亏。</summary>
    public decimal Money { get; set; }
}

/// <summary>注册留存行。</summary>
public sealed class RegisterRetainItemDto
{
    /// <summary>注册日。</summary>
    public DateTime Date { get; set; }

    /// <summary>注册人数。</summary>
    public int RegisterCount { get; set; }

    /// <summary>其中有过充值的人数。</summary>
    public int RechargeUserCount { get; set; }
}

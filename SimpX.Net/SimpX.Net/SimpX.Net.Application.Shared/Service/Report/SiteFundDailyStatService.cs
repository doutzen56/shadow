using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Systems;

using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点充提/盈亏日统计（只读）。充提用固定类型；盈亏用 sys_MoneyTypeIsProfit。
/// </summary>
public sealed partial class SiteFundDailyStatService : ITransient
{
    /// <summary>充值。</summary>
    public const int TypeRecharge = 1;

    /// <summary>提现。</summary>
    public const int TypeWithdraw = 2;

    /// <summary>提现手续费。</summary>
    public const int TypeWithdrawFee = 4;

    /// <summary>充值手续费。</summary>
    public const int TypeRechargeFee = 6;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteFundDailyStatService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 充提按日汇总。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteFundDailyItemDto>> ListRechargeWithdrawAsync(
        SiteFundDailyListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteFundDailyListInput();
        var siteId = _site.RequireSiteId();
        var start = input.StartAt?.Date ?? DateTime.Today.AddDays(-7);
        var end = (input.EndAt?.Date ?? DateTime.Today).AddDays(1);
        var types = new[] { TypeRecharge, TypeWithdraw, TypeWithdrawFee, TypeRechargeFee };

        var query = _db.Queryable<UserDateMoney>()
            .Where(m => m.SiteID == siteId && m.Date >= start && m.Date < end && types.Contains(m.Type));
        if (input.Currency >= 0)
            query = query.Where(m => m.Currency == input.Currency);

        var rows = await query
            .GroupBy(m => new { m.Date, m.Type, m.Currency })
            .Select(m => new
            {
                m.Date,
                m.Type,
                m.Currency,
                Money = SqlFunc.AggregateSum(m.Money),
                Count = SqlFunc.AggregateSum(m.Count)
            })
            .ToListAsync(cancellationToken);

        return rows.OrderBy(r => r.Date).ThenBy(r => r.Type)
            .Select(r => new SiteFundDailyItemDto
            {
                Date = r.Date,
                Type = r.Type,
                Currency = r.Currency,
                Money = r.Money,
                Count = r.Count,
                TypeName = TypeLabel(r.Type)
            }).ToList();
    }

    /// <summary>
    /// 盈亏按日汇总（IsProfit=true 的资金类型）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteFundDailyItemDto>> ListProfitAsync(
        SiteFundDailyListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteFundDailyListInput();
        var siteId = _site.RequireSiteId();
        var start = input.StartAt?.Date ?? DateTime.Today.AddDays(-7);
        var end = (input.EndAt?.Date ?? DateTime.Today).AddDays(1);

        var profitTypes = await _db.Queryable<MoneyTypeIsProfit>()
            .Where(t => t.IsProfit)
            .Select(t => t.Type)
            .ToListAsync(cancellationToken);
        if (profitTypes.Count == 0)
            return [];

        var query = _db.Queryable<UserDateMoney>()
            .Where(m => m.SiteID == siteId && m.Date >= start && m.Date < end && profitTypes.Contains(m.Type));
        if (input.Currency >= 0)
            query = query.Where(m => m.Currency == input.Currency);

        var rows = await query
            .GroupBy(m => new { m.Date, m.Type, m.Currency })
            .Select(m => new
            {
                m.Date,
                m.Type,
                m.Currency,
                Money = SqlFunc.AggregateSum(m.Money),
                Count = SqlFunc.AggregateSum(m.Count)
            })
            .ToListAsync(cancellationToken);

        return rows.OrderBy(r => r.Date).ThenBy(r => r.Type)
            .Select(r => new SiteFundDailyItemDto
            {
                Date = r.Date,
                Type = r.Type,
                Currency = r.Currency,
                Money = r.Money,
                Count = r.Count,
                TypeName = $"类型{r.Type}"
            }).ToList();
    }

    private static string TypeLabel(int type) => type switch
    {
        TypeRecharge => "充值",
        TypeWithdraw => "提现",
        TypeWithdrawFee => "提现手续费",
        TypeRechargeFee => "充值手续费",
        _ => $"类型{type}"
    };

    // --- merged from SiteFundDailyStatService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 activeusers。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ActiveusersAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ActiveusersCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 betamount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> BetamountAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return BetamountCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 chartlist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ChartlistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ChartlistCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 gamecreditlog。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GamecreditlogAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GamecreditlogCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 getplatformbalance。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetplatformbalanceAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetplatformbalanceCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 ordercount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> OrdercountAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return OrdercountCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 profitofindex。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ProfitofindexAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ProfitofindexCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 rebateactive。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> RebateactiveAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return RebateactiveCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 rechargeandwitdraw。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> RechargeandwitdrawAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return RechargeandwitdrawCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 rechargeusers。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> RechargeusersAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return RechargeusersCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 rechargeusersdetails。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> RechargeusersdetailsAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return RechargeusersdetailsCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 systemdatareport。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SystemdatareportAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SystemdatareportCoreAsync(input, cancellationToken);
    }


    /// <summary>区间内有登录的会员人数。数据在登录日志。</summary>
    private async Task<object?> ActiveusersCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var (start, end) = ReadRange(input);
        var ids = await _db.Queryable<LoginLog>()
            .Where(l => l.SiteID == siteId && l.LoginAt >= start && l.LoginAt < end)
            .Select(l => l.UserID)
            .Distinct()
            .ToListAsync(cancellationToken);
        return new { StartAt = start, EndAt = end, Count = ids.Count };
    }

    /// <summary>区间投注额。数据在会员游戏日汇总。</summary>
    private async Task<object?> BetamountCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var rows = await LoadGamesAsync(input, cancellationToken);
        return new
        {
            BetMoney = rows.Sum(g => g.BetMoney),
            Money = rows.Sum(g => g.Money),
            Count = rows.Count
        };
    }

    /// <summary>按日的投注和盈亏，给图表用。没有行就返回空数组，不再回显入参。</summary>
    private async Task<object?> ChartlistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var rows = await LoadGamesAsync(input, cancellationToken);
        return rows
            .GroupBy(g => g.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                BetMoney = g.Sum(x => x.BetMoney),
                Money = g.Sum(x => x.Money)
            })
            .ToList();
    }

    /// <summary>站点授信额度流水。数据在 site_GameCreditLog。</summary>
    private async Task<object?> GamecreditlogCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var (start, end) = ReadRange(input);
        var rows = await _db.Queryable<SiteGameCreditLog>()
            .Where(l => l.SiteID == siteId && l.CreateAt >= start && l.CreateAt < end)
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .Take(200)
            .ToListAsync(cancellationToken);
        return rows.Select(l => new
        {
            l.ID,
            l.UserID,
            l.UserName,
            l.Money,
            l.GameCredit,
            l.CreateAt,
            l.TransferType,
            l.GameType,
            l.LogDesc
        }).ToList();
    }

    /// <summary>
    /// 厂商余额。没有厂商查询客户端，不编余额。
    /// </summary>
    private Task<object?> GetplatformbalanceCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        _ = cancellationToken;
        return Task.FromResult<object?>(new
        {
            Supported = false,
            Items = Array.Empty<object>(),
            Message = "#没有厂商余额客户端#"
        });
    }

    /// <summary>当前待审、待出款、付款中的提现，以及未到账充值。这是队列，不按默认日期砍掉。</summary>
    private async Task<object?> OrdercountCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var hasDate = HasDate(input);
        var start = DateTime.MinValue;
        var end = DateTime.MaxValue;
        if (hasDate)
            (start, end) = ReadRange(input);

        var pending = await CountWithdrawAsync(siteId, WithdrawOrderOperateService.StatusNone, hasDate, start, end, cancellationToken);
        var waitPay = await CountWithdrawAsync(siteId, WithdrawOrderOperateService.StatusWait, hasDate, start, end, cancellationToken);
        var paying = await CountWithdrawAsync(siteId, WithdrawOrderOperateService.StatusInPayment, hasDate, start, end, cancellationToken);
        var recharge = _db.Queryable<RechargeOrder>().Where(o => o.SiteID == siteId && !o.IsPay);
        if (hasDate)
            recharge = recharge.Where(o => o.CreateAt >= start && o.CreateAt < end);
        var unpaid = await recharge.CountAsync(cancellationToken);
        return new
        {
            WithdrawPending = pending,
            WithdrawWaitPay = waitPay,
            WithdrawPaying = paying,
            RechargeUnpaid = unpaid
        };
    }

    /// <summary>首页盈亏：区间内游戏日汇总的盈亏合计。</summary>
    private async Task<object?> ProfitofindexCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var rows = await LoadGamesAsync(input, cancellationToken);
        return new { Money = rows.Sum(g => g.Money), BetMoney = rows.Sum(g => g.BetMoney) };
    }

    /// <summary>还没进计划的待发返水。计划列表空、这里有笔数，就是日志还没生成计划。</summary>
    private async Task<object?> RebateactiveCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId && l.Status == RebatePayoutService.LogStatusNone && l.PlanID == 0 && l.Money > 0)
            .Select(l => new { l.Money, l.Type })
            .ToListAsync(cancellationToken);
        return new
        {
            PendingLogs = rows.Count,
            PendingMoney = rows.Sum(r => r.Money),
            ManualLogs = rows.Count(r => !string.Equals(r.Type, "V2", StringComparison.OrdinalIgnoreCase))
        };
    }

    /// <summary>充提按日。复用已有日汇总，不再回显表单。</summary>
    private async Task<object?> RechargeandwitdrawCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var (start, end) = ReadRange(input);
        var list = await ListRechargeWithdrawAsync(new SiteFundDailyListInput
        {
            StartAt = start,
            EndAt = end.AddDays(-1),
            Currency = TryReadInt(input, out var currency, "Currency") ? currency : -1
        }, cancellationToken);
        return list;
    }

    /// <summary>区间内有充值的会员人数。</summary>
    private async Task<object?> RechargeusersCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var rows = await LoadRechargeAsync(input, cancellationToken);
        return new { Count = rows.Select(r => r.UserID).Distinct().Count(), Money = rows.Sum(r => r.Money) };
    }

    /// <summary>充值会员明细，按金额从高到低，最多 200 人。</summary>
    private async Task<object?> RechargeusersdetailsCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var rows = await LoadRechargeAsync(input, cancellationToken);
        var grouped = rows
            .GroupBy(r => r.UserID)
            .Select(g => new { UserID = g.Key, Money = g.Sum(x => x.Money), Count = g.Sum(x => x.Count) })
            .OrderByDescending(x => x.Money)
            .Take(200)
            .ToList();
        var ids = grouped.Select(g => g.UserID).ToList();
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

        return grouped.Select(g => new
        {
            g.UserID,
            UserName = names.TryGetValue(g.UserID, out var n) ? n : string.Empty,
            g.Money,
            g.Count
        }).ToList();
    }

    /// <summary>首页一块：活跃、投注、待审提现、待发返水。</summary>
    private async Task<object?> SystemdatareportCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var active = await ActiveusersCoreAsync(input, cancellationToken);
        var bet = await BetamountCoreAsync(input, cancellationToken);
        var orders = await OrdercountCoreAsync(input, cancellationToken);
        var rebate = await RebateactiveCoreAsync(input, cancellationToken);
        return new { Active = active, Bet = bet, Orders = orders, Rebate = rebate };
    }

    /// <summary>按状态数提现单。带了日期才限制申请时间。</summary>
    /// <param name="siteId">站点</param>
    /// <param name="status">订单状态</param>
    /// <param name="hasDate">是否按日期</param>
    /// <param name="start">开始（含）</param>
    /// <param name="end">结束（不含）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>笔数</returns>
    private async Task<int> CountWithdrawAsync(
        int siteId,
        int status,
        bool hasDate,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var query = _db.Queryable<WithdrawOrder>()
            .Where(o => o.SiteID == siteId && o.Status == status);
        if (hasDate)
            query = query.Where(o => o.CreateAt >= start && o.CreateAt < end);
        return await query.CountAsync(cancellationToken);
    }

    /// <summary>读区间内的游戏日汇总。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    private async Task<List<UserDateGame>> LoadGamesAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var (start, end) = ReadRange(input);
        var query = _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= start && g.Date < end);
        if (TryReadInt(input, out var currency, "Currency"))
            query = query.Where(g => g.Currency == currency);
        if (TryReadInt(input, out var type, "Type") && type > 0)
            query = query.Where(g => g.Type == type);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>读区间内的充值日汇总（类型 1）。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    private async Task<List<UserDateMoney>> LoadRechargeAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var (start, end) = ReadRange(input);
        var query = _db.Queryable<UserDateMoney>()
            .Where(m => m.SiteID == siteId && m.Type == TypeRecharge && m.Date >= start && m.Date < end);
        if (TryReadInt(input, out var currency, "Currency"))
            query = query.Where(m => m.Currency == currency);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>表单里是否带了日期。待审队列默认不按日期砍。</summary>
    private static bool HasDate(Dictionary<string, object?> input) =>
        ReadDate(input, "StartAt", "EndAt", "Date", "EndDate").HasValue;


    /// <summary>按键读整数；没有键返回 0。</summary>
    private static int ReadInt(Dictionary<string, object?> input, params string[] keys)
    {
        TryReadInt(input, out var n, keys);
        return n;
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

/// <summary>站点日统计筛选。</summary>
public sealed class SiteFundDailyListInput
{
    /// <summary>开始日。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束日。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>币种；-1=全部。</summary>
    public int Currency { get; set; } = -1;
}

/// <summary>站点日统计行。</summary>
public sealed class SiteFundDailyItemDto
{
    /// <summary>业务日。</summary>
    public DateTime Date { get; set; }

    /// <summary>资金类型。</summary>
    public int Type { get; set; }

    /// <summary>类型说明。</summary>
    public string TypeName { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>金额。</summary>
    public decimal Money { get; set; }

    /// <summary>笔数。</summary>
    public int Count { get; set; }
}

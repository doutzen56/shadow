using System.Globalization;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员端资金只读。对齐旧 /request/money/getbalance|moneylog|locked|rechargeorderlist|withdraworderlist|moneytype。
/// </summary>
public sealed class MemberMoneyQueryService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly MemberAuthService _memberAuth;

    public MemberMoneyQueryService(IBizDbAccessor biz, ICurrentSiteAccessor site, MemberAuthService memberAuth)
    {
        _db = biz.Db;
        _site = site;
        _memberAuth = memberAuth;
    }

    /// <summary>多币种余额。对齐 getbalance。</summary>
    public async Task<OldApiResult> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var ctx = await TryUserAsync(cancellationToken);
        if (ctx == null)
            return OldApiResult.NeedLogin();
        var siteId = ctx.Value.SiteId;
        var userId = ctx.Value.UserId;
        var user = ctx.Value.User;

        var rows = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && m.UserID == userId)
            .OrderBy(m => m.Currency, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var deposit = (user.Lock & 2) != 0 ? "0" : "1";
        var withdraw = (user.Lock & 4) != 0 ? "0" : "1";
        var list = rows.Select(t => new
        {
            UserID = t.UserID.ToString(CultureInfo.InvariantCulture),
            TotalMoney = OldApiMoneyDisplay.FormatMoney4(t.TotalMoney),
            LockMoney = OldApiMoneyDisplay.FormatMoney4(t.LockMoney),
            Unbalanced = OldApiMoneyDisplay.FormatMoney4(t.Unbalanced),
            Money = OldApiMoneyDisplay.FormatMoneyTrim(t.TotalMoney - t.LockMoney),
            Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency)
        }).ToList();
        return OldApiResult.Ok(
            string.Empty,
            OldApiShowResult.OfPage(list, list.Count, 1, 20, new { Deposit = deposit, Withdraw = withdraw }));
    }

    /// <summary>帐变。对齐 moneylog。</summary>
    public async Task<OldApiResult> MoneyLogAsync(
        DateTime? startAt,
        DateTime? endAt,
        string? typeCsv,
        string? currencyText,
        string? sourceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var ctx = await TryUserAsync(cancellationToken);
        if (ctx == null)
            return OldApiResult.NeedLogin();
        var siteId = ctx.Value.SiteId;
        var userId = ctx.Value.UserId;
        var user = ctx.Value.User;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);
        var query = _db.Queryable<SiteMoneyLog>()
            .Where(t => t.SiteID == siteId && t.UserID == userId);
        if (startAt.HasValue)
            query = query.Where(t => t.CreateAt > startAt.Value);
        if (endAt.HasValue)
            query = query.Where(t => t.CreateAt < endAt.Value);
        if (!string.IsNullOrWhiteSpace(sourceId))
            query = query.Where(t => t.SourceID == sourceId.Trim());
        if (!string.IsNullOrWhiteSpace(typeCsv))
        {
            var types = new List<int>();
            foreach (var part in typeCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (MoneyTypeNameResolver.TryResolve(part, out var ty))
                    types.Add(ty);
                else if (int.TryParse(part, out var n))
                    types.Add(n);
            }
            if (types.Count > 0)
                query = query.Where(t => types.Contains(t.Type));
        }
        if (!string.IsNullOrWhiteSpace(currencyText))
        {
            if (int.TryParse(currencyText, out var cur))
                query = query.Where(t => t.Currency == cur);
            else
            {
                for (var i = 0; i <= 16; i++)
                {
                    if (string.Equals(OldApiMoneyDisplay.FormatCurrency(i), currencyText.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(t => t.Currency == i);
                        break;
                    }
                }
            }
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(t => t.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        var list = rows.Select(t => new
        {
            ID = t.ID.ToString(CultureInfo.InvariantCulture),
            UserID = t.UserID.ToString(CultureInfo.InvariantCulture),
            UserName = user.UserName,
            CreateAt = OldApiMoneyDisplay.FormatDateTime(t.CreateAt),
            Money = OldApiMoneyDisplay.FormatMoney4(t.Money),
            LockMoney = OldApiMoneyDisplay.FormatMoney4(t.LockMoney),
            Balance = OldApiMoneyDisplay.FormatMoney4(t.Balance),
            Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
            Type = FormatMoneyType(t.Type),
            TypeValue = OldApiMoneyDisplay.FormatMoneyTypeValue(t.Type),
            Description = (t.Description ?? string.Empty).Split('|')[0].Trim().Replace("代理转账", "", StringComparison.Ordinal),
            SourceID = t.SourceID ?? string.Empty
        });
        return OldApiResult.Ok(string.Empty, OldApiShowResult.OfPage(list, total, page, pageSize));
    }

    /// <summary>锁定列表。对齐 locked。</summary>
    public async Task<OldApiResult> LockedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var ctx = await TryUserAsync(cancellationToken);
        if (ctx == null)
            return OldApiResult.NeedLogin();
        var siteId = ctx.Value.SiteId;
        var userId = ctx.Value.UserId;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);
        var query = _db.Queryable<MoneyLock>()
            .Where(t => t.SiteID == siteId && t.UserID == userId && t.IsLock);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(t => t.CreateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        var list = rows.Select(t => new
        {
            ID = t.ID.ToString(CultureInfo.InvariantCulture),
            Type = FormatMoneyType(t.Type),
            Money = OldApiMoneyDisplay.FormatMoney4(t.Money),
            CreateAt = OldApiMoneyDisplay.FormatDateTime(t.CreateAt),
            LockDesc = t.LockDesc ?? string.Empty
        });
        return OldApiResult.Ok(string.Empty, OldApiShowResult.OfPage(list, total, page, pageSize));
    }

    /// <summary>充值订单。对齐 rechargeorderlist。</summary>
    public async Task<OldApiResult> RechargeOrderListAsync(
        DateTime? startAt,
        DateTime? endAt,
        string? orderId,
        int payId,
        bool? isPay,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var ctx = await TryUserAsync(cancellationToken);
        if (ctx == null)
            return OldApiResult.NeedLogin();
        var siteId = ctx.Value.SiteId;
        var userId = ctx.Value.UserId;
        var user = ctx.Value.User;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);
        var since = DateTime.Now.AddDays(-90);
        var dayAgo = DateTime.Now.AddDays(-1);
        var query = _db.Queryable<RechargeOrder>()
            .Where(t => t.SiteID == siteId && t.UserID == userId && t.CreateAt > since)
            .Where(t => t.IsPay || t.CreateAt > dayAgo);
        if (startAt.HasValue)
            query = query.Where(t => t.CreateAt > startAt.Value);
        if (endAt.HasValue)
            query = query.Where(t => t.CreateAt < endAt.Value);
        if (!string.IsNullOrWhiteSpace(orderId))
            query = query.Where(t => t.ID == orderId.Trim());
        if (payId > 0)
            query = query.Where(t => t.PayID == payId);
        if (isPay.HasValue)
            query = query.Where(t => t.IsPay == isPay.Value);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(t => t.CreateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        // 支付通道名（Payment）
        var payIds = rows.Select(r => r.PayID).Where(id => id > 0).Distinct().ToList();
        var payNames = new Dictionary<int, string>();
        if (payIds.Count > 0)
        {
            try
            {
                var pays = await _db.Queryable<SitePayment>()
                    .Where(p => p.SiteID == siteId && payIds.Contains(p.ID))
                    .Select(p => new { p.ID, p.Name })
                    .ToListAsync(cancellationToken);
                foreach (var p in pays)
                    payNames[p.ID] = p.Name ?? string.Empty;
            }
            catch
            {
                // 缺表时忽略 Payment 名
            }
        }

        var list = rows.Select(t => new
        {
            ID = t.ID,
            UserID = t.UserID.ToString(CultureInfo.InvariantCulture),
            UserName = user.UserName,
            Money = OldApiMoneyDisplay.FormatMoney4(t.Money),
            Fee = OldApiMoneyDisplay.FormatMoney4(t.Fee),
            Paid = OldApiMoneyDisplay.FormatMoney4(t.Paid),
            CreateAt = OldApiMoneyDisplay.FormatDateTime(t.CreateAt),
            PayAt = t.PayAt == default ? string.Empty : OldApiMoneyDisplay.FormatDateTime(t.PayAt),
            IsPay = t.IsPay ? 1 : 0,
            PayID = t.PayID.ToString(CultureInfo.InvariantCulture),
            Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
            Remark = (t.Remark ?? string.Empty).Split('|')[0].Trim(),
            FromCurrency = OldApiMoneyDisplay.FormatCurrency(t.FromCurrency),
            Payment = payNames.TryGetValue(t.PayID, out var pn) ? pn : string.Empty
        });
        return OldApiResult.Ok(string.Empty, OldApiShowResult.OfPage(list, total, page, pageSize));
    }

    /// <summary>提现订单。对齐 withdraworderlist。</summary>
    public async Task<OldApiResult> WithdrawOrderListAsync(
        DateTime? startAt,
        DateTime? endAt,
        int orderId,
        string? statusText,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var ctx = await TryUserAsync(cancellationToken);
        if (ctx == null)
            return OldApiResult.NeedLogin();
        var siteId = ctx.Value.SiteId;
        var userId = ctx.Value.UserId;
        var user = ctx.Value.User;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);
        var since = DateTime.Now.AddDays(-90);
        var query = _db.Queryable<WithdrawOrder>()
            .Where(t => t.SiteID == siteId && t.UserID == userId && t.CreateAt > since);
        if (startAt.HasValue)
            query = query.Where(t => t.CreateAt > startAt.Value);
        if (endAt.HasValue)
            query = query.Where(t => t.CreateAt < endAt.Value);
        if (orderId > 0)
            query = query.Where(t => t.ID == orderId);
        if (!string.IsNullOrWhiteSpace(statusText) && int.TryParse(statusText, out var st))
            query = query.Where(t => t.Status == st);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(t => t.CreateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        var list = rows.Select(t => new
        {
            ID = t.ID.ToString(CultureInfo.InvariantCulture),
            UserID = t.UserID.ToString(CultureInfo.InvariantCulture),
            UserName = user.UserName,
            CreateAt = OldApiMoneyDisplay.FormatDateTime(t.CreateAt),
            Money = OldApiMoneyDisplay.FormatMoney4(t.Money),
            Fee = OldApiMoneyDisplay.FormatMoney4(t.Fee),
            Bank = $"{t.AccountName} {t.AccountNumber}".Trim(),
            Paid = Math.Round(t.Paid, 2).ToString("0.##", CultureInfo.InvariantCulture),
            PayAt = t.PayAt == default ? string.Empty : OldApiMoneyDisplay.FormatDateTime(t.PayAt),
            Currency = OldApiMoneyDisplay.FormatCurrency(t.Currency),
            FromCurrency = OldApiMoneyDisplay.FormatCurrency(t.FromCurrency),
            Status = t.Status,
            Description = (t.Description ?? string.Empty).Split('|')[0].Trim()
        });
        return OldApiResult.Ok(string.Empty, OldApiShowResult.OfPage(list, total, page, pageSize));
    }

    /// <summary>资金类型树。对齐 moneytype（站点 1002 抽检全量）。</summary>
    public Task<OldApiResult> MoneyTypeAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var info = BuildMoneyTypeTree();
        return Task.FromResult(OldApiResult.Ok(string.Empty, info));
    }

    private async Task<(int SiteId, int UserId, User User)?> TryUserAsync(CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var uid = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, cancellationToken);
        if (uid == null)
            return null;

        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == uid.Value && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return null;
        return (siteId, uid.Value, user);
    }

    private static string FormatMoneyType(int type)
    {
        foreach (var kv in new Dictionary<int, string>
                 {
                     [1] = "充值",
                     [2] = "提现",
                     [3] = "充值奖励",
                     [4] = "提现手续费",
                     [5] = "游戏转出",
                     [6] = "充值手续费",
                     [8] = "转入游戏",
                     [9] = "充值扣除",
                     [10] = "提现退回",
                     [11] = "提现扣除",
                     [60] = "返水",
                     [61] = "手动返水",
                     [62] = "返水扣除",
                     [111] = "代客充值",
                     [120] = "转入",
                     [121] = "转出",
                     [130] = "代理返佣",
                     [131] = "代理返点",
                     [132] = "佣金扣除",
                     [200] = "手动活动",
                     [250] = "手工加款",
                     [251] = "手工减款"
                 })
        {
            if (kv.Key == type)
                return kv.Value;
        }

        return type.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>旧站 1002 moneytype 全量树（对打抽检）。</summary>
    private static Dictionary<string, Dictionary<string, string>> BuildMoneyTypeTree() =>
        MoneyTypeCatalog.BuildTree();
}

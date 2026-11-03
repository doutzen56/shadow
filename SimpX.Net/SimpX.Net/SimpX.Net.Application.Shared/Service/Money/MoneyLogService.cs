using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台会员帐变只读。对齐旧版 /admin/report/money/log 核心筛选；不写库。
/// </summary>
public sealed class MoneyLogService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。帐变在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public MoneyLogService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站帐变分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页与合计</returns>
    public async Task<MoneyLogAdminPageOutput> PageAsync(
        MoneyLogAdminPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        var startAt = input.StartAt ?? DateTime.Now.Date;
        var endExclusive = (input.EndAt ?? DateTime.Now).AddSeconds(1);

        var query = _db.Queryable<SiteMoneyLog>()
            .Where(t => t.SiteID == siteId && t.CreateAt >= startAt && t.CreateAt < endExclusive);

        if (input.UserID > 0)
            query = query.Where(t => t.UserID == input.UserID);
        else if (!string.IsNullOrWhiteSpace(input.User))
        {
            var userKey = input.User.Trim();
            var belong = input.BelongUser is "1" or "true" or "True";
            if (int.TryParse(userKey, out var uid))
            {
                if (belong)
                {
                    var childIds = await _db.Queryable<User>()
                        .Where(u => u.SiteID == siteId && u.AgentID == uid)
                        .Select(u => u.ID)
                        .ToListAsync(cancellationToken);
                    childIds.Add(uid);
                    query = query.Where(t => childIds.Contains(t.UserID));
                }
                else
                    query = query.Where(t => t.UserID == uid);
            }
            else
            {
                var roots = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(userKey))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
                if (roots.Count == 0)
                    query = query.Where(t => false);
                else if (belong)
                {
                    var rootId = roots[0];
                    var childIds = await _db.Queryable<User>()
                        .Where(u => u.SiteID == siteId && u.AgentID == rootId)
                        .Select(u => u.ID)
                        .ToListAsync(cancellationToken);
                    childIds.Add(rootId);
                    query = query.Where(t => childIds.Contains(t.UserID));
                }
                else
                    query = query.Where(t => roots.Contains(t.UserID));
            }
        }

        if (!string.IsNullOrWhiteSpace(input.Content))
            query = query.Where(t => t.Description != null && t.Description.Contains(input.Content.Trim()));

        if (!string.IsNullOrWhiteSpace(input.SourceID))
            query = query.Where(t => t.SourceID != null && t.SourceID.Contains(input.SourceID.Trim()));

        if (!string.IsNullOrWhiteSpace(input.Type))
        {
            var types = input.Type.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var v) ? v : (int?)null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();
            if (types.Count > 0)
                query = query.Where(t => types.Contains(t.Type));
        }

        int? currencyFilter = null;
        if (!string.IsNullOrWhiteSpace(input.Currency))
        {
            var curRaw = input.Currency.Trim();
            if (int.TryParse(curRaw, out var cur) && cur > 0)
                currencyFilter = cur;
            else if (Enum.TryParse<Currency>(curRaw, true, out var curEnum) && (int)curEnum > 0)
                currencyFilter = (int)curEnum;
            if (currencyFilter.HasValue)
                query = query.Where(t => t.Currency == currencyFilter.Value);
        }

        // 每次独立 Clone，避免 Distinct/聚合污染后续分页查询的 ORDER BY
        var member = await query.Clone().Select(t => t.UserID).Distinct().CountAsync(cancellationToken);
        var total = await query.Clone().CountAsync(cancellationToken);

        var summary = await BuildSummaryAsync(query.Clone(), siteId, currencyFilter.HasValue, cancellationToken);
        summary.Member = member;

        var rows = await query.Clone()
            .OrderBy(t => t.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.UserID).Distinct().ToList();
        var users = userIds.Count == 0
            ? new List<User>()
            : await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
                .ToListAsync(cancellationToken);
        var userMap = users.ToDictionary(u => u.ID, u => u);
        var parentIds = users.Select(u => u.AgentID).Where(id => id > 0).Distinct().ToList();
        var parents = parentIds.Count == 0
            ? new List<User>()
            : await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && parentIds.Contains(u.ID))
                .ToListAsync(cancellationToken);
        var parentMap = parents.ToDictionary(u => u.ID, u => u.UserName ?? string.Empty);

        var data = rows.Select(t =>
        {
            userMap.TryGetValue(t.UserID, out var user);
            var up = string.Empty;
            if (user != null && user.AgentID > 0 && parentMap.TryGetValue(user.AgentID, out var pn))
                up = pn;
            return new MoneyLogAdminListItemDto
            {
                ID = t.ID,
                UserID = t.UserID,
                UserName = user?.UserName ?? string.Empty,
                UpUserName = up,
                Money = Math.Round(t.Money, 2),
                LockMoney = t.LockMoney,
                Balance = t.Balance,
                CreateAt = t.CreateAt,
                IP = t.IP ?? string.Empty,
                Type = t.Type,
                TypeValue = OldApiMoneyDisplay.FormatMoneyTypeValue(t.Type),
                SourceID = t.SourceID ?? string.Empty,
                Description = t.Description ?? string.Empty,
                Currency = t.Currency,
                CurrencyCode = OldApiMoneyDisplay.FormatCurrency(t.Currency)
            };
        }).ToList();

        return new MoneyLogAdminPageOutput
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Data = data,
            Summary = summary
        };
    }

    /// <summary>
    /// 合计金额/余额；多币种时按基准汇率折算。
    /// </summary>
    private async Task<MoneyLogAdminSummaryDto> BuildSummaryAsync(
        ISugarQueryable<SiteMoneyLog> query,
        int siteId,
        bool singleCurrency,
        CancellationToken cancellationToken)
    {
        if (singleCurrency)
        {
            var money = await query.Clone().SumAsync(t => (decimal?)t.Money) ?? 0m;
            var balance = await query.Clone().SumAsync(t => (decimal?)t.Balance) ?? 0m;
            return new MoneyLogAdminSummaryDto { Money = money, Balance = balance };
        }

        var groups = await query.Clone()
            .GroupBy(t => t.Currency)
            .Select(t => new
            {
                Currency = t.Currency,
                Money = SqlFunc.AggregateSum(t.Money),
                Balance = SqlFunc.AggregateSum(t.Balance)
            })
            .ToListAsync(cancellationToken);

        var baseCurrency = await ResolveBaseCurrencyAsync(siteId, cancellationToken);
        var rates = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.SiteID == siteId && r.BaseCurrency == baseCurrency)
            .ToListAsync(cancellationToken);

        decimal moneySum = 0m, balanceSum = 0m;
        foreach (var g in groups)
        {
            var rate = rates.FirstOrDefault(r => r.ExchangeCurrency == g.Currency)?.Point ?? 1m;
            if (rate == 0m) rate = 1m;
            moneySum += g.Money / rate;
            balanceSum += g.Balance / rate;
        }

        return new MoneyLogAdminSummaryDto { Money = moneySum, Balance = balanceSum };
    }

    /// <summary>
    /// 取站点基准币：优先 IsBase 行的 BaseCurrency。
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
}

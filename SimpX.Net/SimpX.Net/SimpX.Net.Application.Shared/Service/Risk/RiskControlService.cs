using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Risk;
using SimpX.Net.Data.Entity.Systems;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 风控列表：会员盈利 / 游戏盈利 / 风险会员。对齐旧 /admin/risk/control/profit|game、/admin/risk/user/list。
/// </summary>
public sealed partial class RiskControlService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。风控汇总在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public RiskControlService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 会员盈利（按资金分类汇总）。对齐旧 profit。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行+合计</returns>
    public async Task<RiskProfitOutput> ProfitAsync(
        RiskControlQueryInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var startAt = (input.StartAt ?? DateTime.Now.Date).Date;
        var endAt = (input.EndAt ?? DateTime.Now.Date).Date.AddDays(1);
        if (startAt > endAt)
            throw Oops.Oh("#日期错误#");

        var profitTypes = await _db.Queryable<MoneyTypeIsProfit>()
            .Where(t => t.IsProfit)
            .Select(t => t.Type)
            .ToListAsync(cancellationToken);
        if (profitTypes.Count == 0)
            return new RiskProfitOutput();

        var typeCats = await _db.Queryable<MoneyTypeCategory>()
            .Where(t => t.Index == 0)
            .ToListAsync(cancellationToken);
        var typeToCat = typeCats
            .GroupBy(t => t.Type)
            .ToDictionary(g => g.Key, g => g.First().Category);

        var moneyQ = _db.Queryable<UserDateMoney>()
            .Where(t => t.SiteID == siteId && t.Date >= startAt && t.Date < endAt && profitTypes.Contains(t.Type));

        var userName = (input.User ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(userName))
        {
            var userIdsFilter = await ResolveUserIdsAsync(siteId, userName, cancellationToken);
            if (userIdsFilter.Count == 0)
                moneyQ = moneyQ.Where(t => false);
            else
                moneyQ = moneyQ.Where(t => userIdsFilter.Contains(t.UserID));
        }

        var moneyRows = await moneyQ
            .Select(t => new { t.UserID, t.Type, t.Money })
            .ToListAsync(cancellationToken);

        var byUserType = moneyRows
            .GroupBy(t => new { t.UserID, t.Type })
            .Select(g => new { g.Key.UserID, g.Key.Type, Money = g.Sum(x => x.Money) })
            .ToList();

        var cateRows = byUserType
            .Where(t => typeToCat.ContainsKey(t.Type))
            .Select(t => new { t.UserID, Category = typeToCat[t.Type], t.Money })
            .GroupBy(t => new { t.UserID, t.Category })
            .Select(g => new { g.Key.UserID, g.Key.Category, Money = g.Sum(x => x.Money) })
            .ToList();

        var userIds = cateRows.Select(t => t.UserID).Distinct().ToList();
        var nameMap = await LoadUserNamesAsync(siteId, userIds, cancellationToken);

        var rows = new List<RiskProfitRowDto>();
        foreach (var uid in userIds.OrderBy(t => t))
        {
            var cats = cateRows.Where(t => t.UserID == uid).ToList();
            var dict = cats.ToDictionary(t => CategoryName(t.Category), t => t.Money);
            rows.Add(new RiskProfitRowDto
            {
                UserID = uid,
                UserName = nameMap.GetValueOrDefault(uid, string.Empty),
                Categories = dict,
                Total = cats.Sum(t => t.Money)
            });
        }

        var footerCats = cateRows
            .GroupBy(t => t.Category)
            .ToDictionary(g => CategoryName(g.Key), g => g.Sum(x => x.Money));
        rows.Add(new RiskProfitRowDto
        {
            UserID = 0,
            UserName = "统计",
            Categories = footerCats,
            Total = footerCats.Values.Sum(),
            IsFooter = true
        });

        return new RiskProfitOutput { Rows = rows };
    }

    /// <summary>
    /// 游戏盈利。对齐旧 control/game。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行+合计</returns>
    public async Task<RiskGameProfitOutput> GameProfitAsync(
        RiskControlQueryInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var q = _db.Queryable<UserDateGame>().Where(t => t.SiteID == siteId);

        if (input.StartAt.HasValue)
            q = q.Where(t => t.Date >= input.StartAt.Value.Date);
        if (input.EndAt.HasValue)
            q = q.Where(t => t.Date < input.EndAt.Value.Date.AddDays(1));
        if (input.Type > 0)
            q = q.Where(t => t.Type == input.Type);

        var userName = (input.User ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(userName))
        {
            var userIdsFilter = await ResolveUserIdsAsync(siteId, userName, cancellationToken);
            if (userIdsFilter.Count == 0)
                q = q.Where(t => false);
            else
                q = q.Where(t => userIdsFilter.Contains(t.UserID));
        }

        var raw = await q.Select(t => new { t.Type, t.UserID, t.Money }).ToListAsync(cancellationToken);
        if (raw.Count == 0)
            return new RiskGameProfitOutput();

        var grouped = raw
            .GroupBy(t => new { t.Type, t.UserID })
            .Select(g => new { g.Key.Type, g.Key.UserID, Money = g.Sum(x => x.Money) })
            .OrderByDescending(t => t.Money)
            .ToList();

        var nameMap = await LoadUserNamesAsync(siteId, grouped.Select(t => t.UserID).Distinct().ToList(), cancellationToken);
        var rows = grouped.Select(t => new RiskGameProfitRowDto
        {
            UserID = t.UserID,
            UserName = nameMap.GetValueOrDefault(t.UserID, string.Empty),
            Type = t.Type,
            Money = t.Money
        }).ToList();

        return new RiskGameProfitOutput
        {
            Rows = rows,
            TotalMoney = rows.Sum(t => t.Money)
        };
    }

    /// <summary>
    /// 风险会员列表。对齐旧 /admin/risk/user/list。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<RiskDangerUserRowDto>> DangerUsersAsync(
        RiskControlQueryInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var startAt = (input.StartAt ?? DateTime.Now.Date).Date;
        var endAt = (input.EndAt ?? DateTime.Now.Date).Date.AddDays(1);
        if (startAt > endAt)
            throw Oops.Oh("#起始日期不能大于或等于截至日期#");

        var q = _db.Queryable<RiskUserDanger>()
            .Where(t => t.SiteID == siteId && t.Date >= startAt && t.Date < endAt);

        var userName = (input.User ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(userName))
        {
            var userIdsFilter = await ResolveUserIdsAsync(siteId, userName, cancellationToken);
            if (userIdsFilter.Count == 0)
                q = q.Where(t => false);
            else
                q = q.Where(t => userIdsFilter.Contains(t.UserID));
        }

        var status = (input.Status ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(status))
            q = q.Where(t => t.Type.Contains(status));

        var risks = await q.OrderBy(t => t.CreateAt, OrderByType.Desc).ToListAsync(cancellationToken);
        if (risks.Count == 0)
            return [];

        var userIds = risks.Select(t => t.UserID).Distinct().ToList();
        var users = await _db.Queryable<User>()
            .Where(t => t.SiteID == siteId && userIds.Contains(t.ID))
            .Select(t => new { t.ID, t.UserName, t.AgentID, t.Lock, t.CreateAt, t.Level })
            .ToListAsync(cancellationToken);
        var userMap = users.ToDictionary(t => t.ID);
        var agentIds = users.Select(t => t.AgentID).Where(t => t > 0).Distinct().ToList();
        var agentNames = await LoadUserNamesAsync(siteId, agentIds, cancellationToken);

        return risks.Select(r =>
        {
            userMap.TryGetValue(r.UserID, out var u);
            return new RiskDangerUserRowDto
            {
                RiskID = r.RiskID,
                UserID = r.UserID,
                UserName = u?.UserName ?? string.Empty,
                AgentName = u != null && u.AgentID > 0
                    ? agentNames.GetValueOrDefault(u.AgentID, string.Empty)
                    : string.Empty,
                Context = r.Context ?? string.Empty,
                CreateAt = r.CreateAt,
                Date = r.Date,
                Lock = u?.Lock ?? 0,
                RegCreateAt = u?.CreateAt ?? default,
                Level = u?.Level ?? (byte)0,
                Type = r.Type ?? string.Empty
            };
        }).ToList();
    }

    /// <summary>
    /// 用户名 → 编号（精确；兼容旧单用户解析）。
    /// </summary>
    private async Task<int> ResolveUserIdAsync(int siteId, string userName, CancellationToken cancellationToken)
    {
        var ids = await ResolveUserIdsAsync(siteId, userName, cancellationToken);
        return ids.FirstOrDefault();
    }

    /// <summary>
    /// 用户名模糊 → 编号列表；纯数字按 UserID。
    /// </summary>
    private async Task<List<int>> ResolveUserIdsAsync(int siteId, string userName, CancellationToken cancellationToken)
    {
        var key = (userName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
            return [];
        if (int.TryParse(key, out var uidExact))
            return [uidExact];
        return await _db.Queryable<User>()
            .Where(t => t.SiteID == siteId && t.UserName.Contains(key))
            .Select(t => t.ID)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量取用户名。
    /// </summary>
    private async Task<Dictionary<int, string>> LoadUserNamesAsync(
        int siteId,
        List<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return new Dictionary<int, string>();
        var rows = await _db.Queryable<User>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .Select(t => new { t.ID, t.UserName })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(t => t.ID, t => t.UserName ?? string.Empty);
    }

    /// <summary>
    /// 资金分类显示名（对齐旧 CategoryType.GetDescription 常用项）。
    /// </summary>
    private static string CategoryName(int category) => category switch
    {
        0 => "手动操作",
        1 => "充值",
        2 => "提现",
        3 => "手续费",
        4 => "奖励",
        5 => "游戏",
        6 => "投注",
        7 => "奖金",
        8 => "佣金",
        9 => "返水",
        10 => "活动",
        11 => "转账",
        12 => "返点",
        _ => $"分类{category}"
    };
}

using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 代理日统计只读。对齐旧 /report/agent/statistics 简化列表。
/// </summary>
public sealed class AgentStatReportService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public AgentStatReportService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 列表（最多 500）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<AgentStatReportItemDto>> ListAsync(
        AgentStatReportListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new AgentStatReportListInput();
        var siteId = _site.RequireSiteId();
        var start = input.StartAt?.Date ?? DateTime.Today.AddDays(-7);
        var end = (input.EndAt?.Date ?? DateTime.Today).AddDays(1);

        var query = _db.Queryable<ReportAgentStatis>()
            .Where(a => a.SiteID == siteId && a.Date >= start && a.Date < end);
        if (input.Currency >= 0)
            query = query.Where(a => a.Currency == input.Currency);
        if (input.UserID > 0)
            query = query.Where(a => a.UserID == input.UserID);
        else if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName.Contains(input.UserName.Trim()))
                .Select(u => u.ID).Take(50).ToListAsync(cancellationToken);
            if (users.Count == 0)
                return [];
            query = query.Where(a => users.Contains(a.UserID));
        }

        var rows = await query.OrderBy(a => a.Date, OrderByType.Desc).Take(500)
            .ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var names = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName }).ToListAsync(cancellationToken);
            foreach (var n in names)
                nameMap[n.ID] = n.UserName;
        }

        return rows.Select(r => new AgentStatReportItemDto
        {
            Date = r.Date,
            UserID = r.UserID,
            UserName = nameMap.TryGetValue(r.UserID, out var un) ? un : string.Empty,
            Currency = r.Currency,
            GameMoney = r.GameMoney,
            MoneyEvent = r.MoneyEvent,
            MoneyRebate = r.MoneyRebate,
            RechargeFee = r.RechargeFee,
            WithdrawFee = r.WithdrawFee
        }).ToList();
    }
}

/// <summary>代理统计筛选。</summary>
public sealed class AgentStatReportListInput
{
    /// <summary>开始日。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束日。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>代理编号；0=不限。</summary>
    public int UserID { get; set; }

    /// <summary>用户名模糊。</summary>
    public string UserName { get; set; }

    /// <summary>币种；-1=全部。</summary>
    public int Currency { get; set; } = -1;
}

/// <summary>代理统计行。</summary>
public sealed class AgentStatReportItemDto
{
    /// <summary>业务日。</summary>
    public DateTime Date { get; set; }

    /// <summary>代理编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>游戏盈亏。</summary>
    public decimal GameMoney { get; set; }

    /// <summary>活动金额。</summary>
    public decimal MoneyEvent { get; set; }

    /// <summary>返水金额。</summary>
    public decimal MoneyRebate { get; set; }

    /// <summary>充值手续费。</summary>
    public decimal RechargeFee { get; set; }

    /// <summary>提现手续费。</summary>
    public decimal WithdrawFee { get; set; }
}

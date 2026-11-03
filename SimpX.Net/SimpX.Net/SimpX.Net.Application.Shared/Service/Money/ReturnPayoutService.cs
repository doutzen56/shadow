using System.Text.Json;
using SimpX.Net.Data.Entity.Agents;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 代理返点发放。复用 usr_RebateLog，Type=AgentReturn；账变 AgentReturn=121。
/// </summary>
public sealed class ReturnPayoutService : ITransient
{
    /// <summary>返点日志类型标记。</summary>
    public const string LogTypeAgentReturn = "AgentReturn";

    /// <summary>待发放。</summary>
    public const int StatusNone = 0;

    /// <summary>失败/拒绝（旧 CheckStatus.Faild）。重叠判断时忽略这类记录。</summary>
    public const int StatusFailed = 1;

    /// <summary>已发放。</summary>
    public const int StatusPaid = 2;

    /// <summary>代理返点账变类型。</summary>
    public const int MoneyTypeAgentReturn = 121;

    /// <summary>
    /// 简算默认比例：下级投注合计 × 本比例。
    /// 无独立返点方案表时先用固定值；Detail 里可带 Rate 覆盖。
    /// </summary>
    public const decimal DefaultReturnRate = 0.01m;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;

    /// <summary>注入。</summary>
    public ReturnPayoutService(IBizDbAccessor biz, ICurrentSiteAccessor site, IMoneyLedger money)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
    }

    /// <summary>返点日志列表（最多 200）。</summary>
    public async Task<List<ReturnLogListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId && l.Type == LogTypeAgentReturn)
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var names = await LoadNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);
        return rows.Select(l => new ReturnLogListItemDto
        {
            ID = l.ID,
            UserID = l.UserID,
            UserName = names.GetValueOrDefault(l.UserID, string.Empty),
            Money = l.Money,
            Status = l.Status,
            Currency = l.Currency,
            StartAt = l.StartAt,
            EndAt = l.EndAt,
            CreateAt = l.CreateAt,
            PlanID = l.PlanID
        }).ToList();
    }

    /// <summary>
    /// 按区间生成待发返点日志（写 usr_RebateLog Type=AgentReturn，不写账变）。
    /// 日期校验与佣金生成对齐：截止日须早于今天；可注入 now 便于单测。
    /// </summary>
    /// <param name="start">统计起（含）。</param>
    /// <param name="end">统计止（含）；必须早于今天。</param>
    /// <param name="now">当前时间；单测可注入，默认 DateTime.Now。</param>
    /// <param name="cancellationToken">取消。</param>
    /// <returns>成功与失败条数。</returns>
    public async Task<ReturnCreateResultDto> CreateAsync(
        DateTime start,
        DateTime end,
        DateTime? now = null,
        CancellationToken cancellationToken = default)
    {
        var clock = now ?? DateTime.Now;
        var siteId = _site.RequireSiteId();
        start = start.Date;
        end = end.Date;

        if (start > end)
            throw Oops.Oh("#起始日期不能大于或等于截至日期#");
        if (end >= clock.Date)
            throw Oops.Oh("#不能生成大于或等于当天的返点#");
        if (clock < clock.Date.AddHours(1))
            throw Oops.Oh("#当天凌晨1点后才能生成代理返点#");

        // 同站 Type=AgentReturn 且 Status≠Faild 的区间重叠则拒发（对齐旧幂等）。
        var overlap = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId
                        && l.Type == LogTypeAgentReturn
                        && l.Status != StatusFailed
                        && l.StartAt <= end
                        && start <= l.EndAt)
            .AnyAsync(cancellationToken);
        if (overlap)
            throw Oops.Oh("#该时间段已生成返佣记录,不可重复发放#");

        var agents = await _db.Queryable<AgentUser>()
            .Where(a => a.SiteID == siteId
                        && a.ReturnID > 0
                        && a.Status == AgentUser.AgentStatus.Normal)
            .ToListAsync(cancellationToken);
        if (agents.Count == 0)
            return new ReturnCreateResultDto { Success = 0, Failed = 0 };

        var success = 0;
        var failed = 0;
        foreach (var agent in agents)
        {
            try
            {
                var childIds = await _db.Queryable<UserDepth>()
                    .Where(d => d.SiteID == siteId && d.UserID == agent.ID && d.ChildID != agent.ID)
                    .Select(d => d.ChildID)
                    .ToListAsync(cancellationToken);

                decimal sumBet = 0;
                var currency = 0;
                if (childIds.Count > 0)
                {
                    var bets = await _db.Queryable<UserDateGame>()
                        .Where(g => g.SiteID == siteId
                                    && childIds.Contains(g.UserID)
                                    && g.Date >= start
                                    && g.Date <= end)
                        .ToListAsync(cancellationToken);
                    if (bets.Count > 0)
                    {
                        currency = bets
                            .GroupBy(g => g.Currency)
                            .OrderByDescending(g => g.Count())
                            .Select(g => g.Key)
                            .First();
                        sumBet = bets.Where(g => g.Currency == currency).Sum(g => g.BetMoney);
                    }
                }

                // 简算：下级投注 × 固定 Rate；Detail 里写清 Rate/BetMoney 便于核对。
                var rate = DefaultReturnRate;
                var money = sumBet * rate;
                if (money <= 0)
                    continue;

                var detailJson = JsonSerializer.Serialize(new
                {
                    BetMoney = sumBet,
                    Rate = rate,
                    Money = money,
                    Currency = currency,
                    ReturnID = agent.ReturnID
                });

                var log = new UserRebateLog
                {
                    SiteID = siteId,
                    PlanID = 0,
                    UserID = agent.ID,
                    StartAt = start,
                    EndAt = end,
                    Money = money,
                    CreateAt = clock,
                    CheckAt = DateTime.MinValue,
                    Detail = detailJson,
                    Type = LogTypeAgentReturn,
                    Status = StatusNone,
                    FaildMsg = string.Empty,
                    RebateID = agent.ReturnID,
                    Currency = currency,
                    SortID = 0
                };
                await _db.Insertable(log).ExecuteCommandAsync(cancellationToken);
                success++;
            }
            catch
            {
                failed++;
            }
        }

        return new ReturnCreateResultDto { Success = success, Failed = failed };
    }

    /// <summary>发放一笔待审返点。</summary>
    public async Task<bool> PayAsync(int logId, CancellationToken cancellationToken = default)
    {
        if (logId <= 0)
            throw Oops.Oh("#返点记录错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId && l.ID == logId && l.Type == LogTypeAgentReturn)
            .Take(1)
            .ToListAsync(cancellationToken);
        var log = rows.FirstOrDefault() ?? throw Oops.Oh("#返点记录不存在#");
        if (log.Status != StatusNone)
            throw Oops.Oh("#已处理#");
        if (log.Money <= 0)
            throw Oops.Oh("#金额错误#");

        await _money.AddMoneyLogAsync(
            siteId,
            log.UserID,
            log.Money,
            MoneyTypeAgentReturn,
            $"AR{log.ID}",
            "代理返点发放",
            log.Currency,
            cancellationToken);
        log.Status = StatusPaid;
        log.CheckAt = DateTime.Now;
        await _db.Updateable(log)
            .UpdateColumns(l => new { l.Status, l.CheckAt })
            .ExecuteCommandAsync(cancellationToken);
        return true;
    }

    /// <summary>拒绝一笔待审返点。对应旧 logreject。</summary>
    public async Task RejectAsync(int logId, CancellationToken cancellationToken = default)
    {
        if (logId <= 0)
            throw Oops.Oh("#返点记录错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId && l.ID == logId && l.Type == LogTypeAgentReturn)
            .Take(1)
            .ToListAsync(cancellationToken);
        var log = rows.FirstOrDefault() ?? throw Oops.Oh("#返点记录不存在#");
        if (log.Status != StatusNone)
            throw Oops.Oh("#已处理#");
        log.Status = StatusFailed;
        log.CheckAt = DateTime.Now;
        await _db.Updateable(log)
            .UpdateColumns(l => new { l.Status, l.CheckAt })
            .ExecuteCommandAsync(cancellationToken);
    }

    private async Task<Dictionary<int, string>> LoadNamesAsync(int siteId, List<int> ids, CancellationToken ct)
    {
        var map = new Dictionary<int, string>();
        if (ids.Count == 0) return map;
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName })
            .ToListAsync(ct);
        foreach (var u in users)
            map[u.ID] = u.UserName ?? string.Empty;
        return map;
    }
}

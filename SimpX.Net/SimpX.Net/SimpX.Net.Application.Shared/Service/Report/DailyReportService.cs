using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 日报定时任务。对齐旧 DailyReportJob：
/// 00:05 生成昨日代理报表；02:05 把站点 1012 沉寂会员改到分组 562。
/// </summary>
public interface IDailyReportService
{
    /// <summary>
    /// 按当前本地时刻决定是否跑 00:05 / 02:05 两段；其它分钟空跑。
    /// </summary>
    Task<DailyReportRunResult> RunTickAsync(DateTime? nowLocal = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按日从 MoneyLog 扫用户+币种，对本人及上级链调 rpt_AgentStatistics_Update。
    /// </summary>
    Task<int> AgentStatisticsUpdateAsync(DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// 站点沉寂会员改组：CreateAt/UpdateAt 均超 inactiveDays、无近 inactiveDays 登录、非 excludeGroup → targetGroup。
    /// </summary>
    Task<int> MoveDormantUsersAsync(
        int siteId,
        int excludeGroupId,
        int targetGroupId,
        int inactiveDays = 90,
        CancellationToken cancellationToken = default);
}

/// <summary>一轮 tick 结果。</summary>
public sealed class DailyReportRunResult
{
    /// <summary>代理报表更新次数（SP 调用成功计次）。</summary>
    public int AgentUpdates { get; set; }

    /// <summary>沉寂改组人数。</summary>
    public int DormantMoved { get; set; }

    /// <summary>说明。</summary>
    public string Message { get; set; }
}

/// <summary>
/// 默认实现。不挂 ITransient：只在 Schedule 显式注册。
/// </summary>
public sealed class DailyReportService : IDailyReportService
{
    /// <summary>旧硬编码：自动改组站点。</summary>
    public const int DormantSiteId = 1012;

    /// <summary>旧硬编码：排除分组（不改）。</summary>
    public const int DormantExcludeGroupId = 554;

    /// <summary>旧硬编码：目标分组。</summary>
    public const int DormantTargetGroupId = 562;

    /// <summary>沉寂天数。</summary>
    public const int DormantInactiveDays = 90;

    private readonly ISqlSugarClient _db;
    private readonly ILogger<DailyReportService> _logger;

    public DailyReportService(IBizDbAccessor biz, ILogger<DailyReportService> logger)
    {
        _db = biz.Db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DailyReportRunResult> RunTickAsync(
        DateTime? nowLocal = null,
        CancellationToken cancellationToken = default)
    {
        var now = nowLocal ?? DateTime.Now;
        var result = new DailyReportRunResult();
        var parts = new List<string>();

        if (now.Hour == 0 && now.Minute == 5)
        {
            var yesterday = now.Date.AddDays(-1);
            _logger.LogInformation("开始生成昨日代理报表 Date={Date}", yesterday);
            result.AgentUpdates = await AgentStatisticsUpdateAsync(yesterday, cancellationToken);
            parts.Add($"代理报表更新 {result.AgentUpdates}");
            _logger.LogInformation("生成昨日代理报表完成 updates={N}", result.AgentUpdates);
        }

        if (now.Hour == 2 && now.Minute == 5)
        {
            _logger.LogInformation("沉寂会员改组 SiteID={Site}", DormantSiteId);
            result.DormantMoved = await MoveDormantUsersAsync(
                DormantSiteId,
                DormantExcludeGroupId,
                DormantTargetGroupId,
                DormantInactiveDays,
                cancellationToken);
            parts.Add($"沉寂改组 {result.DormantMoved}");
        }

        result.Message = parts.Count == 0
            ? $"空跑 {now:HH:mm}"
            : string.Join("；", parts);
        return result;
    }

    /// <inheritdoc />
    public async Task<int> AgentStatisticsUpdateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);
        List<(int SiteID, int UserID, int Currency)> pairs;
        try
        {
            var rows = await _db.Queryable<MoneyLog>()
                .Where(t => t.CreateAt >= dayStart && t.CreateAt < dayEnd)
                .GroupBy(t => new { t.SiteID, t.UserID, t.Currency })
                .Select(t => new { t.SiteID, t.UserID, t.Currency })
                .ToListAsync(cancellationToken);
            pairs = rows.Select(r => (r.SiteID, r.UserID, r.Currency)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "扫 MoneyLog 失败 Date={Date}", dayStart);
            return 0;
        }

        var hasSend = new HashSet<string>(StringComparer.Ordinal);
        var updates = 0;
        foreach (var site in pairs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var selfKey = site.Currency.ToString() + site.UserID;
                if (hasSend.Add(selfKey))
                {
                    if (await CallAgentStatisticsUpdateAsync(dayStart, site.Currency, site.UserID, site.SiteID, cancellationToken))
                        updates++;
                }

                var parents = await LoadParentIdsAsync(site.SiteID, site.UserID, cancellationToken);
                foreach (var agentId in parents)
                {
                    var key = site.Currency.ToString() + agentId;
                    if (!hasSend.Add(key))
                        continue;
                    if (await CallAgentStatisticsUpdateAsync(dayStart, site.Currency, agentId, site.SiteID, cancellationToken))
                        updates++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "代理报表单用户失败 Currency={Currency} SiteID={Site} UserID={User}",
                    site.Currency,
                    site.SiteID,
                    site.UserID);
            }
        }

        return updates;
    }

    /// <inheritdoc />
    public async Task<int> MoveDormantUsersAsync(
        int siteId,
        int excludeGroupId,
        int targetGroupId,
        int inactiveDays = 90,
        CancellationToken cancellationToken = default)
    {
        if (siteId <= 0 || targetGroupId <= 0 || inactiveDays <= 0)
            return 0;

        var cutoff = DateTime.Now.AddDays(-inactiveDays);
        List<int> recentLoginUserIds;
        try
        {
            recentLoginUserIds = await _db.Queryable<LoginLog>()
                .Where(l => l.SiteID == siteId && l.LoginAt > cutoff)
                .GroupBy(l => l.UserID)
                .Select(l => l.UserID)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "扫 LoginLog 失败 SiteID={Site}", siteId);
            return 0;
        }

        var recentSet = recentLoginUserIds.ToHashSet();
        var candidates = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId
                && u.CreateAt < cutoff
                && u.UpdateAt < cutoff
                && u.GroupID != excludeGroupId
                && u.GroupID != targetGroupId)
            .Select(u => new { u.ID, u.GroupID })
            .ToListAsync(cancellationToken);

        var moved = 0;
        foreach (var u in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (recentSet.Contains(u.ID))
                continue;
            try
            {
                var oldGroupId = u.GroupID;
                var ok = await _db.Updateable<User>()
                    .SetColumns(x => new User
                    {
                        GroupID = targetGroupId,
                        OldGroupID = oldGroupId
                    })
                    .Where(x => x.ID == u.ID && x.SiteID == siteId)
                    .ExecuteCommandAsync(cancellationToken);
                if (ok <= 0)
                    continue;
                moved++;
                await AdjustGroupMemberAsync(siteId, oldGroupId, -1, cancellationToken);
                await AdjustGroupMemberAsync(siteId, targetGroupId, +1, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "沉寂改组失败 UserID={User}", u.ID);
            }
        }

        return moved;
    }

    private async Task<List<int>> LoadParentIdsAsync(int siteId, int childId, CancellationToken cancellationToken)
    {
        return await _db.Queryable<UserDepth>()
            .Where(t => t.SiteID == siteId && t.ChildID == childId)
            .OrderBy(t => t.Depth)
            .Select(t => t.UserID)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> CallAgentStatisticsUpdateAsync(
        DateTime date,
        int currency,
        int userId,
        int siteId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _db.Ado.UseStoredProcedure().ExecuteCommandAsync(
                "rpt_AgentStatistics_Update",
                new SugarParameter("@Date", date.Date),
                new SugarParameter("@Currency", currency),
                new SugarParameter("@UserID", userId),
                new SugarParameter("@SiteID", siteId));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "rpt_AgentStatistics_Update 失败 Date={Date} Currency={Currency} UserID={User} SiteID={Site}: {Msg}",
                date.Date,
                currency,
                userId,
                siteId,
                ex.Message);
            return false;
        }
    }

    private async Task AdjustGroupMemberAsync(int siteId, int groupId, int delta, CancellationToken cancellationToken)
    {
        if (groupId <= 0 || delta == 0)
            return;
        try
        {
            await _db.Updateable<UserGroup>()
                .SetColumns(g => g.Member == g.Member + delta)
                .Where(g => g.ID == groupId && g.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "调整分组人数失败 GroupID={Group} delta={Delta}", groupId, delta);
        }
    }
}

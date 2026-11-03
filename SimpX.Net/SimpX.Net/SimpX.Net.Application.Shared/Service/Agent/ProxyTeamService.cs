using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 代理网团队：下级列表（UserDepth）。
/// </summary>
public sealed class ProxyTeamService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public ProxyTeamService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 某代理下的直属/深度下级（最多 500）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<ProxyTeamItemDto>> ListAsync(
        ProxyTeamListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new ProxyTeamListInput();
        var siteId = _site.RequireSiteId();
        if (input.UserID <= 0)
            return [];

        var depthQuery = _db.Queryable<UserDepth>()
            .Where(d => d.SiteID == siteId && d.UserID == input.UserID);
        if (input.MaxDepth > 0)
            depthQuery = depthQuery.Where(d => d.Depth <= input.MaxDepth);

        var childIds = await depthQuery.Select(d => d.ChildID).Take(500).ToListAsync(cancellationToken);
        if (childIds.Count == 0)
            return [];

        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && childIds.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName, u.CreateAt, u.Type })
            .ToListAsync(cancellationToken);
        var depths = await _db.Queryable<UserDepth>()
            .Where(d => d.SiteID == siteId && d.UserID == input.UserID && childIds.Contains(d.ChildID))
            .ToListAsync(cancellationToken);
        var depthMap = depths.ToDictionary(d => d.ChildID, d => d.Depth);

        return users.Select(u => new ProxyTeamItemDto
        {
            UserID = u.ID,
            UserName = u.UserName,
            Type = u.Type,
            Depth = depthMap.TryGetValue(u.ID, out var dep) ? dep : 0,
            CreateAt = u.CreateAt
        }).OrderBy(x => x.Depth).ThenBy(x => x.UserID).ToList();
    }

    /// <summary>团队人数（对齐 /request/agent/team/membercount）。</summary>
    /// <param name="userId">代理</param>
    /// <param name="directlyOnly">仅直属 Depth=1</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>{ Count }</returns>
    public async Task<object> MemberCountAsync(
        int userId,
        bool directlyOnly = false,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (userId <= 0)
            return new { Count = 0 };
        var query = _db.Queryable<UserDepth>()
            .Where(d => d.SiteID == siteId && d.UserID == userId);
        if (directlyOnly)
            query = query.Where(d => d.Depth == 1);
        var count = await query.CountAsync(cancellationToken);
        return new { Count = count };
    }

    /// <summary>团队总余额（对齐 /request/agent/team/moneytotal）。</summary>
    /// <param name="userId">代理</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>{ Money }</returns>
    public async Task<object> MoneyTotalAsync(int userId, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (userId <= 0)
            return new { Money = 0m };
        var childIds = await _db.Queryable<UserDepth>()
            .Where(d => d.SiteID == siteId && d.UserID == userId)
            .Select(d => d.ChildID)
            .ToListAsync(cancellationToken);
        if (childIds.Count == 0)
            return new { Money = 0m };
        var money = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && childIds.Contains(m.UserID))
            .SumAsync(m => m.TotalMoney);
        _ = cancellationToken;
        return new { Money = money };
    }
}

/// <summary>代理团队筛选。</summary>
public sealed class ProxyTeamListInput
{
    /// <summary>代理编号。</summary>
    public int UserID { get; set; }

    /// <summary>最大深度；0=不限。</summary>
    public int MaxDepth { get; set; }
}

/// <summary>团队成员行。</summary>
public sealed class ProxyTeamItemDto
{
    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>深度。</summary>
    public int Depth { get; set; }

    /// <summary>注册时间。</summary>
    public DateTime CreateAt { get; set; }
}

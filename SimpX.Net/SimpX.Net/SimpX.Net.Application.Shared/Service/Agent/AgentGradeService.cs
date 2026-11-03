using SimpX.Net.Data.Entity.Agents;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台代理层级查看与下级设为代理。对齐旧 /user/agent/gradesetting（配置型，不改资金）。
/// </summary>
public sealed class AgentGradeService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public AgentGradeService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 按上级代理列出直属下级（Depth=1），并标注是否已是代理。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<AgentGradeListItemDto>> ListAsync(
        AgentGradeListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new AgentGradeListInput();
        var siteId = _site.RequireSiteId();
        int parentId = 0;
        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var parents = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == input.UserName.Trim())
                .Take(1).ToListAsync(cancellationToken);
            parentId = parents.FirstOrDefault()?.ID
                ?? throw Oops.Oh("#代理用户不存在#");
        }

        // 无上级时列顶层代理（ParentID=0）
        if (parentId <= 0)
        {
            var tops = await _db.Queryable<AgentUser, User>((a, u) => new JoinQueryInfos(
                    JoinType.Inner, a.ID == u.ID && a.SiteID == u.SiteID))
                .Where((a, u) => a.SiteID == siteId && a.ParentID == 0)
                .OrderBy((a, u) => a.ID)
                .Select((a, u) => new AgentGradeListItemDto
                {
                    ID = a.ID,
                    UserName = u.UserName,
                    AgentName = string.Empty,
                    Type = 1
                }).Take(200).ToListAsync(cancellationToken);
            if (input.Type == 0)
                return [];
            return tops;
        }

        var childIds = await _db.Queryable<UserDepth>()
            .Where(d => d.SiteID == siteId && d.UserID == parentId && d.Depth == 1)
            .Select(d => d.ChildID).ToListAsync(cancellationToken);
        if (childIds.Count == 0)
            return [];

        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && childIds.Contains(u.ID))
            .ToListAsync(cancellationToken);
        var agentIds = (await _db.Queryable<AgentUser>()
            .Where(a => a.SiteID == siteId && childIds.Contains(a.ID))
            .Select(a => a.ID).ToListAsync(cancellationToken)).ToHashSet();

        var parentName = input.UserName?.Trim() ?? string.Empty;
        var list = users.Select(u => new AgentGradeListItemDto
        {
            ID = u.ID,
            UserName = u.UserName ?? string.Empty,
            AgentName = parentName,
            Type = agentIds.Contains(u.ID) ? 1 : 0
        }).ToList();

        if (input.Type == 0)
            return list.Where(x => x.Type == 0).ToList();
        if (input.Type == 1)
            return list.Where(x => x.Type == 1).ToList();
        return list;
    }

    /// <summary>
    /// 把会员设为代理，或把代理改回锁定（不做资金操作）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(AgentGradeSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();

        var userId = input.UserID;
        if (userId <= 0 && !string.IsNullOrWhiteSpace(input.UserName))
        {
            var byName = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == input.UserName.Trim())
                .Take(1).ToListAsync(cancellationToken);
            userId = byName.FirstOrDefault()?.ID ?? 0;
        }

        if (userId <= 0)
            throw Oops.Oh("#请选择用户#");

        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.ID == userId).Take(1)
            .ToListAsync(cancellationToken);
        _ = users.FirstOrDefault() ?? throw Oops.Oh("#用户不存在#");

        var parentId = input.ParentID;
        if (parentId < 0 && !string.IsNullOrWhiteSpace(input.ParentName))
        {
            var parents = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == input.ParentName.Trim())
                .Take(1).ToListAsync(cancellationToken);
            parentId = parents.FirstOrDefault()?.ID
                ?? throw Oops.Oh("#上级代理不存在#");
        }

        var agents = await _db.Queryable<AgentUser>()
            .Where(a => a.SiteID == siteId && a.ID == userId).Take(1)
            .ToListAsync(cancellationToken);
        var agent = agents.FirstOrDefault();

        // Status=1 设为代理；0 表示取消代理权限（锁定）
        if (input.AsAgent)
        {
            if (agent is null)
            {
                await _db.Insertable(new AgentUser
                {
                    ID = userId,
                    SiteID = siteId,
                    ParentID = parentId < 0 ? 0 : parentId,
                    CreateAt = DateTime.Now,
                    Status = AgentUser.AgentStatus.Normal,
                    CommissionID = input.CommissionID,
                    ReturnID = input.ReturnID,
                    Permission = 0
                }).ExecuteCommandAsync(cancellationToken);
                return;
            }

            agent.Status = AgentUser.AgentStatus.Normal;
            if (parentId >= 0)
                agent.ParentID = parentId;
            if (input.CommissionID > 0)
                agent.CommissionID = input.CommissionID;
            if (input.ReturnID > 0)
                agent.ReturnID = input.ReturnID;
            await _db.Updateable(agent).ExecuteCommandAsync(cancellationToken);
            return;
        }

        if (agent is null)
            return;
        agent.Status = AgentUser.AgentStatus.Locked;
        await _db.Updateable(agent).ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>层级列表筛选。</summary>
public sealed class AgentGradeListInput
{
    /// <summary>上级用户名；空=顶层代理。</summary>
    public string UserName { get; set; }

    /// <summary>0=会员 1=代理 null/其它=全部。</summary>
    public int? Type { get; set; }
}

/// <summary>层级列表行。</summary>
public sealed class AgentGradeListItemDto
{
    /// <summary>用户编号。</summary>
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>上级名。</summary>
    public string AgentName { get; set; }

    /// <summary>0=会员 1=代理。</summary>
    public int Type { get; set; }
}

/// <summary>保存层级（设代理/锁定）。</summary>
public sealed class AgentGradeSaveInput
{
    /// <summary>用户编号；0 时可按 UserName 解析。</summary>
    public int UserID { get; set; }

    /// <summary>用户名（UserID=0 时用）。</summary>
    public string UserName { get; set; }

    /// <summary>上级代理编号；负数表示不改（除非给了 ParentName）。</summary>
    public int ParentID { get; set; } = -1;

    /// <summary>上级用户名（ParentID&lt;0 时解析）。</summary>
    public string ParentName { get; set; }

    /// <summary>是否设为代理。</summary>
    public bool AsAgent { get; set; } = true;

    /// <summary>佣金方案；0=不改。</summary>
    public int CommissionID { get; set; }

    /// <summary>返点方案；0=不改。</summary>
    public int ReturnID { get; set; }
}

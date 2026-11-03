using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台站点域名（JWT）。对齐旧版 /admin/site/domain/list。
/// </summary>
public sealed class SiteDomainService : ITransient
{
    /// <summary>域名管理权限 GUID。</summary>
    public const string DomainManagePermissionId = "0f41d60ea0a3416996249682e985dfe3";

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。站点域名在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteDomainService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站域名列表（旧版返回）。对应 /admin/site/domain/list。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回</returns>
    public async Task<OldApiResult> ListOldApiAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ListAsync(cancellationToken);
        return OldApiResult.Ok(string.Empty, rows);
    }

    /// <summary>
    /// 当前站域名列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteDomainItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteDomain>()
            .Where(t => t.SiteID == siteId)
            .OrderBy(t => t.Weights, OrderByType.Desc)
            .OrderBy(t => t.ID)
            .ToListAsync(cancellationToken);

        return rows.Select(t => new SiteDomainItemDto
        {
            ID = t.ID,
            Domain = t.Domain ?? string.Empty,
            Link = string.Format("http{0}://{1}", t.IsSSL ? "s" : "", t.Domain ?? string.Empty),
            Weights = t.Weights,
            AgentName = t.AgentName ?? string.Empty,
            IsVisible = t.IsVisible,
            IsSSL = t.IsSSL,
            IsAPP = t.IsAPP,
            Speed = t.Speed
        }).ToList();
    }

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号；0 返回空壳</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SiteDomainDetailDto> InfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (id <= 0)
        {
            return new SiteDomainDetailDto
            {
                Domain = string.Empty,
                AgentName = string.Empty
            };
        }

        var rows = await _db.Queryable<SiteDomain>()
            .Where(t => t.ID == id && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var d = rows.FirstOrDefault() ?? new SiteDomain();
        return new SiteDomainDetailDto
        {
            ID = d.ID,
            Domain = d.Domain ?? string.Empty,
            IsVisible = d.IsVisible,
            IsSSL = d.IsSSL,
            IsAPP = d.IsAPP,
            Speed = d.Speed,
            Weights = d.Weights,
            IsSingle = d.IsSingle,
            AgentName = string.Equals(d.AgentName, "官网", StringComparison.Ordinal) ? string.Empty : (d.AgentName ?? string.Empty)
        };
    }

    /// <summary>
    /// 保存。对齐旧 DomainAgent.SaveSiteDomain：代理账号须存在且为代理，并已有代理域名。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SiteDomainSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Domain))
            throw Oops.Oh("#请输入域名#");

        var siteId = _site.RequireSiteId();
        var domain = NormalizeDomain(input.Domain);
        string agentName;
        var isSingle = false;
        if (string.IsNullOrWhiteSpace(input.AgentName))
        {
            agentName = "官网";
        }
        else
        {
            agentName = input.AgentName.Trim();
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == agentName)
                .Take(1)
                .ToListAsync(cancellationToken);
            var user = users.FirstOrDefault();
            if (user == null)
                throw Oops.Oh("代理账号不存在");
            // 旧 UserType.Agent = 1
            if (user.Type != 1)
                throw Oops.Oh("不是代理");
            var hasAgentDomain = await _db.Queryable<UserDomain>()
                .AnyAsync(d => d.SiteID == siteId && d.UserID == user.ID, cancellationToken);
            if (!hasAgentDomain)
                throw Oops.Oh("请先添加代理域名");
            isSingle = true;
        }

        var dup = await _db.Queryable<SiteDomain>()
            .AnyAsync(t => t.Domain == domain && t.ID != input.ID, cancellationToken);
        if (dup)
            throw Oops.Oh("#当前域名已存在#");

        if (input.ID == 0)
        {
            var entity = new SiteDomain
            {
                SiteID = siteId,
                Domain = domain,
                AgentName = agentName,
                Weights = input.Weights,
                IsVisible = input.IsVisible,
                IsSSL = input.IsSSL,
                IsAPP = input.IsAPP,
                IsSingle = isSingle || input.IsSingle,
                Speed = input.Speed
            };
            await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var existing = await _db.Queryable<SiteDomain>()
            .Where(t => t.ID == input.ID && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = existing.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        row.Domain = domain;
        row.AgentName = agentName;
        row.Weights = input.Weights;
        row.IsVisible = input.IsVisible;
        row.IsSSL = input.IsSSL;
        row.IsAPP = input.IsAPP;
        row.IsSingle = isSingle || input.IsSingle;
        row.Speed = input.Speed;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 去掉协议与路径，只留主机名（对齐旧 SaveSiteDomain）。
    /// </summary>
    private static string NormalizeDomain(string raw)
    {
        var domainStr = raw.Trim().Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase);
        var slash = domainStr.IndexOf('/');
        if (slash >= 0)
            domainStr = domainStr[..slash];
        return domainStr;
    }

    /// <summary>
    /// 删除当前站域名。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var ok = await _db.Queryable<SiteDomain>()
            .AnyAsync(t => t.ID == id && t.SiteID == siteId, cancellationToken);
        if (!ok)
            throw Oops.Oh("#编号错误#");
        await _db.Deleteable<SiteDomain>().Where(t => t.ID == id && t.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }
}

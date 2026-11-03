using System.Text.RegularExpressions;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台代理域名。对齐旧版 /admin/user/agent/domain/list|info|save|delete。
/// </summary>
public sealed partial class AgentDomainService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。代理域名在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public AgentDomainService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站代理域名分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<AgentDomainPageOutput> PageAsync(
        AgentDomainPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        // 对齐旧版：未传日期时 StartAt=今天 0 点、EndAt=现在（右开 +1 秒）
        var startAt = input.StartAt ?? DateTime.Now.Date;
        var endExclusive = (input.EndAt ?? DateTime.Now).AddSeconds(1);

        var query = _db.Queryable<UserDomain>()
            .Where(d => d.SiteID == siteId && d.CreateAt >= startAt && d.CreateAt < endExclusive);

        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            var userIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName != null && u.UserName.Contains(name))
                .Select(u => u.ID)
                .ToListAsync(cancellationToken);
            query = query.Where(d => userIds.Contains(d.UserID));
        }

        if (!string.IsNullOrWhiteSpace(input.Invite))
        {
            var inviteKey = input.Invite.Trim();
            query = query.Where(d => d.InviteID != null && d.InviteID.Contains(inviteKey));
        }

        if (!string.IsNullOrWhiteSpace(input.Domain))
        {
            var domainKey = input.Domain.Trim();
            query = query.Where(d => d.Name != null && d.Name.Contains(domainKey));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(d => d.CreateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIdSet = rows.Select(r => r.UserID).Where(id => id > 0).Distinct().ToList();
        var users = userIdSet.Count == 0
            ? new List<User>()
            : await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIdSet.Contains(u.ID))
                .ToListAsync(cancellationToken);
        var userMap = users.ToDictionary(u => u.ID, u => u);

        var parentIds = users.Select(u => u.AgentID).Where(id => id > 0).Distinct().ToList();
        var parents = parentIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && parentIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ID, x => x.UserName ?? string.Empty);

        var domainNames = rows.Select(r => r.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        Dictionary<string, int> memberCounts;
        if (domainNames.Count == 0)
        {
            memberCounts = new Dictionary<string, int>();
        }
        else
        {
            var regRows = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && domainNames.Contains(u.RegDomain))
                .Select(u => u.RegDomain)
                .ToListAsync(cancellationToken);
            memberCounts = regRows
                .GroupBy(n => n ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        var siteRows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var siteNameFallback = siteRows.FirstOrDefault()?.Name ?? string.Empty;

        var data = rows.Select(d =>
        {
            userMap.TryGetValue(d.UserID, out var u);
            var userName = u?.UserName ?? string.Empty;
            var upName = string.Empty;
            if (u != null && u.AgentID > 0 && parents.TryGetValue(u.AgentID, out var pn))
                upName = pn;
            memberCounts.TryGetValue(d.Name ?? string.Empty, out var mc);
            return new AgentDomainListItemDto
            {
                ID = d.ID,
                UserID = d.UserID,
                UserName = userName,
                UpUserName = upName,
                Name = d.Name ?? string.Empty,
                MemberCount = mc,
                SiteName = string.IsNullOrEmpty(d.SiteName) ? siteNameFallback : d.SiteName,
                Logo = d.Logo ?? string.Empty,
                CreateAt = d.CreateAt,
                InviteID = d.InviteID ?? string.Empty,
                DefaultBackground = d.DefaultBackground ?? string.Empty
            };
        }).ToList();

        return new AgentDomainPageOutput
        {
            RecordCount = total,
            Page = page,
            PageSize = pageSize,
            Data = data
        };
    }

    /// <summary>
    /// 当前站域名详情；不存在返回空壳（对齐旧版 info）。
    /// </summary>
    /// <param name="id">域名编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<AgentDomainDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        UserDomain domain = null;
        if (id > 0)
        {
            var rows = await _db.Queryable<UserDomain>()
                .Where(d => d.ID == id && d.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            domain = rows.FirstOrDefault();
        }

        domain ??= new UserDomain();
        var userName = string.Empty;
        if (domain.UserID > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.ID == domain.UserID && u.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            userName = users.FirstOrDefault()?.UserName ?? string.Empty;
        }

        return new AgentDomainDetailDto
        {
            ID = domain.ID,
            Name = domain.Name ?? string.Empty,
            SiteName = domain.SiteName ?? string.Empty,
            Logo = domain.Logo ?? string.Empty,
            UserID = domain.UserID,
            UserName = userName,
            InviteID = domain.InviteID ?? string.Empty,
            DefaultBackground = domain.DefaultBackground ?? string.Empty,
            DefaultTemplate = domain.DefaultTemplate ?? string.Empty
        };
    }

    /// <summary>
    /// 保存代理域名。域名归属与邀请码所属会员对齐；格式对齐旧 WebAgent.IsDomain。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(AgentDomainSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.InviteID))
            throw Oops.Oh("#请输入要绑定的邀请码#");
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入域名#");

        var siteId = _site.RequireSiteId();
        var name = input.Name.Trim().ToLowerInvariant();
        if (!Regex.IsMatch(name, @"^\w[\.\w\-_\:]+$"))
            throw Oops.Oh("#域名格式错误#");
        var inviteId = input.InviteID.Trim();

        var invites = await _db.Queryable<UserInvite>()
            .Where(t => t.ID == inviteId && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var invite = invites.FirstOrDefault() ?? throw Oops.Oh("#邀请码错误#");

        var dup = await _db.Queryable<UserDomain>()
            .AnyAsync(t => t.Name == name && t.ID != input.ID, cancellationToken);
        if (dup)
            throw Oops.Oh("#该域名已绑定#");

        if (input.ID == 0)
        {
            await _db.Insertable(new UserDomain
            {
                SiteID = siteId,
                UserID = invite.UserID,
                Name = name,
                InviteID = inviteId,
                CreateAt = DateTime.Now,
                Logo = input.Logo ?? string.Empty,
                SiteName = input.SiteName ?? string.Empty,
                DefaultBackground = input.DefaultBackground ?? string.Empty,
                DefaultTemplate = input.DefaultTemplate ?? string.Empty
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<UserDomain>()
            .Where(d => d.ID == input.ID && d.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        row.Name = name;
        row.InviteID = inviteId;
        row.UserID = invite.UserID;
        row.Logo = input.Logo ?? string.Empty;
        row.SiteName = input.SiteName ?? string.Empty;
        row.DefaultBackground = input.DefaultBackground ?? string.Empty;
        row.DefaultTemplate = input.DefaultTemplate ?? string.Empty;
        row.CreateAt = DateTime.Now;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除当前站代理域名。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var ok = await _db.Queryable<UserDomain>()
            .AnyAsync(d => d.ID == id && d.SiteID == siteId, cancellationToken);
        if (!ok)
            throw Oops.Oh("#编号错误#");
        await _db.Deleteable<UserDomain>()
            .Where(d => d.ID == id && d.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }

    // --- merged from AgentDomainService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 background。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> BackgroundAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return BackgroundCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：background —— 站点背景类型下拉。</summary>
    private async Task<object?> BackgroundCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SimpX.Net.Data.Entity.Sites.SiteConfig>()
            .Where(c => c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var raw = rows.FirstOrDefault()?.BackgroundType ?? string.Empty;
        var names = new List<string>();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var p in doc.RootElement.EnumerateObject())
                    {
                        var v = p.Value.ValueKind == System.Text.Json.JsonValueKind.String
                            ? p.Value.GetString()
                            : p.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(v))
                            names.Add(v!);
                    }
                }
                else if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var v = el.ValueKind == System.Text.Json.JsonValueKind.String ? el.GetString() : el.ToString();
                        if (!string.IsNullOrWhiteSpace(v))
                            names.Add(v!);
                    }
                }
            }
            catch
            {
                names = raw
                    .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }
        }

        names = names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (names.Count == 0)
            names.Add("Default");
        return names.Select(t => new { Name = t, Type = t }).ToList();
    }


    private static int ReadInt(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var n)) return n;
        }
        return 0;
    }
}

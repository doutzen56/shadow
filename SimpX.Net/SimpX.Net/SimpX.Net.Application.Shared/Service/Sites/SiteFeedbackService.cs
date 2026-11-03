using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点意见反馈。对齐旧 /admin/user/customer/feedback/list|delete。
/// </summary>
public sealed class SiteFeedbackService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    public SiteFeedbackService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>反馈列表（最多 500）。</summary>
    public async Task<List<SiteFeedbackListItemDto>> ListAsync(
        SiteFeedbackListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteFeedbackListInput();
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<SiteFeedback>().Where(f => f.SiteID == siteId);
        var userId = input.UserID;
        if (userId <= 0 && !string.IsNullOrWhiteSpace(input.User))
        {
            var key = input.User.Trim();
            var named = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName.Contains(key))
                .Select(u => u.ID)
                .Take(500)
                .ToListAsync(cancellationToken);
            if (named.Count == 0)
                return new List<SiteFeedbackListItemDto>();
            query = query.Where(f => named.Contains(f.UserID));
        }
        else if (userId > 0)
            query = query.Where(f => f.UserID == userId);
        if (!string.IsNullOrWhiteSpace(input.Content))
            query = query.Where(f => f.Content != null && f.Content.Contains(input.Content.Trim()));
        if (!string.IsNullOrWhiteSpace(input.Mobile))
            query = query.Where(f => f.Mobile != null && f.Mobile.Contains(input.Mobile.Trim()));
        if (!string.IsNullOrWhiteSpace(input.Email))
            query = query.Where(f => f.Email != null && f.Email.Contains(input.Email.Trim()));
        if (input.StartAt.HasValue)
            query = query.Where(f => f.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue)
            query = query.Where(f => f.CreateAt <= input.EndAt.Value);

        var rows = await query.OrderBy(f => f.CreateAt, OrderByType.Desc).Take(500)
            .ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.UserID).Where(id => id > 0).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var names = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName }).ToListAsync(cancellationToken);
            foreach (var n in names)
                nameMap[n.ID] = n.UserName;
        }

        return rows.Select(f => new SiteFeedbackListItemDto
        {
            ID = f.ID,
            UserID = f.UserID,
            UserName = f.UserID == 0
                ? "#游客#"
                : (nameMap.TryGetValue(f.UserID, out var un) ? un : string.Empty),
            CreateAt = f.CreateAt,
            Mobile = f.Mobile ?? string.Empty,
            Email = f.Email ?? string.Empty,
            Content = f.Content ?? string.Empty
        }).ToList();
    }

    /// <summary>删除一条反馈。</summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var ok = await _db.Queryable<SiteFeedback>()
            .AnyAsync(f => f.ID == id && f.SiteID == siteId, cancellationToken);
        if (!ok)
            throw Oops.Oh("#记录不存在#");
        await _db.Deleteable<SiteFeedback>()
            .Where(f => f.ID == id && f.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>反馈筛选。</summary>
public sealed class SiteFeedbackListInput
{
    /// <summary>会员编号；0=全部。</summary>
    public int UserID { get; set; }

    /// <summary>用户名精确匹配（优先于 UserID=0 时解析）。</summary>
    public string User { get; set; }

    /// <summary>内容包含。</summary>
    public string Content { get; set; }

    /// <summary>手机包含。</summary>
    public string Mobile { get; set; }

    /// <summary>邮箱包含。</summary>
    public string Email { get; set; }

    /// <summary>开始时间。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束时间。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>反馈行。</summary>
public sealed class SiteFeedbackListItemDto
{
    public int ID { get; set; }
    public int UserID { get; set; }
    public string UserName { get; set; }
    public DateTime CreateAt { get; set; }
    public string Mobile { get; set; }
    public string Email { get; set; }
    public string Content { get; set; }
}

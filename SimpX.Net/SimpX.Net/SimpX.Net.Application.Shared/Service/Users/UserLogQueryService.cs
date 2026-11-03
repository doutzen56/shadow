using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台会员操作日志只读。对齐旧 /report/user/userlog。
/// </summary>
public sealed class UserLogQueryService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public UserLogQueryService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<UserLogPageOutput> PageAsync(
        UserLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new UserLogPageInput();
        var siteId = _site.RequireSiteId();
        var page = input.Page <= 0 ? 1 : input.Page;
        var pageSize = input.PageSize <= 0 ? 20 : Math.Min(input.PageSize, 100);

        var query = _db.Queryable<UserLog, User>((l, u) => new JoinQueryInfos(
                JoinType.Left, l.UserID == u.ID && l.SiteID == u.SiteID))
            .Where((l, u) => l.SiteID == siteId);

        if (input.UserID > 0)
            query = query.Where((l, u) => l.UserID == input.UserID);
        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where((l, u) => u.UserName.Contains(input.UserName.Trim()));
        if (input.Type.HasValue)
            query = query.Where((l, u) => l.Type == input.Type.Value);
        if (!string.IsNullOrWhiteSpace(input.Content))
            query = query.Where((l, u) => l.Content.Contains(input.Content.Trim()));

        RefAsync<int> total = 0;
        var rows = await query.OrderBy((l, u) => l.CreateAt, OrderByType.Desc)
            .Select((l, u) => new UserLogListItemDto
            {
                ID = l.ID,
                UserID = l.UserID,
                UserName = u.UserName,
                Type = l.Type,
                Content = l.Content,
                IP = l.IP,
                AdminID = l.AdminID,
                CreateAt = l.CreateAt
            })
            .ToPageListAsync(page, pageSize, total, cancellationToken);

        return new UserLogPageOutput { Items = rows, Total = total };
    }
}

/// <summary>会员日志分页入参。</summary>
public sealed class UserLogPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员编号；0=全部。</summary>
    public int UserID { get; set; }

    /// <summary>用户名模糊。</summary>
    public string UserName { get; set; }

    /// <summary>类型；null=全部。</summary>
    public int? Type { get; set; }

    /// <summary>内容模糊。</summary>
    public string Content { get; set; }
}

/// <summary>会员日志分页出参。</summary>
public sealed class UserLogPageOutput
{
    /// <summary>行。</summary>
    public List<UserLogListItemDto> Items { get; set; }

    /// <summary>总数。</summary>
    public int Total { get; set; }
}

/// <summary>会员日志行。</summary>
public sealed class UserLogListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>内容。</summary>
    public string Content { get; set; }

    /// <summary>IP。</summary>
    public string IP { get; set; }

    /// <summary>代操作管理员。</summary>
    public int AdminID { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }
}

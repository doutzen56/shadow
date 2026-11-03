using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 轮盘 / 红包次数日志只读。
/// </summary>
public sealed class PromoChanceLogService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public PromoChanceLogService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>轮盘日志分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<ChanceLogPageOutput> PageDelotteryAsync(
        ChanceLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new ChanceLogPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<UserDelotteryLog>().Where(l => l.SiteID == siteId);
        if (input.UserID > 0)
            query = query.Where(l => l.UserID == input.UserID);
        if (input.Type > 0)
            query = query.Where(l => l.Type == input.Type);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(l => l.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new ChanceLogPageOutput
        {
            Total = total,
            Items = rows.Select(l => new ChanceLogItemDto
            {
                ID = l.ID,
                UserID = l.UserID,
                Point = l.Point,
                Balance = l.Balance,
                Type = l.Type,
                Content = l.Content,
                CreateAt = l.CreateAt
            }).ToList()
        };
    }

    /// <summary>红包日志分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<ChanceLogPageOutput> PageRedEnvelopeAsync(
        ChanceLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new ChanceLogPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<UserRedEnvelopeLog>().Where(l => l.SiteID == siteId);
        if (input.UserID > 0)
            query = query.Where(l => l.UserID == input.UserID);
        if (input.Type > 0)
            query = query.Where(l => l.Type == input.Type);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(l => l.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new ChanceLogPageOutput
        {
            Total = total,
            Items = rows.Select(l => new ChanceLogItemDto
            {
                ID = l.ID,
                UserID = l.UserID,
                Point = l.Point,
                Balance = l.Balance,
                Type = l.Type,
                Content = l.Content,
                CreateAt = l.CreateAt
            }).ToList()
        };
    }
}

/// <summary>次数日志筛选。</summary>
public sealed class ChanceLogPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>类型；0=全部。</summary>
    public int Type { get; set; }
}

/// <summary>次数日志分页。</summary>
public sealed class ChanceLogPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<ChanceLogItemDto> Items { get; set; }
}

/// <summary>次数日志行。</summary>
public sealed class ChanceLogItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>变动。</summary>
    public decimal Point { get; set; }

    /// <summary>余额。</summary>
    public decimal Balance { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>说明。</summary>
    public string Content { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }
}

using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// VIP 升级日志只读。对齐旧 /site/vip/upgradelog。
/// </summary>
public sealed class VipUpgradeLogService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public VipUpgradeLogService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<VipUpgradeLogPageOutput> PageAsync(
        VipUpgradeLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new VipUpgradeLogPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        var query = _db.Queryable<SiteVipUpGradeLog>().Where(l => l.SiteID == siteId);
        if (input.UserID > 0)
            query = query.Where(l => l.UserID == input.UserID);
        else if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var key = input.UserName.Trim();
            query = query.Where(l => l.UserName.Contains(key));
        }

        if (input.Type > 0)
            query = query.Where(l => l.Type == input.Type);
        if (input.StartAt.HasValue)
            query = query.Where(l => l.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue)
            query = query.Where(l => l.CreateAt < input.EndAt.Value.Date.AddDays(1));

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(l => l.LogID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new VipUpgradeLogPageOutput
        {
            Total = total,
            Items = rows.Select(l => new VipUpgradeLogItemDto
            {
                LogID = l.LogID,
                UserID = l.UserID,
                UserName = l.UserName,
                Type = l.Type,
                OldLevel = l.OldLevel,
                NewLevel = l.NewLevel,
                CreateAt = l.CreateAt,
                Status = l.Status,
                Remark = l.Remark
            }).ToList()
        };
    }
}

/// <summary>升级日志筛选。</summary>
public sealed class VipUpgradeLogPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名模糊。</summary>
    public string UserName { get; set; }

    /// <summary>类型；0=全部，1升级，2降级。</summary>
    public int Type { get; set; }

    /// <summary>开始时间。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束时间。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>升级日志分页。</summary>
public sealed class VipUpgradeLogPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<VipUpgradeLogItemDto> Items { get; set; }
}

/// <summary>升级日志行。</summary>
public sealed class VipUpgradeLogItemDto
{
    /// <summary>日志编号。</summary>
    public int LogID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>旧等级。</summary>
    public byte OldLevel { get; set; }

    /// <summary>新等级。</summary>
    public byte NewLevel { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>状态。</summary>
    public bool Status { get; set; }

    /// <summary>说明。</summary>
    public string Remark { get; set; }
}

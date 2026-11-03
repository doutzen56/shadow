using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 活动栏目。对齐旧 EventAgent.SaveColumn / DeleteColumn / GetColumnList。
/// </summary>
public sealed class SiteEventColumnService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>注入业务库与当前站。活动栏目在 BetWin30 业务库，不能走主库 Sqlite。</summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteEventColumnService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 本站栏目列表（排序从大到小）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteEventColumnListItemDto>> ColumnListAsync(
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteEventColumn>()
            .Where(c => c.SiteID == siteId)
            .OrderBy(c => c.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return rows.Select(c => new SiteEventColumnListItemDto
        {
            ID = c.ID,
            Name = c.Name ?? string.Empty,
            Sort = c.Sort,
            EventCount = c.EventCount
        }).ToList();
    }

    /// <summary>
    /// 保存栏目。名称必填；可改排序；始终落本站。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SiteEventColumnSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入分类名称#");

        var siteId = _site.RequireSiteId();
        var name = input.Name.Trim();

        if (input.ID == 0)
        {
            var entity = new SiteEventColumn
            {
                SiteID = siteId,
                Name = name,
                Sort = input.Sort,
                EventCount = 0
            };
            await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<SiteEventColumn>()
            .Where(c => c.ID == input.ID && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("ID错误");
        row.Name = name;
        row.Sort = input.Sort;
        await _db.Updateable(row).UpdateColumns(c => new { c.Name, c.Sort })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除栏目。若 EventCount&gt;0 或仍有活动引用 ColID，则拒绝。
    /// </summary>
    /// <param name="id">栏目编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("ID错误");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteEventColumn>()
            .Where(c => c.ID == id && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("ID错误");

        if (row.EventCount > 0)
            throw Oops.Oh("#栏目下还有活动#");

        var hasEvents = await _db.Queryable<SiteEvent>()
            .AnyAsync(e => e.ColID == id && e.SiteID == siteId, cancellationToken);
        if (hasEvents)
            throw Oops.Oh("#栏目下还有活动#");

        await _db.Deleteable<SiteEventColumn>()
            .Where(c => c.ID == id && c.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>活动栏目列表行。</summary>
public sealed class SiteEventColumnListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }

    /// <summary>活动数量。</summary>
    public int EventCount { get; set; }
}

/// <summary>活动栏目保存入参。</summary>
public sealed class SiteEventColumnSaveInput
{
    /// <summary>编号；0 表示新增。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>排序（从大到小）。</summary>
    public short Sort { get; set; }
}

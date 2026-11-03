using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点游戏分类 / 列表 / 预设开关。
/// </summary>
public sealed partial class SiteGameCatalogService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteGameCatalogService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>分类列表。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteGameCateItemDto>> ListCatesAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameCate>()
            .Where(c => c.SiteID == siteId).OrderBy(c => c.Sort).OrderBy(c => c.ID)
            .ToListAsync(cancellationToken);
        return rows.Select(c => new SiteGameCateItemDto
        {
            ID = c.ID,
            GameName = c.GameName,
            GameCode = c.GameCode,
            GameID = c.GameID,
            GameParentID = c.GameParentID,
            IsEnable = c.IsEnable,
            PcEnable = c.PcEnable,
            AppEnable = c.AppEnable,
            Sort = c.Sort
        }).ToList();
    }

    /// <summary>保存分类开关与排序。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveCateAsync(SiteGameCateSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameCate>()
            .Where(c => c.SiteID == siteId && c.ID == input.ID).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        row.IsEnable = input.IsEnable;
        row.PcEnable = input.PcEnable;
        row.AppEnable = input.AppEnable;
        row.Sort = input.Sort;
        row.UpdateAt = DateTime.Now;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>游戏列表分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<SiteGameListPageOutput> PageGamesAsync(
        SiteGameListPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteGameListPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<SiteGameList>().Where(g => g.SiteID == siteId);
        if (input.Type > 0)
            query = query.Where(g => g.Type == input.Type);
        if (!string.IsNullOrWhiteSpace(input.Name))
        {
            var key = input.Name.Trim();
            query = query.Where(g => g.Name.Contains(key) || g.GameName.Contains(key));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(g => g.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new SiteGameListPageOutput
        {
            Total = total,
            Items = rows.Select(g => new SiteGameListItemDto
            {
                ID = g.ID,
                GameName = g.GameName,
                Name = g.Name,
                Code = g.Code,
                Type = g.Type,
                Category = g.Category,
                IsEnable = g.IsEnable,
                PcEnable = g.PcEnable
            }).ToList()
        };
    }

    /// <summary>保存游戏开关（预设）。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveGameAsync(SiteGameListSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameList>()
            .Where(g => g.SiteID == siteId && g.Type == input.Type && g.Code == input.Code)
            .Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        row.IsEnable = input.IsEnable;
        row.PcEnable = input.PcEnable;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>分类行。</summary>
public sealed class SiteGameCateItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string GameName { get; set; }

    /// <summary>编码。</summary>
    public string GameCode { get; set; }

    /// <summary>游戏枚举。</summary>
    public int GameID { get; set; }

    /// <summary>父级枚举。</summary>
    public int GameParentID { get; set; }

    /// <summary>开启。</summary>
    public bool IsEnable { get; set; }

    /// <summary>PC。</summary>
    public bool PcEnable { get; set; }

    /// <summary>App。</summary>
    public bool AppEnable { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }
}

/// <summary>保存分类。</summary>
public sealed class SiteGameCateSaveInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>开启。</summary>
    public bool IsEnable { get; set; }

    /// <summary>PC。</summary>
    public bool PcEnable { get; set; }

    /// <summary>App。</summary>
    public bool AppEnable { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }
}

/// <summary>游戏列表筛选。</summary>
public sealed class SiteGameListPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>类型；0=全部。</summary>
    public int Type { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }
}

/// <summary>游戏列表分页。</summary>
public sealed class SiteGameListPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<SiteGameListItemDto> Items { get; set; }
}

/// <summary>游戏列表行。</summary>
public sealed class SiteGameListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>游戏名。</summary>
    public string GameName { get; set; }

    /// <summary>显示名。</summary>
    public string Name { get; set; }

    /// <summary>代码。</summary>
    public string Code { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>分类。</summary>
    public int Category { get; set; }

    /// <summary>开启。</summary>
    public bool IsEnable { get; set; }

    /// <summary>PC。</summary>
    public bool PcEnable { get; set; }
}

/// <summary>保存游戏开关。</summary>
public sealed class SiteGameListSaveInput
{
    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>代码。</summary>
    public string Code { get; set; }

    /// <summary>开启。</summary>
    public bool IsEnable { get; set; }

    /// <summary>PC。</summary>
    public bool PcEnable { get; set; }
}

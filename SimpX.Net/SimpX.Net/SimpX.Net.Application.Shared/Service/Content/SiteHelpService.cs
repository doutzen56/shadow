using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台帮助中心。对齐旧版 /admin/site/help/list|info|save|delete|columnlist。
/// </summary>
public sealed partial class SiteHelpService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。帮助内容在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteHelpService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 帮助文章列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteHelpListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var columns = await _db.Queryable<HelpColumn>()
            .Where(c => c.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var columnMap = columns.ToDictionary(
            c => c.ID,
            c => SystemSiteService.PickChineseName(c.Name ?? c.ID.ToString()));

        var rows = await _db.Queryable<SiteHelp>()
            .Where(t => t.SiteID == siteId)
            .OrderBy(t => t.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return rows.Select(t => new SiteHelpListItemDto
        {
            ID = t.ID,
            Title = SystemSiteService.PickChineseName(t.Title ?? string.Empty),
            ColID = t.ColID,
            Column = columnMap.TryGetValue(t.ColID, out var cn) ? cn : t.ColID.ToString(),
            CreateAt = t.CreateAt
        }).ToList();
    }

    /// <summary>
    /// 栏目列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>栏目</returns>
    public async Task<List<SiteHelpColumnItemDto>> ColumnListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var list = await _db.Queryable<HelpColumn>()
            .Where(t => t.SiteID == siteId)
            .OrderBy(t => t.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return list.Select(t => new SiteHelpColumnItemDto
        {
            ID = t.ID,
            Name = SystemSiteService.PickChineseName(t.Name ?? string.Empty),
            Count = t.Count,
            Sort = t.Sort,
            CreateAt = t.CreateAt,
            IsFooter = t.IsFooter,
            IsUrl = !string.IsNullOrEmpty(t.Url)
        }).ToList();
    }

    /// <summary>
    /// 帮助详情；编号 0 或不存在返回新建默认值。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SiteHelpDetailDto> InfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        SiteHelp help = null;
        if (id > 0)
        {
            var rows = await _db.Queryable<SiteHelp>()
                .Where(t => t.ID == id && t.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            help = rows.FirstOrDefault();
        }

        help ??= new SiteHelp
        {
            CreateAt = DateTime.Now,
            Content = "{}",
            Title = "{}"
        };

        return new SiteHelpDetailDto
        {
            ID = help.ID,
            ColID = help.ColID,
            Title = help.Title ?? "{}",
            Content = help.Content ?? "{}",
            Sort = help.Sort,
            CreateAt = help.CreateAt
        };
    }

    /// <summary>
    /// 保存帮助。对齐旧版 SaveSiteHelp；联动改栏目文章数。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SiteHelpSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Title))
            throw Oops.Oh("#请输入标题#");
        if (string.IsNullOrWhiteSpace(input.Content))
            throw Oops.Oh("#请输入内容#");
        if (input.ColID == 0)
            throw Oops.Oh("#请选择栏目#");

        var siteId = _site.RequireSiteId();
        var colOk = await _db.Queryable<HelpColumn>()
            .AnyAsync(c => c.ID == input.ColID && c.SiteID == siteId, cancellationToken);
        if (!colOk)
            throw Oops.Oh("#请选择栏目#");

        if (input.ID == 0)
        {
            await _db.Insertable(new SiteHelp
            {
                SiteID = siteId,
                ColID = input.ColID,
                Title = input.Title,
                Content = input.Content,
                Sort = input.Sort,
                CreateAt = DateTime.Now
            }).ExecuteCommandAsync(cancellationToken);
            await BumpColumnCountAsync(siteId, input.ColID, delta: 1, cancellationToken);
            return;
        }

        var rows = await _db.Queryable<SiteHelp>()
            .Where(t => t.ID == input.ID && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        var oldCol = row.ColID;
        row.ColID = input.ColID;
        row.Title = input.Title;
        row.Content = input.Content;
        row.Sort = input.Sort;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
        if (oldCol != input.ColID)
        {
            await BumpColumnCountAsync(siteId, input.ColID, delta: 1, cancellationToken);
            await BumpColumnCountAsync(siteId, oldCol, delta: -1, cancellationToken);
        }
    }

    /// <summary>
    /// 删除当前站帮助，并减栏目文章数。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteHelp>()
            .Where(t => t.ID == id && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        var colId = row.ColID;
        await _db.Deleteable<SiteHelp>()
            .Where(t => t.ID == id && t.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        await BumpColumnCountAsync(siteId, colId, delta: -1, cancellationToken);
    }

    /// <summary>
    /// 栏目文章数加减，不低于 0。
    /// </summary>
    private async Task BumpColumnCountAsync(
        int siteId,
        int colId,
        int delta,
        CancellationToken cancellationToken)
    {
        if (colId <= 0 || delta == 0)
            return;
        var cols = await _db.Queryable<HelpColumn>()
            .Where(c => c.ID == colId && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var col = cols.FirstOrDefault();
        if (col == null)
            return;
        col.Count = Math.Max(0, col.Count + delta);
        await _db.Updateable(col).UpdateColumns(c => new { c.Count })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>栏目详情。对应旧 columninfo。</summary>
    public async Task<object> ColumnInfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        HelpColumn col = null;
        if (id > 0)
        {
            var rows = await _db.Queryable<HelpColumn>()
                .Where(c => c.ID == id && c.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            col = rows.FirstOrDefault();
        }

        col ??= new HelpColumn { SiteID = siteId, Name = "{}", CreateAt = DateTime.Now };
        return new
        {
            col.ID,
            col.SiteID,
            Name = col.Name ?? "{}",
            col.CreateAt,
            col.Count,
            col.Sort,
            Url = col.Url ?? string.Empty,
            col.IsFooter
        };
    }

    /// <summary>保存栏目。对应旧 columnsave。</summary>
    public async Task SaveColumnAsync(SiteHelpColumnSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入栏目名称#");
        var siteId = _site.RequireSiteId();
        var name = input.Name.Trim();
        if (input.ID <= 0)
        {
            await _db.Insertable(new HelpColumn
            {
                SiteID = siteId,
                Name = name,
                Sort = input.Sort,
                Url = input.Url ?? string.Empty,
                IsFooter = input.IsFooter,
                CreateAt = DateTime.Now,
                Count = 0
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<HelpColumn>()
            .Where(c => c.ID == input.ID && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var col = rows.FirstOrDefault() ?? throw Oops.Oh("#栏目不存在#");
        col.Name = name;
        col.Sort = input.Sort;
        col.Url = input.Url ?? string.Empty;
        col.IsFooter = input.IsFooter;
        await _db.Updateable(col)
            .UpdateColumns(c => new { c.Name, c.Sort, c.Url, c.IsFooter })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>删除栏目。对应旧 columndelete。</summary>
    public async Task DeleteColumnAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#栏目不存在#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<HelpColumn>()
            .Where(c => c.ID == id && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var col = rows.FirstOrDefault() ?? throw Oops.Oh("#栏目不存在#");
        if (col.Count > 0)
            throw Oops.Oh("#栏目下还有文章#");
        var has = await _db.Queryable<SiteHelp>()
            .AnyAsync(h => h.SiteID == siteId && h.ColID == id, cancellationToken);
        if (has)
            throw Oops.Oh("#栏目下还有文章#");
        await _db.Deleteable<HelpColumn>()
            .Where(c => c.ID == id && c.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }

    // --- merged from SiteHelpService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 columnsort。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ColumnsortAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ColumnsortCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：columnsort — ID 数组顺序写 Sort。</summary>
    private async Task<object?> ColumnsortCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var raw = input.TryGetValue("ID", out var v) ? v?.ToString() ?? string.Empty
            : input.TryGetValue("Id", out var v2) ? v2?.ToString() ?? string.Empty : string.Empty;
        var ids = raw.Split([',', '|', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .ToArray();
        short sort = 0;
        foreach (var id in ids)
        {
            var rows = await _db.Queryable<HelpColumn>()
                .Where(c => c.SiteID == siteId && c.ID == id).Take(1).ToListAsync(cancellationToken);
            var col = rows.FirstOrDefault();
            if (col == null) continue;
            col.Sort = sort++;
            await _db.Updateable(col).UpdateColumns(c => c.Sort).ExecuteCommandAsync(cancellationToken);
        }

        return null;
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

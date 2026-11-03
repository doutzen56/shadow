using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台新闻公告。业务字段与校验对齐旧版 /admin/site/news/*。
/// </summary>
public sealed partial class SiteNewsService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。新闻公告在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteNewsService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站新闻分页。对齐旧版 list。
    /// </summary>
    /// <param name="input">筛选与分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    public async Task<SiteNewsPageOutput> PageAsync(
        SiteNewsPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        var columns = await _db.Queryable<NewsColumn>()
            .Where(c => c.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var columnMap = columns.ToDictionary(c => c.ID, c => c.Name ?? string.Empty);

        var query = _db.Queryable<News>().Where(n => n.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.Title))
        {
            var title = input.Title.Trim();
            query = query.Where(n => n.Title != null && n.Title.Contains(title));
        }

        if (input.ColID != 0)
            query = query.Where(n => n.ColID == input.ColID);

        if (!string.IsNullOrEmpty(input.IsTip))
        {
            var tipOn = string.Equals(input.IsTip, "1", StringComparison.Ordinal);
            query = query.Where(n => n.IsTipAfterLogin == tipOn || n.IsTipBeforeLogin == tipOn);
        }

        if (input.IsTop == 1)
            query = query.Where(n => n.Sort != 0);

        var total = await query.CountAsync(cancellationToken);
        var includeContent = input.Content == 1;
        var rows = await query
            .OrderBy(n => n.Sort, OrderByType.Desc)
            .OrderBy(n => n.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var data = rows.Select(n => new SiteNewsListItemDto
        {
            ID = n.ID,
            Column = columnMap.TryGetValue(n.ColID, out var colName) ? colName : string.Empty,
            Title = n.Title ?? string.Empty,
            Cover = n.Cover ?? string.Empty,
            CreateAt = n.CreateAt,
            IsTip = n.IsTipAfterLogin || n.IsTipBeforeLogin,
            Content = includeContent ? (n.Content ?? string.Empty) : string.Empty,
            IsTop = n.Sort > 0,
            StartAt = n.StartAt,
            EndAt = n.EndAt,
            Sort = n.Sort
        }).ToList();

        return new SiteNewsPageOutput
        {
            RecordCount = total,
            PageIndex = page,
            PageSize = pageSize,
            Data = data
        };
    }

    /// <summary>
    /// 当前站新闻详情；ID≤0 或不存在时返回新建默认值（对齐旧版 info）。
    /// </summary>
    /// <param name="id">新闻编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SiteNewsDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        News news = null;
        if (id > 0)
        {
            var rows = await _db.Queryable<News>()
                .Where(n => n.ID == id && n.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            news = rows.FirstOrDefault();
        }

        news ??= new News
        {
            ID = id > 0 ? id : 0,
            Title = "{}",
            Content = "{}",
            Cover = "{}",
            CreateAt = DateTime.Now,
            StartAt = DateTime.Now.Date,
            EndAt = DateTime.Now.AddDays(30).Date
        };

        return ToDetail(news);
    }

    /// <summary>
    /// 保存新闻。校验与旧版 SaveNewsInfo 一致。会写库，勿对 BetWin30 做自动化写测。
    /// </summary>
    /// <param name="input">保存入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SiteNewsSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrEmpty(input.Title))
            throw Oops.Oh("#请输入新闻标题#");
        if (string.IsNullOrEmpty(input.Content))
            throw Oops.Oh("#请输入新闻内容#");

        var siteId = _site.RequireSiteId();
        var colOk = await _db.Queryable<NewsColumn>()
            .AnyAsync(c => c.ID == input.ColID && c.SiteID == siteId, cancellationToken);
        if (!colOk)
            throw Oops.Oh("#请选择所在栏目#");

        var createAt = input.CreateAt.Year < 2000 ? DateTime.Now : input.CreateAt;

        if (input.ID == 0)
        {
            var entity = new News
            {
                SiteID = siteId,
                ColID = input.ColID,
                Cover = input.Cover ?? string.Empty,
                Title = input.Title,
                Content = input.Content,
                CreateAt = createAt,
                IsTipAfterLogin = input.IsTipAfterLogin,
                IsTipBeforeLogin = input.IsTipBeforeLogin,
                Sort = input.Sort,
                StartAt = input.StartAt,
                EndAt = input.EndAt
            };
            await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var existing = await _db.Queryable<News>()
            .Where(n => n.ID == input.ID && n.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = existing.FirstOrDefault() ?? throw Oops.Oh("#该新闻已经被删除#");

        row.ColID = input.ColID;
        row.Cover = input.Cover ?? string.Empty;
        row.Title = input.Title;
        row.Content = input.Content;
        row.CreateAt = createAt;
        row.IsTipAfterLogin = input.IsTipAfterLogin;
        row.IsTipBeforeLogin = input.IsTipBeforeLogin;
        row.Sort = input.Sort;
        row.StartAt = input.StartAt;
        row.EndAt = input.EndAt;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除当前站新闻，并删掉 site_Language 里 Category=新闻 的扩展行（对齐旧版）。
    /// </summary>
    /// <param name="id">新闻编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<News>()
            .Where(n => n.ID == id && n.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            throw Oops.Oh("#编号错误#");

        await _db.Deleteable<News>().Where(n => n.ID == id && n.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        // SiteLanguageType.NewsLanguage = 0
        await _db.Deleteable<SiteLanguage>()
            .Where(l => l.SiteID == siteId && l.Category == 0 && l.ExpandID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 设定/取消置顶。对齐旧版 updatesort（Sort=1 或 0）。会写库，勿对 BetWin30 做自动化写测。
    /// </summary>
    /// <param name="id">新闻编号</param>
    /// <param name="isTop">是否置顶</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateTopAsync(int id, bool isTop, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<News>()
            .Where(n => n.ID == id && n.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var news = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        news.Sort = isTop ? 1 : 0;
        await _db.Updateable(news).UpdateColumns(n => new { n.Sort }).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 当前站栏目列表。对齐旧版 columnlist。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>栏目行</returns>
    public async Task<List<SiteNewsColumnItemDto>> ColumnListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var list = await _db.Queryable<NewsColumn>()
            .Where(c => c.SiteID == siteId)
            .OrderBy(c => c.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return list.Select(c => new SiteNewsColumnItemDto
        {
            ID = c.ID,
            Name = c.Name ?? string.Empty,
            Code = c.Code ?? string.Empty,
            Sort = c.Sort
        }).ToList();
    }

    /// <summary>
    /// 转详情 DTO。
    /// </summary>
    private static SiteNewsDetailDto ToDetail(News news) => new()
    {
        ID = news.ID,
        ColID = news.ColID,
        Title = news.Title ?? "{}",
        Content = news.Content ?? "{}",
        CreateAt = news.CreateAt.ToString("yyyy-MM-dd HH:mm:ss"),
        IsTipAfterLogin = news.IsTipAfterLogin,
        IsTipBeforeLogin = news.IsTipBeforeLogin,
        Cover = string.IsNullOrEmpty(news.Cover) ? "{}" : news.Cover,
        Sort = news.Sort,
        StartAt = news.StartAt.ToString("yyyy-MM-dd HH:mm:ss"),
        EndAt = news.EndAt.ToString("yyyy-MM-dd HH:mm:ss")
    };

    /// <summary>保存栏目。对应旧 savecolumn。</summary>
    public async Task SaveColumnAsync(SiteNewsColumnSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入栏目名称#");
        var siteId = _site.RequireSiteId();
        var name = input.Name.Trim();
        var code = input.Code?.Trim() ?? string.Empty;
        if (input.ID <= 0)
        {
            await _db.Insertable(new NewsColumn
            {
                SiteID = siteId,
                Name = name,
                Code = code,
                Sort = input.Sort
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<NewsColumn>()
            .Where(c => c.ID == input.ID && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var col = rows.FirstOrDefault() ?? throw Oops.Oh("#栏目不存在#");
        col.Name = name;
        if (!string.IsNullOrEmpty(code))
            col.Code = code;
        if (input.Sort != 0)
            col.Sort = input.Sort;
        await _db.Updateable(col)
            .UpdateColumns(c => new { c.Name, c.Code, c.Sort })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>只改栏目 Code。对应旧 savecolumncode。</summary>
    public async Task SaveColumnCodeAsync(int id, string code, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#栏目不存在#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<NewsColumn>()
            .SetColumns(c => c.Code == (code ?? string.Empty))
            .Where(c => c.ID == id && c.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#栏目不存在#");
    }

    /// <summary>删除栏目及下属新闻。对应旧 deletecolumn。</summary>
    public async Task DeleteColumnAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#栏目不存在#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<NewsColumn>()
            .Where(c => c.ID == id && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        _ = rows.FirstOrDefault() ?? throw Oops.Oh("#栏目不存在#");
        await _db.Deleteable<News>()
            .Where(n => n.SiteID == siteId && n.ColID == id)
            .ExecuteCommandAsync(cancellationToken);
        await _db.Deleteable<NewsColumn>()
            .Where(c => c.ID == id && c.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>栏目排序。对应旧 savecolumnsort。</summary>
    public async Task SaveColumnSortAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Length == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var sort = ids.Length;
        foreach (var id in ids.Where(x => x > 0))
        {
            await _db.Updateable<NewsColumn>()
                .SetColumns(c => c.Sort == (short)sort)
                .Where(c => c.ID == id && c.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
            sort--;
        }
    }

    // --- merged from SiteNewsService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 getcolumninfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetcolumninfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetcolumninfoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatesort。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatesortAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatesortCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：getcolumninfo</summary>
    private async Task<object?> GetcolumninfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        var siteId = _site.RequireSiteId();
        NewsColumn column = null;
        if (id > 0)
        {
            var rows = await _db.Queryable<NewsColumn>()
                .Where(c => c.SiteID == siteId && c.ID == id).Take(1).ToListAsync(cancellationToken);
            column = rows.FirstOrDefault();
        }

        column ??= new NewsColumn();
        return new
        {
            column.ID,
            column.Name,
            column.Code,
            column.Sort
        };
    }


    /// <summary>核心：updatesort（旧名 updatesort = 置顶，对齐 UpdateNewsIsTop）</summary>
    private async Task<object?> UpdatesortCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        var isTop = ReadInt(input, "IsTop", "isTop") == 1;
        await UpdateTopAsync(id, isTop, cancellationToken);
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

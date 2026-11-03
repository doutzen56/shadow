using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 活动会员导入。对齐旧 /site/admin/eventimportusers。
/// </summary>
public sealed class EventImportAdminService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public EventImportAdminService(IBizDbAccessor biz, ICurrentSiteAccessor site)
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
    public async Task<SimpleImportPageOutput> PageAsync(
        SimpleImportPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SimpleImportPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<EventImport>().Where(t => t.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.Keyword))
            query = query.Where(t => t.UserName.Contains(input.Keyword.Trim()));
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(t => t.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new SimpleImportPageOutput
        {
            Total = total,
            Items = rows.Select(r => new SimpleImportItemDto
            {
                ID = r.ID,
                Value = r.UserName,
                CreateAt = r.CreateAt
            }).ToList()
        };
    }

    /// <summary>
    /// 批量追加用户名（去空、去重）。
    /// </summary>
    /// <param name="input">多行文本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>写入条数</returns>
    public async Task<int> AddBatchAsync(SimpleImportBatchInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var names = SplitLines(input.Text);
        if (names.Count == 0)
            return 0;
        var now = DateTime.Now;
        var entities = names.Select(n => new EventImport
        {
            SiteID = siteId,
            UserName = n,
            CreateAt = now
        }).ToList();
        return await _db.Insertable(entities).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除一行。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<EventImport>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    internal static List<string> SplitLines(string text) =>
        (text ?? string.Empty)
            .Split(['\r', '\n', ',', ';', '，', '；'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2000)
            .ToList();
}

/// <summary>
/// 号码导入。对齐旧 /site/admin/importmobile。
/// </summary>
public sealed class MobileImportAdminService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public MobileImportAdminService(IBizDbAccessor biz, ICurrentSiteAccessor site)
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
    public async Task<SimpleImportPageOutput> PageAsync(
        SimpleImportPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SimpleImportPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<MobileImport>().Where(t => t.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var key = input.Keyword.Trim();
            query = query.Where(t => t.Mobile.Contains(key));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(t => t.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new SimpleImportPageOutput
        {
            Total = total,
            Items = rows.Select(r => new SimpleImportItemDto
            {
                ID = r.ID,
                Value = MaskMobile(r.Mobile),
                CreateAt = r.CreateAt
            }).ToList()
        };
    }

    /// <summary>
    /// 批量追加手机号。
    /// </summary>
    /// <param name="input">多行文本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>写入条数</returns>
    public async Task<int> AddBatchAsync(SimpleImportBatchInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var mobiles = EventImportAdminService.SplitLines(input.Text);
        if (mobiles.Count == 0)
            return 0;
        var now = DateTime.Now;
        var entities = mobiles.Select(m => new MobileImport
        {
            SiteID = siteId,
            Mobile = m,
            CreateAt = now
        }).ToList();
        return await _db.Insertable(entities).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除一行。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<MobileImport>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    private static string MaskMobile(string mobile)
    {
        if (string.IsNullOrEmpty(mobile) || mobile.Length < 7)
            return "****";
        return mobile[..3] + "****" + mobile[^4..];
    }
}

/// <summary>简单导入筛选。</summary>
public sealed class SimpleImportPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>关键字。</summary>
    public string Keyword { get; set; }
}

/// <summary>批量导入。</summary>
public sealed class SimpleImportBatchInput
{
    /// <summary>多行文本。</summary>
    public string Text { get; set; }
}

/// <summary>简单导入分页。</summary>
public sealed class SimpleImportPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<SimpleImportItemDto> Items { get; set; }
}

/// <summary>简单导入行。</summary>
public sealed class SimpleImportItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>展示值（用户名或脱敏手机号）。</summary>
    public string Value { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }
}

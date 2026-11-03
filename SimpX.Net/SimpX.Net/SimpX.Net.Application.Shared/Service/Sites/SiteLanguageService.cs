using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点多语言内容（site_Language）只读与保存。
/// </summary>
public sealed class SiteLanguageService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteLanguageService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>列表（对齐 /admin/site/language/list）。</summary>
    /// <param name="category">类型；&lt;0=全部</param>
    /// <param name="expandId">ExpandID；0=不限</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<object>> ListAsync(
        int category = -1,
        int expandId = 0,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<SiteLanguage>().Where(l => l.SiteID == siteId);
        if (category >= 0)
            query = query.Where(l => l.Category == category);
        if (expandId > 0)
            query = query.Where(l => l.ExpandID == expandId);
        var rows = await query.OrderBy(l => l.ID, OrderByType.Desc).Take(500).ToListAsync(cancellationToken);
        return rows.Select(l => (object)new
        {
            l.ID,
            l.Category,
            l.Language,
            l.ExpandID,
            l.Content
        }).ToList();
    }

    /// <summary>编辑器取内容。对齐旧 /admin/site/editor/info（Type+Language+ExpandId）。</summary>
    public async Task<object> EditorInfoAsync(
        int category,
        int language,
        int expandId,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<SiteLanguage>()
            .Where(l => l.SiteID == siteId && l.Category == category && l.Language == language);
        if (expandId > 0)
            query = query.Where(l => l.ExpandID == expandId);
        var rows = await query.Take(1).ToListAsync(cancellationToken);
        return new { Context = rows.FirstOrDefault()?.Content ?? string.Empty };
    }

    /// <summary>详情（对齐 /admin/site/language/info）。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<object> InfoAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteLanguage>()
            .Where(l => l.SiteID == siteId && l.ID == id).Take(1).ToListAsync(cancellationToken);
        var l = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new
        {
            l.ID,
            l.Category,
            l.Language,
            l.ExpandID,
            l.Content
        };
    }

    /// <summary>保存（对齐 /admin/site/language/save）。</summary>
    /// <param name="id">编号；0=新建</param>
    /// <param name="category">类型</param>
    /// <param name="language">语言</param>
    /// <param name="expandId">扩展编号</param>
    /// <param name="content">内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAsync(
        int id,
        int category,
        int language,
        int expandId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (id > 0)
        {
            var rows = await _db.Queryable<SiteLanguage>()
                .Where(l => l.SiteID == siteId && l.ID == id).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            row.Category = category;
            row.Language = language;
            row.ExpandID = expandId;
            row.Content = content ?? string.Empty;
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return row.ID;
        }

        return await _db.Insertable(new SiteLanguage
        {
            SiteID = siteId,
            Category = category,
            Language = language,
            ExpandID = expandId,
            Content = content ?? string.Empty
        }).ExecuteReturnIdentityAsync(cancellationToken);
    }
}

using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台内容区域。对齐旧版 /admin/site/region 与 regioncontent。
/// </summary>
public sealed class SiteRegionService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。内容区域在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteRegionService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站区域分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<SiteRegionPageOutput> PageAsync(
        SiteRegionPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        var query = _db.Queryable<SiteRegion>().Where(r => r.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.Name))
        {
            var name = input.Name.Trim();
            query = query.Where(r => r.Name != null && r.Name.Contains(name));
        }

        if (input.Type.HasValue)
            query = query.Where(r => r.Type == input.Type.Value);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(r => r.CreateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new SiteRegionPageOutput
        {
            RecordCount = total,
            Page = page,
            PageSize = pageSize,
            Data = rows.Select(r => new SiteRegionListItemDto
            {
                ID = r.ID,
                Name = r.Name ?? string.Empty,
                Description = r.Description ?? string.Empty,
                Type = r.Type,
                CreateAt = r.CreateAt
            }).ToList()
        };
    }

    /// <summary>
    /// 按区域名列出内容项（对齐 /request/site/region/list）。
    /// </summary>
    /// <param name="name">区域名</param>
    /// <param name="adNo">广告号可选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>内容行</returns>
    public async Task<List<SiteRegionContentItemDto>> ListContentByNameAsync(
        string name,
        string adNo = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw Oops.Oh("#参数传递错误#");
        var siteId = _site.RequireSiteId();
        var regions = await _db.Queryable<SiteRegion>()
            .Where(r => r.SiteID == siteId && r.Name == name.Trim())
            .Take(1).ToListAsync(cancellationToken);
        var region = regions.FirstOrDefault() ?? throw Oops.Oh("#参数传递错误#");
        var query = _db.Queryable<SiteRegionContent>()
            .Where(c => c.SiteID == siteId && c.RegionID == region.ID && c.IsShow);
        if (!string.IsNullOrWhiteSpace(adNo))
            query = query.Where(c => c.AdNo == adNo.Trim());
        var rows = await query.OrderBy(c => c.Sort).ToListAsync(cancellationToken);
        return rows.Select(ToContentItem).ToList();
    }

    /// <summary>
    /// 按名称取区域内容（会员侧 /request/content/region/get）。
    /// </summary>
    /// <param name="name">区域名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>内容原文；不存在返回空 Content</returns>
    public async Task<object> GetContentByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (string.IsNullOrWhiteSpace(name))
            return new { Content = string.Empty };
        var key = name.Trim();
        var rows = await _db.Queryable<SiteRegion>()
            .Where(r => r.SiteID == siteId && r.Name == key)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        return new { Content = row?.Content ?? string.Empty };
    }

    /// <summary>
    /// 区域详情；编号 0 或不存在返回新建默认值。
    /// </summary>
    /// <param name="id">区域编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SiteRegionDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        SiteRegion region = null;
        if (id > 0)
        {
            var rows = await _db.Queryable<SiteRegion>()
                .Where(r => r.ID == id && r.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            region = rows.FirstOrDefault();
        }

        region ??= new SiteRegion
        {
            Content = "{\"Interval\":0,\"Items\":[]}",
            CreateAt = DateTime.Now
        };

        return new SiteRegionDetailDto
        {
            ID = region.ID,
            Name = region.Name ?? string.Empty,
            Description = region.Description ?? string.Empty,
            Content = region.Content ?? string.Empty,
            Type = region.Type,
            IsSiteGif = region.IsSiteGif,
            GifType = region.GifType,
            CreateAt = region.CreateAt
        };
    }

    /// <summary>
    /// 保存区域。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SiteRegionSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入区域名称#");

        var siteId = _site.RequireSiteId();
        var name = input.Name.Trim();
        var content = string.IsNullOrEmpty(input.Content) ? "{\"Interval\":0,\"Items\":[]}" : input.Content;

        if (input.ID == 0)
        {
            await _db.Insertable(new SiteRegion
            {
                SiteID = siteId,
                Name = name,
                Description = input.Description ?? string.Empty,
                Content = content,
                Type = input.Type,
                IsSiteGif = input.IsSiteGif,
                GifType = input.GifType,
                CreateAt = DateTime.Now
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<SiteRegion>()
            .Where(r => r.ID == input.ID && r.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        row.Name = name;
        row.Description = input.Description ?? string.Empty;
        row.Content = content;
        row.Type = input.Type;
        row.IsSiteGif = input.IsSiteGif;
        row.GifType = input.GifType;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除区域及其内容行。
    /// </summary>
    /// <param name="id">区域编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var ok = await _db.Queryable<SiteRegion>()
            .AnyAsync(r => r.ID == id && r.SiteID == siteId, cancellationToken);
        if (!ok)
            throw Oops.Oh("#编号错误#");

        await _db.Deleteable<SiteRegionContent>()
            .Where(c => c.RegionID == id && c.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        await _db.Deleteable<SiteRegion>()
            .Where(r => r.ID == id && r.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 某区域下的内容列表。
    /// </summary>
    /// <param name="regionId">区域编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>内容行</returns>
    public async Task<List<SiteRegionContentItemDto>> ContentListAsync(
        int regionId,
        CancellationToken cancellationToken = default)
    {
        if (regionId <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var regionOk = await _db.Queryable<SiteRegion>()
            .AnyAsync(r => r.ID == regionId && r.SiteID == siteId, cancellationToken);
        if (!regionOk)
            throw Oops.Oh("#编号错误#");

        var rows = await _db.Queryable<SiteRegionContent>()
            .Where(c => c.RegionID == regionId && c.SiteID == siteId)
            .OrderBy(c => c.Sort, OrderByType.Desc)
            .OrderBy(c => c.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return rows.Select(ToContentItem).ToList();
    }

    /// <summary>
    /// 保存区域内容。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ContentSaveAsync(
        SiteRegionContentSaveInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.RegionID <= 0)
            throw Oops.Oh("#编号错误#");

        var siteId = _site.RequireSiteId();
        var regionOk = await _db.Queryable<SiteRegion>()
            .AnyAsync(r => r.ID == input.RegionID && r.SiteID == siteId, cancellationToken);
        if (!regionOk)
            throw Oops.Oh("#编号错误#");

        var expireAt = input.ExpireAt ?? DateTime.Now.AddYears(10);
        if (input.ID == 0)
        {
            await _db.Insertable(new SiteRegionContent
            {
                SiteID = siteId,
                RegionID = input.RegionID,
                AdNo = input.AdNo ?? string.Empty,
                Title = input.Title ?? string.Empty,
                Content = input.Content ?? string.Empty,
                Remark = input.Remark ?? string.Empty,
                Url = input.Url ?? string.Empty,
                Sort = input.Sort,
                Pic = input.Pic ?? string.Empty,
                IsShow = input.IsShow,
                ClickCount = 0,
                ExpireAt = expireAt,
                CreateAt = DateTime.Now,
                Type = input.Type
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<SiteRegionContent>()
            .Where(c => c.ID == input.ID && c.SiteID == siteId && c.RegionID == input.RegionID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        row.AdNo = input.AdNo ?? string.Empty;
        row.Title = input.Title ?? string.Empty;
        row.Content = input.Content ?? string.Empty;
        row.Remark = input.Remark ?? string.Empty;
        row.Url = input.Url ?? string.Empty;
        row.Sort = input.Sort;
        row.Pic = input.Pic ?? string.Empty;
        row.IsShow = input.IsShow;
        row.ExpireAt = expireAt;
        row.Type = input.Type;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除区域内容。
    /// </summary>
    /// <param name="id">内容编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ContentDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var ok = await _db.Queryable<SiteRegionContent>()
            .AnyAsync(c => c.ID == id && c.SiteID == siteId, cancellationToken);
        if (!ok)
            throw Oops.Oh("#编号错误#");
        await _db.Deleteable<SiteRegionContent>()
            .Where(c => c.ID == id && c.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 映射内容行。
    /// </summary>
    /// <param name="c">实体</param>
    /// <returns>DTO</returns>
    private static SiteRegionContentItemDto ToContentItem(SiteRegionContent c) => new()
    {
        ID = c.ID,
        RegionID = c.RegionID,
        AdNo = c.AdNo ?? string.Empty,
        Title = c.Title ?? string.Empty,
        Content = c.Content ?? string.Empty,
        Remark = c.Remark ?? string.Empty,
        Url = c.Url ?? string.Empty,
        Sort = c.Sort,
        Pic = c.Pic ?? string.Empty,
        IsShow = c.IsShow,
        ExpireAt = c.ExpireAt,
        CreateAt = c.CreateAt,
        Type = c.Type
    };
}

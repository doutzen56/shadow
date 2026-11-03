using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 国别/地区访问限制。对齐旧 /admin/risk/ip/getblackregion|saveblackregion（site_Config.BlackRegion）。
/// </summary>
public sealed class BlackRegionService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。地区限制写在站点配置，配置在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public BlackRegionService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站已限制地区列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>地区名</returns>
    public async Task<BlackRegionListOutput> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteConfig>()
            .Where(t => t.SiteID == siteId)
            .Select(t => t.BlackRegion)
            .Take(1)
            .ToListAsync(cancellationToken);
        var raw = rows.FirstOrDefault() ?? string.Empty;
        var regions = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
        return new BlackRegionListOutput { Regions = regions };
    }

    /// <summary>
    /// 加入或移除一个地区。对齐旧 SaveBlackRegion。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(BlackRegionSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var value = (input.Value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(value) || value.Contains(','))
            throw Oops.Oh("#地区错误#");

        var siteId = _site.RequireSiteId();
        var current = await ListAsync(cancellationToken);
        var region = current.Regions.ToList();
        if (region.Contains(value) && !input.Save)
            region.Remove(value);
        else if (!region.Contains(value) && input.Save)
            region.Add(value);

        var joined = string.Join(",", region);
        var exists = await _db.Queryable<SiteConfig>().AnyAsync(t => t.SiteID == siteId, cancellationToken);
        if (!exists)
        {
            await _db.Insertable(new SiteConfig
            {
                SiteID = siteId,
                BlackRegion = joined
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var n = await _db.Updateable<SiteConfig>()
            .SetColumns(t => t.BlackRegion == joined)
            .Where(t => t.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#更新失败#");
    }
}

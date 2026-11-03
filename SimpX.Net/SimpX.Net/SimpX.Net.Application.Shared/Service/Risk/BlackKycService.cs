using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 证件黑名单。对齐旧 importblackkyc 列表/增改删。
/// </summary>
public sealed partial class BlackKycService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。证件黑名单在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public BlackKycService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 列表；可选按证件号模糊筛。
    /// </summary>
    /// <param name="keyword">关键字</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<BlackKycListItemDto>> ListAsync(
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var q = _db.Queryable<BlackKycImport>().Where(t => t.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(t => t.BlackKyc.Contains(keyword.Trim()));

        var rows = await q.OrderBy(t => t.ID, OrderByType.Desc).ToListAsync(cancellationToken);
        return rows.Select(t => new BlackKycListItemDto
        {
            ID = t.ID,
            BlackKyc = t.BlackKyc ?? string.Empty,
            Demo = t.Demo ?? string.Empty,
            CreateAt = t.CreateAt
        }).ToList();
    }

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(BlackKycSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var kyc = (input.BlackKyc ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(kyc))
            throw Oops.Oh("#不能为空#");

        var siteId = _site.RequireSiteId();
        if (input.ID == 0)
        {
            var exists = await _db.Queryable<BlackKycImport>()
                .AnyAsync(t => t.SiteID == siteId && t.BlackKyc == kyc, cancellationToken);
            if (exists)
                throw Oops.Oh("#已存在#");

            await _db.Insertable(new BlackKycImport
            {
                SiteID = siteId,
                BlackKyc = kyc,
                Demo = input.Demo ?? string.Empty,
                CreateAt = DateTime.Now,
                RelatedUsers = string.Empty
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<BlackKycImport>()
            .Where(t => t.SiteID == siteId && t.ID == input.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        row.BlackKyc = kyc;
        row.Demo = input.Demo ?? string.Empty;
        await _db.Updateable(row)
            .UpdateColumns(t => new { t.BlackKyc, t.Demo })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var n = await _db.Deleteable<BlackKycImport>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#编号错误#");
    }
}

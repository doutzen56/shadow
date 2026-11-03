using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 手机黑名单。对齐旧 /admin/risk/ip/importbalckmobilelist|addbalckmobile|editbalckmobile|deletebalckmobile。
/// </summary>
public sealed partial class BlackMobileService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。手机黑名单在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public BlackMobileService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 列表；可选按手机号模糊筛。
    /// </summary>
    /// <param name="keyword">手机号关键字</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<BlackMobileListItemDto>> ListAsync(
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var q = _db.Queryable<BlackMobileImport>().Where(t => t.SiteID == siteId);
        var mobile = keyword ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(mobile))
        {
            var digits = System.Text.RegularExpressions.Regex.Replace(mobile, @"[^\d]", "");
            if (digits.StartsWith('0') && digits.Length > 1)
                digits = digits[1..];
            if (!string.IsNullOrEmpty(digits))
                q = q.Where(t => t.BlackMobile.Contains(digits));
        }

        var rows = await q.OrderBy(t => t.ID, OrderByType.Desc).ToListAsync(cancellationToken);
        return rows.Select(t => new BlackMobileListItemDto
        {
            ID = t.ID,
            BlackMobile = t.BlackMobile ?? string.Empty,
            Demo = t.Demo ?? string.Empty,
            IsLock = t.IsLock ?? string.Empty,
            CreateAt = t.CreateAt
        }).ToList();
    }

    /// <summary>
    /// 保存（新建或改）。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(BlackMobileSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var mobile = (input.BlackMobile ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(mobile))
            throw Oops.Oh("#不能为空#");

        var siteId = _site.RequireSiteId();
        if (input.ID == 0)
        {
            var exists = await _db.Queryable<BlackMobileImport>()
                .AnyAsync(t => t.SiteID == siteId && t.BlackMobile == mobile, cancellationToken);
            if (exists)
                throw Oops.Oh("#已存在#");

            await _db.Insertable(new BlackMobileImport
            {
                SiteID = siteId,
                BlackMobile = mobile,
                Demo = input.Demo ?? string.Empty,
                IsLock = "0",
                CreateAt = DateTime.Now
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<BlackMobileImport>()
            .Where(t => t.SiteID == siteId && t.ID == input.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        row.BlackMobile = mobile;
        row.Demo = input.Demo ?? string.Empty;
        await _db.Updateable(row)
            .UpdateColumns(t => new { t.BlackMobile, t.Demo })
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
        var n = await _db.Deleteable<BlackMobileImport>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#编号错误#");
    }
}

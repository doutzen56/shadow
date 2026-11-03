using System.Text.Json;
using SimpX.Net.Data.Entity.Sites;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台站点查询（兼容 /system/site/info/sitelist）。不按域名确定当前站点；靠请求里显式传的 SiteID 过滤。
/// </summary>
public sealed class SystemSiteService : ITransient
{
    /// <summary>站点列表权限 GUID（对齐 SystemAdmin.xml「站点列表」）。</summary>
    public const string SiteListPermissionId = "d8802dfc53aa4c448624aa242d40bc0c";

    /// <summary>站点主机权限 GUID（对齐「站点主机」动作）。</summary>
    public const string SiteHostPermissionId = "8c446b9b544948ee88ba7cc44014b0f9";

    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。站点列表在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemSiteService(IBizDbAccessor biz)
    {
        _db = biz.Db;
    }

    /// <summary>
    /// 站点列表：不传 SiteID 返回多站；传入则只查该站。对齐旧版 sitelist 字段子集。
    /// </summary>
    /// <param name="siteId">站点编号；0 表示不过滤</param>
    /// <param name="nameContains">名称包含（原始 SiteName 字符串模糊）</param>
    /// <param name="statusFilter">状态过滤；null 不过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式；info 是站点数组</returns>
    public async Task<OldApiResult> SiteListAsync(
        int siteId,
        string nameContains,
        int? statusFilter,
        CancellationToken cancellationToken = default)
    {
        var info = await ListItemsAsync(siteId, nameContains, statusFilter, cancellationToken);
        return OldApiResult.Ok(string.Empty, info);
    }

    /// <summary>
    /// 站点列表行（给 JWT / 旧版共用）。
    /// </summary>
    /// <param name="siteId">站点编号；0 不过滤</param>
    /// <param name="nameContains">名称包含</param>
    /// <param name="statusFilter">状态；null 不过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteListItemDto>> ListItemsAsync(
        int siteId,
        string nameContains,
        int? statusFilter,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<Site>();
        if (siteId != 0)
            query = query.Where(s => s.ID == siteId);
        if (!string.IsNullOrWhiteSpace(nameContains))
            query = query.Where(s => s.Name != null && s.Name.Contains(nameContains.Trim()));
        if (statusFilter != null)
            query = query.Where(s => s.Status == statusFilter.Value);

        var rows = await query
            .OrderBy(s => s.Status)
            .OrderBy(s => s.ID, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);

        return rows.Select(s => new SiteListItemDto
        {
            ID = s.ID,
            Name = PickChineseName(s.Name),
            CreateAt = s.CreateAt,
            Status = s.Status,
            GameCredit = s.GameCredit,
            Currency = s.Currency
        }).ToList();
    }

    /// <summary>
    /// 站点主机列表：必须显式 SiteID（总后台不按域名确定当前站点）。
    /// </summary>
    /// <param name="siteId">站点编号；0 视为参数错误</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> HostListAsync(int siteId, CancellationToken cancellationToken = default)
    {
        if (siteId <= 0)
            return OldApiResult.Fail("#请指定SiteID#");

        var rows = await _db.Queryable<SiteHost>()
            .Where(h => h.SiteID == siteId)
            .OrderBy(h => h.ID)
            .Take(2000)
            .ToListAsync(cancellationToken);

        var info = rows.Select(h => new SiteHostItemDto
        {
            ID = h.ID,
            Host = h.Host,
            SiteID = h.SiteID,
            IsEnable = h.IsEnable
        }).ToList();

        return OldApiResult.Ok(string.Empty, info);
    }

    /// <summary>
    /// 从站点名 JSON（如 {"CHN":"..."}）取出中文名；解析失败则原样返回。
    /// </summary>
    /// <param name="raw">库中 SiteName</param>
    /// <returns>展示名</returns>
    public static string PickChineseName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw ?? string.Empty;

        var text = raw.Trim();
        if (!text.StartsWith('{'))
            return text;

        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("CHN", out var chn))
                return chn.GetString() ?? text;
        }
        catch (JsonException)
        {
            // 非标准 JSON 时退回原文
        }

        return text;
    }
}

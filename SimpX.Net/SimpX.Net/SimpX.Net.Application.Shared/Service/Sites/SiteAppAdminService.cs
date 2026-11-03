using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点 APP 包与 APPSetting。对齐旧 site/app list|appinfo|savecustomqrcode。
/// </summary>
public sealed class SiteAppAdminService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>注入。</summary>
    public SiteAppAdminService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 本站已上传 APP 列表。对应旧 list。
    /// </summary>
    public async Task<List<SiteAppListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteAPP>()
            .Where(a => a.SiteID == siteId)
            .OrderBy(a => a.UpdateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(a => new SiteAppListItemDto
        {
            Type = a.Type,
            Name = a.Name ?? string.Empty,
            BundleID = a.BundleID ?? string.Empty,
            Version = a.Version ?? string.Empty,
            UpdateAt = a.UpdateAt,
            Size = FormatSize(a.FileSize),
            FilePath = a.FilePath ?? string.Empty,
            Icon = a.Icon ?? string.Empty,
            DownCount = a.DownCount
        }).ToList();
    }

    /// <summary>
    /// 站点 APPSetting。对应旧 appinfo。
    /// </summary>
    public async Task<object> AppInfoAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = rows.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");
        var map = SiteSettingQuery.Parse(site.APPSettingString);
        var customDownload = SiteSettingQuery.Get(map, "CustomDownload");
        var download = string.IsNullOrWhiteSpace(customDownload)
            ? SiteSettingQuery.Get(map, "Download")
            : customDownload;
        var customQr = SiteSettingQuery.Get(map, "CustomQRCodeShow");
        var qr = SiteSettingQuery.Get(map, "QRCode");
        return new
        {
            Template = SiteSettingQuery.Get(map, "Template"),
            QRCode = qr,
            Download = download,
            QRCodeShow = string.IsNullOrWhiteSpace(customQr) ? qr : customQr,
            CustomQRCodeShow = customQr,
            CustomDownload = customDownload
        };
    }

    /// <summary>
    /// 保存自定义二维码/下载。对应旧 savecustomqrcode。
    /// </summary>
    public async Task SaveCustomQrCodeAsync(
        string customQrCode,
        string customDownload,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = rows.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");
        var map = SiteSettingQuery.Parse(site.APPSettingString);
        map["CustomQRCodeShow"] = customQrCode ?? string.Empty;
        map["CustomDownload"] = customDownload ?? string.Empty;
        site.APPSettingString = SiteSettingQuery.ToQueryString(map);
        await _db.Updateable(site)
            .UpdateColumns(s => new { s.APPSettingString })
            .ExecuteCommandAsync(cancellationToken);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.##}KB";
        return $"{bytes / (1024.0 * 1024.0):0.##}MB";
    }
}

/// <summary>APP 列表行。</summary>
public sealed class SiteAppListItemDto
{
    /// <summary>平台类型。</summary>
    public int Type { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>包名。</summary>
    public string BundleID { get; set; }

    /// <summary>版本。</summary>
    public string Version { get; set; }

    /// <summary>更新时间。</summary>
    public DateTime UpdateAt { get; set; }

    /// <summary>体积文案。</summary>
    public string Size { get; set; }

    /// <summary>下载路径。</summary>
    public string FilePath { get; set; }

    /// <summary>图标。</summary>
    public string Icon { get; set; }

    /// <summary>下载次数。</summary>
    public int DownCount { get; set; }
}

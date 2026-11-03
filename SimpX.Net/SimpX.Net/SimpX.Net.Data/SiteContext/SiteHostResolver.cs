using System.Text.RegularExpressions;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Data;

/// <summary>
/// 按 site_Host 表解析站点；匹配不到时，再兼容主机名里带的 1xxx 站点号（跟现网无转发头时的规则一样）。
/// 先直接查库；以后若要加缓存，语义再跟 SiteHostCaching 对齐。
/// </summary>
public sealed class SiteHostResolver : ISiteHostResolver, ITransient
{
    /// <summary>现网无转发头时从 Authority 抠站点号的规则。</summary>
    public const string ForwardedHostHeaderName = "X-Forwarded-HostX";

    private static readonly Regex SiteIdInHostRegex = new(@"1\d{3}", RegexOptions.Compiled);

    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入库客户端。
    /// </summary>
    /// <param name="db">SqlSugar 客户端</param>
    public SiteHostResolver(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<int?> ResolveSiteIdAsync(string host, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeHost(host);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        try
        {
            // 先按 site_Host.Host 精确匹配（忽略大小写），而且要启用；没有匹配不抛异常
            var matched = await _db.Queryable<SiteHost>()
                .Where(h => h.IsEnable && h.Host != null && h.Host.ToLower() == normalized)
                .Select(h => h.SiteID)
                .Take(1)
                .ToListAsync(cancellationToken);

            if (matched.Count > 0 && matched[0] > 0)
                return matched[0];
        }
        catch (Exception)
        {
            // 本地文件库阶段尚未建业务表（site_Host/Site）时，忽略查库异常，继续走主机名 1xxx 兼容。
        }

        // 兼容：从主机名里取出 1xxx（旧版没有 X-Forwarded-HostX 时的规则）。
        // Site / site_Host 真表在业务库；主库 Sqlite 常只有联调种子，不能拿来否定业务站号。
        var match = SiteIdInHostRegex.Match(host ?? string.Empty);
        if (!match.Success)
            return null;

        if (!int.TryParse(match.Value, out var parsed) || parsed <= 0)
            return null;

        return parsed;
    }

    /// <summary>
    /// 去掉端口、路径、空白，转小写，便于和库里 Host 比对。
    /// </summary>
    /// <param name="host">原始主机或 Authority</param>
    /// <returns>规范化主机名；空输入返回 null</returns>
    public static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        var value = host.Trim();
        var slash = value.IndexOf('/');
        if (slash >= 0)
            value = value[..slash];

        var colon = value.IndexOf(':');
        if (colon >= 0)
            value = value[..colon];

        return string.IsNullOrWhiteSpace(value) ? null : value.ToLowerInvariant();
    }

    /// <summary>
    /// 从主机字符串里抠现网规则的 1xxx 站点号；抠不到返回 null。
    /// </summary>
    /// <param name="host">原始主机或 Authority</param>
    /// <returns>站点号；失败为 null</returns>
    public static int? TryParseSiteIdFromHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        var match = SiteIdInHostRegex.Match(host);
        if (!match.Success)
            return null;

        return int.TryParse(match.Value, out var parsed) && parsed > 0 ? parsed : null;
    }
}


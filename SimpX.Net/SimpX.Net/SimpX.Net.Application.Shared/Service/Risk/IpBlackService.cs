using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台 IP 黑名单。对齐旧版 /admin/risk/ip/blacklist|blackinfo|saveblackip|deleteblackip。
/// </summary>
public sealed partial class IpBlackService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。IP 黑名单在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public IpBlackService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站黑名单列表。
    /// </summary>
    /// <param name="keyword">名称/IP/备注关键字</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<IpBlackListItemDto>> ListAsync(string? keyword = null, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<SiteConfigBlackIP>().Where(t => t.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var key = keyword.Trim();
            var ipNum = TryParseIpNumber(key);
            if (ipNum.HasValue)
                query = query.Where(t => t.StartIP == ipNum.Value || t.EndIP == ipNum.Value
                    || (t.StartIP <= ipNum.Value && t.EndIP >= ipNum.Value));
            else
                query = query.Where(t =>
                    (t.Name != null && t.Name.Contains(key))
                    || (t.Remark != null && t.Remark.Contains(key)));
        }
        var rows = await query
            .OrderBy(t => t.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return rows.Select(t => new IpBlackListItemDto
        {
            ID = t.ID,
            Name = t.Name ?? string.Empty,
            CreateAt = t.CreateAt,
            StartIP = FormatIp(t.StartIP),
            EndIP = FormatIp(t.EndIP),
            Remark = t.Remark ?? string.Empty
        }).ToList();
    }

    /// <summary>
    /// 详情；须属于当前站。编号 0 或不存在返回空壳（对齐旧可空）。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<IpBlackListItemDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (id <= 0)
            return new IpBlackListItemDto { StartIP = string.Empty, EndIP = string.Empty, Name = string.Empty, Remark = string.Empty };

        var rows = await _db.Queryable<SiteConfigBlackIP>()
            .Where(t => t.ID == id && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var t = rows.FirstOrDefault();
        if (t == null)
            return new IpBlackListItemDto { StartIP = string.Empty, EndIP = string.Empty, Name = string.Empty, Remark = string.Empty };

        return new IpBlackListItemDto
        {
            ID = t.ID,
            Name = t.Name ?? string.Empty,
            CreateAt = t.CreateAt,
            StartIP = FormatIp(t.StartIP),
            EndIP = FormatIp(t.EndIP),
            Remark = t.Remark ?? string.Empty
        };
    }

    /// <summary>
    /// 保存黑名单段。对齐旧版 SaveBlackIP。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(IpBlackSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入名称#");

        var start = ParseIp(input.StartIP);
        var end = string.IsNullOrWhiteSpace(input.EndIP) ? start : ParseIp(input.EndIP);
        if (start == 0 || end < start)
            throw Oops.Oh("IP设置错误");

        var siteId = _site.RequireSiteId();
        if (input.ID == 0)
        {
            await _db.Insertable(new SiteConfigBlackIP
            {
                SiteID = siteId,
                Name = input.Name.Trim(),
                StartIP = start,
                EndIP = end,
                Remark = input.Remark ?? string.Empty,
                CreateAt = DateTime.Now
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<SiteConfigBlackIP>()
            .Where(t => t.ID == input.ID && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        row.Name = input.Name.Trim();
        row.StartIP = start;
        row.EndIP = end;
        row.Remark = input.Remark ?? string.Empty;
        row.CreateAt = DateTime.Now;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除当前站黑名单。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("ID错误");
        var siteId = _site.RequireSiteId();
        var ok = await _db.Queryable<SiteConfigBlackIP>()
            .AnyAsync(t => t.ID == id && t.SiteID == siteId, cancellationToken);
        if (!ok)
            throw Oops.Oh("ID错误");
        await _db.Deleteable<SiteConfigBlackIP>()
            .Where(t => t.ID == id && t.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// long IP → 点分十进制（对齐旧 IPAgent.GetIP(long)）。
    /// </summary>
    /// <param name="ip">长整型 IP</param>
    /// <returns>点分文本；0 返回空串</returns>
    public static string FormatIp(long ip)
    {
        if (ip == 0) return string.Empty;
        var parts = new string[4];
        for (var index = 0; index < 4; index++)
        {
            var pow = (long)Math.Pow(256, 3 - index);
            var num = ip / pow;
            parts[index] = num.ToString();
            ip -= num * pow;
        }

        return string.Join(".", parts);
    }

    /// <summary>
    /// 点分 IP → long（对齐旧 IPAgent.GetIP(string)；非法返回 0）。
    /// </summary>
    /// <param name="ip">点分文本</param>
    /// <returns>长整型；非法为 0</returns>
    public static long ParseIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return 0;
        var parts = ip.Trim().Split('.');
        if (parts.Length != 4)
            return 0;
        long sum = 0;
        for (var i = 0; i < 4; i++)
        {
            if (!long.TryParse(parts[i], out var n) || n < 0 || n > 255)
                return 0;
            sum += n * (long)Math.Pow(256, 3 - i);
        }

        return sum;
    }

    /// <summary>点分 IP 可解析时返回 long，否则 null。</summary>
    private static long? TryParseIpNumber(string ip)
    {
        var n = ParseIp(ip);
        return n > 0 ? n : null;
    }
}

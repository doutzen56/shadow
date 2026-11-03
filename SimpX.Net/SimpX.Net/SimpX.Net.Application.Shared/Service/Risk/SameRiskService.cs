using SimpX.Net.Data.Entity.Risk;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 同 IP / 同设备查询。对齐旧 /admin/risk/ip/same、/admin/risk/device/same。
/// </summary>
public sealed class SameRiskService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。同 IP、同设备记录在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SameRiskService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 同 IP 会员列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命中会员</returns>
    public async Task<List<SameRiskListItemDto>> SameIpAsync(
        SameRiskQueryInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var user = (input.User ?? string.Empty).Trim();
        var ip = (input.IP ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(ip))
            throw Oops.Oh("#请输入用户名# #或# IP");

        var siteId = _site.RequireSiteId();
        var startAt = input.StartAt ?? DateTime.Now.AddMonths(-1);
        var endAt = input.EndAt ?? DateTime.Now;

        Dictionary<int, int> counts;
        if (!string.IsNullOrEmpty(user) || string.IsNullOrEmpty(ip))
        {
            var userId = await ResolveUserIdAsync(siteId, user, cancellationToken);
            counts = await SameIpByUserAsync(siteId, userId, startAt, endAt, ip, cancellationToken);
        }
        else
        {
            counts = await SameIpByIpAsync(siteId, ip, startAt, endAt, cancellationToken);
        }

        return await ToRowsAsync(siteId, counts, cancellationToken);
    }

    /// <summary>
    /// 同设备会员列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命中会员</returns>
    public async Task<List<SameRiskListItemDto>> SameDeviceAsync(
        SameRiskQueryInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var user = (input.User ?? string.Empty).Trim();
        var device = (input.Device ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(device))
            throw Oops.Oh("#请输入用户名# #或# 设备号");

        var siteId = _site.RequireSiteId();
        var startAt = input.StartAt ?? DateTime.Now.AddMonths(-1);
        var endAt = input.EndAt ?? DateTime.Now;

        Dictionary<int, int> counts;
        if (string.IsNullOrEmpty(device))
        {
            var userId = await ResolveUserIdAsync(siteId, user, cancellationToken);
            counts = await SameDeviceByUserAsync(siteId, userId, startAt, endAt, cancellationToken);
        }
        else
        {
            counts = await SameDeviceByDeviceAsync(siteId, device, startAt, endAt, cancellationToken);
        }

        return await ToRowsAsync(siteId, counts, cancellationToken);
    }

    /// <summary>
    /// 按会员找同 IP。
    /// </summary>
    private async Task<Dictionary<int, int>> SameIpByUserAsync(
        int siteId,
        int userId,
        DateTime startAt,
        DateTime endAt,
        string ip,
        CancellationToken cancellationToken)
    {
        if (userId == 0)
            return new Dictionary<int, int>();

        var myIps = await _db.Queryable<RiskUserIP>()
            .Where(t => t.SiteID == siteId && t.UserID == userId && t.Date >= startAt && t.Date <= endAt)
            .WhereIF(!string.IsNullOrEmpty(ip), t => t.IP == ip)
            .Select(t => t.IP)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (myIps.Count == 0)
            return new Dictionary<int, int>();

        var rows = await _db.Queryable<RiskUserIP>()
            .Where(t => t.SiteID == siteId && t.UserID != userId && t.Date >= startAt && t.Date <= endAt && myIps.Contains(t.IP))
            .GroupBy(t => t.UserID)
            .Select(t => new { UserID = t.UserID, Count = SqlFunc.AggregateCount(t.UserID) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(t => t.UserID, t => t.Count);
    }

    /// <summary>
    /// 按 IP 找会员。
    /// </summary>
    private async Task<Dictionary<int, int>> SameIpByIpAsync(
        int siteId,
        string ip,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<RiskUserIP>()
            .Where(t => t.SiteID == siteId && t.Date >= startAt && t.Date <= endAt && t.IP == ip)
            .GroupBy(t => t.UserID)
            .Select(t => new { UserID = t.UserID, Count = SqlFunc.AggregateCount(t.UserID) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(t => t.UserID, t => t.Count);
    }

    /// <summary>
    /// 按会员找同设备。
    /// </summary>
    private async Task<Dictionary<int, int>> SameDeviceByUserAsync(
        int siteId,
        int userId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken)
    {
        if (userId == 0)
            return new Dictionary<int, int>();

        var myDevices = await _db.Queryable<RiskUserDevice>()
            .Where(t => t.SiteID == siteId && t.UserID == userId && t.Date >= startAt && t.Date <= endAt)
            .Select(t => t.DeviceID)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (myDevices.Count == 0)
            return new Dictionary<int, int>();

        var rows = await _db.Queryable<RiskUserDevice>()
            .Where(t => t.SiteID == siteId && t.UserID != userId && t.Date >= startAt && t.Date <= endAt && myDevices.Contains(t.DeviceID))
            .GroupBy(t => t.UserID)
            .Select(t => new { UserID = t.UserID, Count = SqlFunc.AggregateCount(t.UserID) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(t => t.UserID, t => t.Count);
    }

    /// <summary>
    /// 按设备号找会员。
    /// </summary>
    private async Task<Dictionary<int, int>> SameDeviceByDeviceAsync(
        int siteId,
        string device,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<RiskUserDevice>()
            .Where(t => t.SiteID == siteId && t.Date >= startAt && t.Date <= endAt && t.DeviceID == device)
            .GroupBy(t => t.UserID)
            .Select(t => new { UserID = t.UserID, Count = SqlFunc.AggregateCount(t.UserID) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(t => t.UserID, t => t.Count);
    }

    /// <summary>
    /// 用户名 → 会员编号。
    /// </summary>
    private async Task<int> ResolveUserIdAsync(int siteId, string userName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return 0;
        var rows = await _db.Queryable<User>()
            .Where(t => t.SiteID == siteId && t.UserName == userName)
            .Select(t => t.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }

    /// <summary>
    /// 补用户名并排序。
    /// </summary>
    private async Task<List<SameRiskListItemDto>> ToRowsAsync(
        int siteId,
        Dictionary<int, int> counts,
        CancellationToken cancellationToken)
    {
        if (counts.Count == 0)
            return [];

        var ids = counts.Keys.ToList();
        var names = await _db.Queryable<User>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .Select(t => new { t.ID, t.UserName })
            .ToListAsync(cancellationToken);
        var nameMap = names.ToDictionary(t => t.ID, t => t.UserName ?? string.Empty);

        return counts
            .OrderByDescending(t => t.Value)
            .Select(t => new SameRiskListItemDto
            {
                UserID = t.Key,
                UserName = nameMap.GetValueOrDefault(t.Key, string.Empty),
                Count = t.Value
            })
            .ToList();
    }
}

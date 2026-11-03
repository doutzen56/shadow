using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台登录日志只读。对齐旧版 /admin/report/user/loginlog。
/// </summary>
public sealed class LoginLogService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。登录日志在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public LoginLogService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站登录日志分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<LoginLogAdminPageOutput> PageAsync(
        LoginLogAdminPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        var startAt = DateTime.Now.Date.AddYears(-10);
        if (!string.IsNullOrWhiteSpace(input.StartAt) && DateTime.TryParse(input.StartAt.Trim(), out var sa))
            startAt = sa;
        var endAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(input.EndAt) && DateTime.TryParse(input.EndAt.Trim(), out var ea))
            endAt = ea;

        var query = _db.Queryable<LoginLog>()
            .Where(t => t.SiteID == siteId && t.LoginAt > startAt && t.LoginAt < endAt);

        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var userKey = input.User.Trim();
            if (int.TryParse(userKey, out var uid))
                query = query.Where(t => t.UserID == uid);
            else
            {
                var ids = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(userKey))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
                if (ids.Count == 0)
                    return new LoginLogAdminPageOutput { Data = new List<LoginLogAdminListItemDto>(), Total = 0, Page = page, PageSize = pageSize };
                query = query.Where(t => ids.Contains(t.UserID));
            }
        }
        else if (input.UserID > 0)
        {
            query = query.Where(t => t.UserID == input.UserID);
        }

        if (!string.IsNullOrWhiteSpace(input.IPAddress))
            query = query.Where(t => t.IPAddress != null && t.IPAddress.Contains(input.IPAddress.Trim()));

        if (input.Platform is >= 0)
            query = query.Where(t => t.Platform == input.Platform.Value);

        if (input.IsLast == "1")
            query = query.Where(t => t.IsLast);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(t => t.LoginAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (userIds.Count > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
                nameMap[u.ID] = u.UserName ?? string.Empty;
        }

        var data = rows.Select(t => new LoginLogAdminListItemDto
        {
            ID = t.ID,
            UserID = t.UserID,
            UserName = nameMap.TryGetValue(t.UserID, out var n) ? n : string.Empty,
            LoginAt = t.LoginAt,
            IP = t.IP ?? string.Empty,
            DeviceID = t.DeviceID ?? string.Empty,
            IPAddress = t.IPAddress ?? string.Empty,
            Platform = t.Platform,
            LoginDomain = t.LoginDomain ?? string.Empty
        }).ToList();

        return new LoginLogAdminPageOutput
        {
            Data = data,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}

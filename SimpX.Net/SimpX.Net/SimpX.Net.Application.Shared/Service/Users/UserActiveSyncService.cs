using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 在线会员活动时间与快照：对齐旧 UpdateUserActiveJob → UpdateUserActiveTime / OnlineAgent.SaveUserOnline。
/// </summary>
public interface IUserActiveSyncService
{
    /// <summary>
    /// 把指定用户的 ActiveAt 写成当前时间。
    /// </summary>
    /// <param name="userIds">用户编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实际更新行数</returns>
    Task<int> UpdateActiveAtAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按站点写入 site_Online 快照（人数、端类型、充值人数）；对齐旧 SaveUserOnline。
    /// </summary>
    /// <param name="userIds">本轮累计在线用户（会 Distinct）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>入参去重后的人数</returns>
    Task<int> SaveOnlineSnapshotAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认实现。不挂自动注入接口，由 Schedule 显式注册。
/// </summary>
public sealed class UserActiveSyncService : IUserActiveSyncService
{
    /// <summary>旧 PlatformType.PC。</summary>
    public const int PlatformPc = 1;

    /// <summary>旧 PlatformType.Mobile。</summary>
    public const int PlatformMobile = 2;

    /// <summary>旧 PlatformType.IOS。</summary>
    public const int PlatformIos = 8;

    /// <summary>旧 PlatformType.Android。</summary>
    public const int PlatformAndroid = 16;

    /// <summary>充值资金类型（旧 MoneyType.Recharge）。</summary>
    public const int MoneyTypeRecharge = 1;

    /// <summary>单页最多用户，避免 SQL 参数过多（旧 2000）。</summary>
    public const int PageSize = 2000;

    private readonly ISqlSugarClient _db;
    private readonly ILogger<UserActiveSyncService> _logger;

    /// <summary>
    /// 注入业务库与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="logger">日志</param>
    public UserActiveSyncService(IBizDbAccessor biz, ILogger<UserActiveSyncService> logger)
    {
        _db = biz.Db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> UpdateActiveAtAsync(
        IReadOnlyList<int> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = (userIds ?? Array.Empty<int>()).Where(id => id != 0).Distinct().ToList();
        if (ids.Count == 0)
            return 0;

        var now = DateTime.Now;
        var updated = 0;
        for (var offset = 0; offset < ids.Count; offset += PageSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = ids.Skip(offset).Take(PageSize).ToList();
            updated += await _db.Updateable<User>()
                .SetColumns(u => new User { ActiveAt = now })
                .Where(u => page.Contains(u.ID))
                .ExecuteCommandAsync(cancellationToken);
        }

        return updated;
    }

    /// <inheritdoc />
    public async Task<int> SaveOnlineSnapshotAsync(
        IReadOnlyList<int> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = (userIds ?? Array.Empty<int>()).Where(id => id != 0).Distinct().ToList();
        if (ids.Count == 0)
            return 0;

        for (var offset = 0; offset < ids.Count; offset += PageSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = ids.Skip(offset).Take(PageSize).ToList();
            await SavePageAsync(page, cancellationToken);
        }

        return ids.Count;
    }

    /// <summary>
    /// 一页用户：按站点分组后写 site_Online。
    /// </summary>
    private async Task SavePageAsync(List<int> page, CancellationToken cancellationToken)
    {
        var users = await _db.Queryable<User>()
            .Where(u => page.Contains(u.ID))
            .Select(u => new { u.ID, u.SiteID, u.LoginPlatform })
            .ToListAsync(cancellationToken);
        if (users.Count == 0)
            return;

        var bySite = users.GroupBy(u => u.SiteID).Where(g => g.Key > 0);
        var now = DateTime.Now;
        foreach (var group in bySite)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var siteId = group.Key;
            var siteUserIds = group.Select(u => u.ID).ToList();
            var platforms = group.Select(u => u.LoginPlatform).ToList();

            var rechargeCount = 0;
            try
            {
                var rechargeUserIds = await _db.Queryable<UserDateMoney>()
                    .Where(t => t.SiteID == siteId
                                && t.Type == MoneyTypeRecharge
                                && siteUserIds.Contains(t.UserID))
                    .Select(t => t.UserID)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                rechargeCount = rechargeUserIds.Count;
            }
            catch (Exception ex)
            {
                // 报表表缺列或缺表时仍写在线人数，只是充值人数记 0。
                _logger.LogWarning(ex, "site_Online 充值人数统计失败 SiteID={SiteId}", siteId);
            }

            var row = new SiteOnline
            {
                SiteID = siteId,
                OnlineTime = now,
                Count = siteUserIds.Count,
                PC = platforms.Count(p => (p & PlatformPc) == PlatformPc),
                Mobile = platforms.Count(p => (p & PlatformMobile) == PlatformMobile),
                Android = platforms.Count(p => (p & PlatformAndroid) == PlatformAndroid),
                IOS = platforms.Count(p => (p & PlatformIos) == PlatformIos),
                RechargeCount = rechargeCount
            };
            await _db.Insertable(row).ExecuteCommandAsync(cancellationToken);
        }
    }
}

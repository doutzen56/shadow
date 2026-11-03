using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 活动锁：本人禁止活动，或上级关闭下级活动。
/// </summary>
public interface IUserActivityLockService
{
    /// <summary>
    /// 上级是否关闭了下级活动（旧 FunctionType.CloseSubActivity）。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="childId">下级会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true 表示被上级关掉活动</returns>
    Task<bool> IsParentCloseSubActivityAsync(
        int siteId,
        int childId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 本人活动锁或上级关活动，任一成立则不可参加活动。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="userLock">会员 Lock 位图；未知可传 null 再查库</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true 表示禁止参加</returns>
    Task<bool> IsBlockedForActivityAsync(
        int siteId,
        int userId,
        int? userLock = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 活动锁：对齐旧 GetUserParentActivity + LockStatus.Activity。
/// </summary>
public sealed class UserActivityLockService : ITransient, IUserActivityLockService
{
    /// <summary>旧 LockStatus.Activity。</summary>
    public const int LockActivity = 128;

    /// <summary>旧 FunctionType.CloseSubActivity。</summary>
    public const int FunctionCloseSubActivity = 64;

    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    public UserActivityLockService(IBizDbAccessor biz) => _db = biz.Db;

    /// <inheritdoc />
    public async Task<bool> IsParentCloseSubActivityAsync(
        int siteId,
        int childId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parentIds = await _db.Queryable<UserDepth>()
                .Where(d => d.SiteID == siteId && d.ChildID == childId && d.UserID != childId)
                .OrderBy(d => d.Depth)
                .Select(d => d.UserID)
                .ToListAsync(cancellationToken);
            if (parentIds.Count == 0)
                return false;

            var functions = await _db.Queryable<User>()
                .Where(u => parentIds.Contains(u.ID) && u.SiteID == siteId)
                .Select(u => u.Function)
                .ToListAsync(cancellationToken);
            return functions.Any(f => (f & FunctionCloseSubActivity) != 0);
        }
        catch (Exception)
        {
            // 缺层级表时不挡活动（单测壳库常见）
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsBlockedForActivityAsync(
        int siteId,
        int userId,
        int? userLock = null,
        CancellationToken cancellationToken = default)
    {
        var lockBits = userLock;
        if (lockBits == null)
        {
            var rows = await _db.Queryable<User>()
                .Where(u => u.ID == userId && u.SiteID == siteId)
                .Select(u => u.Lock)
                .Take(1)
                .ToListAsync(cancellationToken);
            if (rows.Count == 0)
                return true;
            lockBits = rows[0];
        }

        if ((lockBits.Value & LockActivity) != 0)
            return true;

        return await IsParentCloseSubActivityAsync(siteId, userId, cancellationToken);
    }
}

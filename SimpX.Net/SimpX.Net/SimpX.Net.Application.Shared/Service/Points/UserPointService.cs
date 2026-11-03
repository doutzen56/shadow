using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员积分：更新 Users.Coin 并写 usr_PointLog。
/// </summary>
public sealed class UserPointService : ITransient, IUserPointService
{
    /// <summary>旧 PointItem.Event。</summary>
    public const int PointTypeEvent = 18;

    /// <summary>旧 User.LockStatus.Point。</summary>
    public const int LockPoint = 64;

    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    public UserPointService(IBizDbAccessor biz)
    {
        _db = biz.Db;
    }

    /// <inheritdoc />
    public async Task AddPointAsync(
        int siteId,
        int userId,
        decimal point,
        int pointType,
        string content,
        int eventType = 0,
        CancellationToken cancellationToken = default,
        DateTime? createAt = null)
    {
        if (siteId <= 0 || userId <= 0 || point == 0)
            return;

        var users = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault()
                   ?? throw new InvalidOperationException("#用户不存在#");

        var affected = await _db.Updateable<User>()
            .SetColumns(u => u.Coin == u.Coin + point)
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        if (affected == 0)
            throw new InvalidOperationException("#更新积分失败#");

        var lockSum = await _db.Queryable<PointLock>()
            .Where(p => p.UserID == userId && p.IsLock)
            .SumAsync(p => p.Money);
        var balance = user.Coin + point + lockSum;

        await _db.Insertable(new UserPointLog
        {
            SiteID = siteId,
            UserID = userId,
            Point = point,
            Balance = balance,
            Type = pointType,
            EventType = eventType,
            Content = content ?? string.Empty,
            AContent = string.Empty,
            BContent = string.Empty,
            CreateAt = createAt ?? DateTime.Now
        }).ExecuteCommandAsync(cancellationToken);
    }
}

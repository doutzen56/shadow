using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 用户经验镜像同步：对齐旧 ExperAgent.UpdateUserExper（Users.Exper ← usr_Money.Exper）。
/// </summary>
public interface IUserExperSyncService
{
    /// <summary>
    /// 把 usr_Money 的 Exper 写回 Users；优先取基础币种钱包。
    /// </summary>
    /// <param name="userId">用户编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>找到钱包并更新为 true</returns>
    Task<bool> SyncAsync(int userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认实现。不挂自动注入接口，由 Schedule 显式注册。
/// </summary>
public sealed class UserExperSyncService : IUserExperSyncService
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<UserExperSyncService> _logger;

    /// <summary>
    /// 注入业务库与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="logger">日志</param>
    public UserExperSyncService(IBizDbAccessor biz, ILogger<UserExperSyncService> logger)
    {
        _db = biz.Db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SyncAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return false;

        try
        {
            // 旧 SQL 未按币种过滤；这里优先基础币种，其次取经验更大的那条。
            var wallets = await _db.Queryable<UserMoney>()
                .Where(m => m.UserID == userId)
                .ToListAsync(cancellationToken);
            var wallet = wallets
                .OrderByDescending(m => m.IsBase)
                .ThenByDescending(m => m.Exper)
                .FirstOrDefault();
            if (wallet == null)
                return false;

            var n = await _db.Updateable<User>()
                .SetColumns(u => new User { Exper = wallet.Exper })
                .Where(u => u.ID == userId)
                .ExecuteCommandAsync(cancellationToken);
            return n > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "同步用户经验失败 userId={UserId}", userId);
            return false;
        }
    }
}

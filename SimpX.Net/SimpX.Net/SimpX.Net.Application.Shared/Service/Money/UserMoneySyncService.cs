using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 用户余额镜像同步：对齐旧 MoneyAgent.UpdateUserMoney → SP data_UpdateUserMoney。
/// </summary>
public interface IUserMoneySyncService
{
    /// <summary>
    /// 按用户+币种跑 data_UpdateUserMoney，并把返回的 Money/LockMoney/Unbalanced/GameMoney 写回 Users。
    /// </summary>
    Task<bool> SyncAsync(int userId, int siteId, int currency, CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认实现：业务库调 SP；失败只记日志，不抛（对齐旧 Job 吞异常、队列已弹出）。
/// </summary>
public sealed class UserMoneySyncService : ITransient, IUserMoneySyncService
{
    private readonly ISqlSugarClient _db;

    public UserMoneySyncService(IBizDbAccessor biz) => _db = biz.Db;

    /// <inheritdoc />
    public async Task<bool> SyncAsync(int userId, int siteId, int currency, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || siteId <= 0)
            return false;

        try
        {
            var dt = await _db.Ado.UseStoredProcedure().GetDataTableAsync(
                "data_UpdateUserMoney",
                new SugarParameter("@UserID", userId),
                new SugarParameter("@SiteID", siteId),
                new SugarParameter("@Currency", currency));
            if (dt == null || dt.Rows.Count == 0)
                return false;

            var row = dt.Rows[0];
            var money = row["Money"] is decimal m ? m : Convert.ToDecimal(row["Money"]);
            var lockMoney = row["LockMoney"] is decimal lm ? lm : Convert.ToDecimal(row["LockMoney"]);
            var unbalanced = row["Unbalanced"] is decimal u ? u : Convert.ToDecimal(row["Unbalanced"]);
            var gameMoney = row["GameMoney"] is decimal g ? g : Convert.ToDecimal(row["GameMoney"]);

            await _db.Updateable<User>()
                .SetColumns(x => new User
                {
                    Money = money,
                    LockMoney = lockMoney,
                    Unbalanced = unbalanced,
                    GameMoney = gameMoney
                })
                .Where(x => x.ID == userId && x.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning($"data_UpdateUserMoney 失败 userId={userId} siteId={siteId} currency={currency}: {ex.Message}");
            return false;
        }
    }
}

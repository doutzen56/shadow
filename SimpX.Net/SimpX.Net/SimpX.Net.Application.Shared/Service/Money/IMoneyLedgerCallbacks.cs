namespace SimpX.Net.Application.Shared;

/// <summary>
/// 账变/解锁落库成功后的后续动作。对齐旧 MoneyAgent 事务回调：刷新钱包缓存；有流水要求时记洗码。
/// </summary>
public interface IMoneyLedgerCallbacks
{
    /// <summary>
    /// 账变成功后：刷新钱包缓存；加款且有流水倍率/下限时调洗码存储过程。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="money">账变金额</param>
    /// <param name="type">资金类型</param>
    /// <param name="currency">币种</param>
    /// <param name="logId">流水号</param>
    /// <param name="description">备注</param>
    /// <param name="flowRate">流水倍率（0 表示不加洗码要求）</param>
    /// <param name="minFlow">最低流水要求</param>
    /// <param name="eventId">活动编号</param>
    /// <param name="reviewId">加扣款审核编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AfterAddMoneyLogAsync(
        int siteId,
        int userId,
        decimal money,
        int type,
        int currency,
        int logId,
        string description,
        decimal flowRate,
        decimal minFlow,
        int eventId,
        int reviewId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 审核锁解锁成功后：按库里最新钱包刷新缓存。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AfterUnlockReviewAsync(
        int siteId,
        int userId,
        int currency,
        CancellationToken cancellationToken = default);
}

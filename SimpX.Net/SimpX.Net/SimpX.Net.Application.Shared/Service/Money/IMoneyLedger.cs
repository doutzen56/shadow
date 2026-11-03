
namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员钱包缓存快照。账变或解锁成功后写入，避免前台读到旧余额。
/// </summary>
public sealed class UserWalletCacheItem
{
    /// <summary>会员编号。</summary>
    public int UserId { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>总余额。</summary>
    public decimal TotalMoney { get; set; }

    /// <summary>锁定金额。</summary>
    public decimal LockMoney { get; set; }
}

/// <summary>
/// 会员资金账变与锁钱入口。对齐旧 MoneyAgent：加款走 usr_AddMoneyLog，锁/解锁改 usr_MoneyLock + usr_Money.LockMoney。
/// </summary>
public interface IMoneyLedger
{
    /// <summary>
    /// 审核类锁钱（LockType=Review）。对齐旧 LockMoney：待审扣款时按审核单号锁定可用余额。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="money">锁定金额（正数）</param>
    /// <param name="sourceId">锁定来源（审核单号）</param>
    /// <param name="description">锁定备注</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task LockReviewAsync(
        int siteId,
        int userId,
        decimal money,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 解锁审核类锁定（LockType=Review）。对齐旧 UnlockMoney。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="sourceId">锁定来源（兑币订单号后 9 位）</param>
    /// <param name="description">解锁备注</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UnlockReviewAsync(
        int siteId,
        int userId,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 解锁提现锁定（LockType=Withdraw=2）。拒绝审核或人工出款前释放锁定。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="sourceId">提现订单号</param>
    /// <param name="description">解锁备注</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UnlockWithdrawAsync(
        int siteId,
        int userId,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 游戏转账锁钱（LockType=GameTransfer=1）。对齐旧转入厂商前 LockMoney。
    /// 默认抛未实现：单测假账本可不覆写；生产用 MoneyLedgerService。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="money">锁定金额（正数）</param>
    /// <param name="sourceId">转账单号</param>
    /// <param name="description">锁定备注</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task LockGameTransferAsync(
        int siteId,
        int userId,
        decimal money,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("未实现游戏转账锁钱");

    /// <summary>
    /// 解锁游戏转账锁定（LockType=GameTransfer=1）。转账失败或完成后释放。
    /// 默认抛未实现：单测假账本可不覆写；生产用 MoneyLedgerService。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="sourceId">转账单号</param>
    /// <param name="description">解锁备注</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UnlockGameTransferAsync(
        int siteId,
        int userId,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("未实现游戏转账解锁");

    /// <summary>
    /// 写一笔账变。对齐旧 AddMoneyLog（存储过程 usr_AddMoneyLog）。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="money">账变金额（出为负）</param>
    /// <param name="type">资金类型（兑出 151 / 兑入 150）</param>
    /// <param name="sourceId">来源单号</param>
    /// <param name="description">备注</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="flowRate">流水倍率；0 表示不加洗码要求（兑币默认 0）</param>
    /// <param name="minFlow">最低流水要求</param>
    /// <param name="eventId">活动编号</param>
    /// <param name="reviewId">加扣款审核编号</param>
    Task AddMoneyLogAsync(
        int siteId,
        int userId,
        decimal money,
        int type,
        string sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default,
        decimal flowRate = 0,
        decimal minFlow = 0,
        int eventId = 0,
        int reviewId = 0);
}

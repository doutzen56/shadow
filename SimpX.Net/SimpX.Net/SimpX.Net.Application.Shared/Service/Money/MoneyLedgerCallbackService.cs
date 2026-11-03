using System.Data;
using SimpX.Net.Caching;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 账变后刷新钱包缓存，推报表/VIP 旁路队列，并在有流水要求时调用洗码存储过程。对齐旧 MoneyAgent.AddCallback。
/// </summary>
public sealed class MoneyLedgerCallbackService : ITransient, IMoneyLedgerCallbacks
{
    /// <summary>旧 LockStatus.Upgrade 位，命中则不入 VIP 队列。</summary>
    private const int LockUpgradeFlag = 256;

    /// <summary>旧 MoneyType.ProxyIn。</summary>
    public const int MoneyTypeProxyIn = 140;

    /// <summary>旧 MoneyType.ProxyOut。</summary>
    public const int MoneyTypeProxyOut = 141;

    private readonly ISqlSugarClient _db;
    private readonly ISimpXCache _cache;
    private readonly ILedgerSideEffectQueues _queues;

    /// <summary>
    /// 注入业务库、缓存与旁路队列。
    /// </summary>
    /// <param name="biz">业务库（与账本同一库，勿用主库 Sqlite）</param>
    /// <param name="cache">业务缓存</param>
    /// <param name="queues">报表/VIP 旁路队列</param>
    public MoneyLedgerCallbackService(IBizDbAccessor biz, ISimpXCache cache, ILedgerSideEffectQueues queues)
    {
        _db = biz.Db;
        _cache = cache;
        _queues = queues;
    }

    /// <inheritdoc />
    public async Task AfterAddMoneyLogAsync(
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
        CancellationToken cancellationToken = default)
    {
        await RefreshWalletCacheAsync(siteId, userId, currency, cancellationToken);

        // 对齐旧 AddCallback：每次入账都推进资金/VIP 与报表账变队列；失败只打日志。
        var skipVip = await IsUpgradeLockedAsync(userId);
        try
        {
            _queues.EnqueueUserMoneyAndVip(userId, currency, skipVip);
            _queues.EnqueueMoneyLog(logId);
            if (type is MoneyTypeProxyIn or MoneyTypeProxyOut)
            {
                var payload =
                    $"{{\"UserId\":{userId},\"SiteId\":{siteId},\"CreateAt\":\"{DateTime.Now:O}\",\"Type\":{type},\"Money\":{money.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"Currency\":{currency}}}";
                _queues.EnqueueProxyMoneySts(payload);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"账变旁路队列入队异常（加款已入账）：{ex.Message}");
        }

        // 兑币等无流水要求的账变不进洗码；人工加款等有倍率/下限时才调。
        if (money <= 0 || (flowRate <= 0 && minFlow <= 0))
            return;

        var flowMoney = flowRate * money > minFlow ? flowRate * money : minFlow;
        var washDesc =
            $"[{type}] #收入# {currency}{money:n}#当前余额#,{description}";

        // 对齐旧 WashFlowAgent.AddWashFlowLog → usr_AddWashFlowLog。
        // 真库签名：第 5 个是 @WashFlow OUTPUT，@ReviewID 在末位且是输入；
        // 按位置 EXEC 且把 OUTPUT 写在最后，会把 OUTPUT 落到 @ReviewID 上报错。
        // 洗码失败不能把已入账的加款打成失败。
        try
        {
            var washOut = new SugarParameter("@WashFlow", DBNull.Value)
            {
                Direction = ParameterDirection.Output,
                DbType = System.Data.DbType.Decimal
            };
            await _db.Ado.UseStoredProcedure().ExecuteCommandAsync(
                "usr_AddWashFlowLog",
                new SugarParameter("@SiteID", siteId),
                new SugarParameter("@UserID", userId),
                new SugarParameter("@BetMoney", Math.Abs(flowMoney) * -1m),
                new SugarParameter("@Description", washDesc),
                washOut,
                new SugarParameter("@Money", money),
                new SugarParameter("@Type", type),
                new SugarParameter("@Currency", currency),
                new SugarParameter("@EventID", eventId),
                new SugarParameter("@ReviewID", reviewId));
            var washVal = washOut.Value == null || washOut.Value == DBNull.Value
                ? (decimal?)null
                : Convert.ToDecimal(washOut.Value);
            if (washVal.HasValue)
                _cache.Add(MoneyCacheKeys.UserWashFlow, userId, washVal, UntilTomorrow());
            else
                Log.Warning("usr_AddWashFlowLog 未返回洗码余额（钱包可能不存在），加款已入账");
        }
        catch (Exception ex)
        {
            Log.Warning($"usr_AddWashFlowLog 失败（加款已入账，洗码跳过）：{ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task AfterUnlockReviewAsync(
        int siteId,
        int userId,
        int currency,
        CancellationToken cancellationToken = default) =>
        RefreshWalletCacheAsync(siteId, userId, currency, cancellationToken);

    /// <summary>
    /// 读会员锁；带 Upgrade 位则跳过 VIP 队列。查失败当未锁定，照常入 VIP。
    /// </summary>
    /// <param name="userId">用户编号</param>
    /// <returns>是否跳过 VIP</returns>
    private async Task<bool> IsUpgradeLockedAsync(int userId)
    {
        try
        {
            // 走本业务库 Queryable，兼容 SQL Server / SQLite（单测内存库）。
            var lockVal = (await _db.Queryable<User>()
                .Where(u => u.ID == userId)
                .Select(u => u.Lock)
                .Take(1)
                .ToListAsync()).FirstOrDefault();
            return (lockVal & LockUpgradeFlag) != 0;
        }
        catch (Exception ex)
        {
            Log.Warning($"读会员锁失败，VIP 队列照常入队：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从库读最新钱包写入缓存；没有钱包则删掉旧缓存。
    /// </summary>
    private async Task RefreshWalletCacheAsync(
        int siteId,
        int userId,
        int currency,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var id = WalletCacheId(userId, currency);
        // 审核入账走同一条连接上的存储过程。Queryable 在多库时会读到主库，缓存就会还是旧余额。
        var wallet = await ReadWalletAsync(siteId, userId, currency);
        if (wallet == null)
        {
            _cache.Remove(MoneyCacheKeys.UserWallet, id);
            return;
        }

        _cache.Add(
            MoneyCacheKeys.UserWallet,
            id,
            new UserWalletCacheItem
            {
                UserId = userId,
                Currency = currency,
                TotalMoney = wallet.TotalMoney,
                LockMoney = wallet.LockMoney
            },
            UntilTomorrow());
    }

    /// <summary>
    /// 从当前业务库读钱包。SQL Server 用 TOP，内存库用 LIMIT，避免审核后缓存读错库。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="currency">币种</param>
    /// <returns>钱包；没有则 null</returns>
    private async Task<UserMoney> ReadWalletAsync(int siteId, int userId, int currency)
    {
        var sqlite = _db.CurrentConnectionConfig?.DbType == SqlSugar.DbType.Sqlite;
        var sql = sqlite
            ? "SELECT SiteID, UserID, TotalMoney, LockMoney, Currency FROM usr_Money WHERE SiteID=@SiteID AND UserID=@UserID AND Currency=@Currency LIMIT 1"
            : "SELECT TOP 1 SiteID, UserID, TotalMoney, LockMoney, Currency FROM usr_Money WHERE SiteID=@SiteID AND UserID=@UserID AND Currency=@Currency";
        var rows = await _db.Ado.SqlQueryAsync<UserMoney>(
            sql,
            new List<SugarParameter>
            {
                new("@SiteID", siteId),
                new("@UserID", userId),
                new("@Currency", currency)
            });
        return rows.FirstOrDefault();
    }

    /// <summary>
    /// 缓存标识：用户:币种。
    /// </summary>
    private static string WalletCacheId(int userId, int currency) => $"{userId}:{currency}";

    /// <summary>
    /// 对齐旧钱包缓存：过期到次日。
    /// </summary>
    private static TimeSpan UntilTomorrow()
    {
        var now = DateTime.Now;
        var tomorrow = now.Date.AddDays(1);
        var span = tomorrow - now;
        return span < TimeSpan.FromMinutes(1) ? TimeSpan.FromHours(1) : span;
    }
}

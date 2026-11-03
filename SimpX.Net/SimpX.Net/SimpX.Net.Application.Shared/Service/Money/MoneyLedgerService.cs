using Microsoft.AspNetCore.Http;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 资金账变实现。加款走 SP usr_AddMoneyLog；解锁改锁表与 usr_Money.LockMoney。不旁路改 TotalMoney。
/// 成功后走 <see cref="IMoneyLedgerCallbacks"/> 刷新钱包缓存，有流水要求时记洗码。
/// </summary>
public sealed class MoneyLedgerService : ITransient, IMoneyLedger
{
    /// <summary>审核锁类型（对齐旧 MoneyLock.LockType.Review）。</summary>
    public const int LockTypeReview = 4;

    /// <summary>提现锁类型（对齐旧 MoneyLock.LockType.Withdraw）。</summary>
    public const int LockTypeWithdraw = 2;

    /// <summary>游戏转账锁类型（对齐旧 MoneyLock.LockType.GameTransfer）。</summary>
    public const int LockTypeGameTransfer = 1;

    /// <summary>兑入账变类型。</summary>
    public const int MoneyTypeExchangeIn = 150;

    /// <summary>兑出账变类型。</summary>
    public const int MoneyTypeExchangeOut = 151;

    /// <summary>
    /// 游戏转出账变类型（旧 MoneyType.GameTransferOut=5，#游戏转出#）。
    /// 厂商→中心：中心钱包加款，金额为正。
    /// </summary>
    public const int MoneyTypeGameTransferOut = 5;

    /// <summary>
    /// 转入游戏账变类型（旧 MoneyType.GameTransferIn=8，#转入游戏#）。
    /// 中心→厂商：中心钱包扣款，金额为负。
    /// </summary>
    public const int MoneyTypeGameTransferIn = 8;

    private readonly ISqlSugarClient _db;
    private readonly IHttpContextAccessor _http;
    private readonly IMoneyLedgerCallbacks _callbacks;

    /// <summary>
    /// 注入业务库、请求上下文与账变后续动作。
    /// </summary>
    /// <param name="biz">业务库（可挂 BetWin30Biz）</param>
    /// <param name="http">HTTP（取 IP；可空）</param>
    /// <param name="callbacks">缓存/洗码回调</param>
    public MoneyLedgerService(IBizDbAccessor biz, IHttpContextAccessor http, IMoneyLedgerCallbacks callbacks)
    {
        _db = biz.Db;
        _http = http;
        _callbacks = callbacks;
    }

    /// <inheritdoc />
    public async Task LockReviewAsync(
        int siteId,
        int userId,
        decimal money,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default)
    {
        if (money <= 0)
            throw Oops.Oh("#金额错误#");

        var exists = await _db.Queryable<MoneyLock>()
            .Where(l => l.UserID == userId && l.Type == LockTypeReview && l.SourceID == sourceId)
            .AnyAsync(cancellationToken);
        if (exists)
            throw Oops.Oh("#锁定记录已经存在#");

        var moneyLock = new MoneyLock
        {
            SiteID = siteId,
            UserID = userId,
            Type = LockTypeReview,
            SourceID = sourceId,
            Money = money,
            IsLock = true,
            CreateAt = DateTime.Now,
            UnLockAt = DateTime.MinValue,
            LockDesc = description ?? string.Empty,
            UnLockDesc = string.Empty,
            Currency = currency
        };
        await _db.Insertable(moneyLock).ExecuteCommandAsync(cancellationToken);

        // 可用余额不足则锁失败（对齐旧 TotalMoney - LockMoney - money >= 0）
        var affected = await _db.Ado.ExecuteCommandAsync(
            "UPDATE usr_Money SET LockMoney = LockMoney + @Money WHERE SiteID = @SiteID AND UserID = @UserID AND Currency=@Currency AND (TotalMoney - LockMoney - @Money) >= 0",
            new List<SugarParameter>
            {
                new("@SiteID", siteId),
                new("@UserID", userId),
                new("@Money", money),
                new("@Currency", currency)
            });
        if (affected == 0)
            throw Oops.Oh("#可用余额不足#");

        // 只刷新钱包缓存（与解锁后同一套读库写缓存）
        await _callbacks.AfterUnlockReviewAsync(siteId, userId, currency, cancellationToken);
    }

    /// <inheritdoc />
    public Task UnlockReviewAsync(
        int siteId,
        int userId,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default) =>
        UnlockLockAsync(siteId, userId, sourceId, description, currency, LockTypeReview, cancellationToken);

    /// <inheritdoc />
    public Task UnlockWithdrawAsync(
        int siteId,
        int userId,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default) =>
        UnlockLockAsync(siteId, userId, sourceId, description, currency, LockTypeWithdraw, cancellationToken);

    /// <inheritdoc />
    public async Task LockGameTransferAsync(
        int siteId,
        int userId,
        decimal money,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default)
    {
        if (money <= 0)
            throw Oops.Oh("#金额错误#");

        var exists = await _db.Queryable<MoneyLock>()
            .Where(l => l.UserID == userId && l.Type == LockTypeGameTransfer && l.SourceID == sourceId)
            .AnyAsync(cancellationToken);
        if (exists)
            throw Oops.Oh("#锁定记录已经存在#");

        var moneyLock = new MoneyLock
        {
            SiteID = siteId,
            UserID = userId,
            Type = LockTypeGameTransfer,
            SourceID = sourceId,
            Money = money,
            IsLock = true,
            CreateAt = DateTime.Now,
            UnLockAt = DateTime.MinValue,
            LockDesc = description ?? string.Empty,
            UnLockDesc = string.Empty,
            Currency = currency
        };
        await _db.Insertable(moneyLock).ExecuteCommandAsync(cancellationToken);

        var affected = await _db.Ado.ExecuteCommandAsync(
            "UPDATE usr_Money SET LockMoney = LockMoney + @Money WHERE SiteID = @SiteID AND UserID = @UserID AND Currency=@Currency AND (TotalMoney - LockMoney - @Money) >= 0",
            new List<SugarParameter>
            {
                new("@SiteID", siteId),
                new("@UserID", userId),
                new("@Money", money),
                new("@Currency", currency)
            });
        if (affected == 0)
            throw Oops.Oh("#可用余额不足#");

        await _callbacks.AfterUnlockReviewAsync(siteId, userId, currency, cancellationToken);
    }

    /// <inheritdoc />
    public Task UnlockGameTransferAsync(
        int siteId,
        int userId,
        int sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default) =>
        UnlockLockAsync(siteId, userId, sourceId, description, currency, LockTypeGameTransfer, cancellationToken);

    /// <summary>
    /// 按锁定类型解锁一笔，并扣减钱包 LockMoney。
    /// </summary>
    private async Task UnlockLockAsync(
        int siteId,
        int userId,
        int sourceId,
        string description,
        int currency,
        int lockType,
        CancellationToken cancellationToken)
    {
        var locks = await _db.Queryable<MoneyLock>()
            .Where(l => l.UserID == userId && l.Type == lockType && l.SourceID == sourceId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var moneyLock = locks.FirstOrDefault();
        if (moneyLock == null || moneyLock.SiteID != siteId)
            throw Oops.Oh("#锁定记录不存在#");
        if (!moneyLock.IsLock)
            throw Oops.Oh("#该笔资金已被解锁#");

        moneyLock.IsLock = false;
        moneyLock.UnLockAt = DateTime.Now;
        moneyLock.UnLockDesc = description ?? string.Empty;
        moneyLock.Currency = currency;

        var updated = await _db.Updateable(moneyLock)
            .UpdateColumns(l => new { l.IsLock, l.UnLockAt, l.UnLockDesc, l.Currency })
            .ExecuteCommandAsync(cancellationToken);
        if (updated == 0)
            throw Oops.Oh("#锁定记录不存在#");

        var affected = await _db.Ado.ExecuteCommandAsync(
            "UPDATE usr_Money SET LockMoney = LockMoney + @Money WHERE SiteID = @SiteID AND UserID = @UserID AND LockMoney + @Money >= 0 AND Currency=@Currency",
            new List<SugarParameter>
            {
                new("@SiteID", siteId),
                new("@UserID", userId),
                new("@Money", moneyLock.Money * -1m),
                new("@Currency", currency)
            });
        if (affected == 0)
            throw Oops.Oh("#锁定资金不足#");

        await _callbacks.AfterUnlockReviewAsync(siteId, userId, currency, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddMoneyLogAsync(
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
        int reviewId = 0)
    {
        // 用 Ado 直查，避开 SqlSugar Queryable 在多库场景下偶发落到主库 Sqlite 的问题
        Log.Information(
            $"MoneyLedger.AddMoneyLog 当前库 ConfigId={_db.CurrentConnectionConfig?.ConfigId} DbType={_db.CurrentConnectionConfig?.DbType} Ado={_db.Ado.Connection?.GetType().Name}");
        var wallet = (await _db.Ado.SqlQueryAsync<UserMoney>(
            "SELECT TOP 1 SiteID, UserID, TotalMoney, LockMoney, Coin, Credit, Exper, WashFlow, Unbalanced, Currency, IsBase FROM usr_Money WHERE SiteID=@SiteID AND UserID=@UserID AND Currency=@Currency",
            new List<SugarParameter>
            {
                new("@SiteID", siteId),
                new("@UserID", userId),
                new("@Currency", currency)
            })).FirstOrDefault()
            ?? throw Oops.Oh("#钱包不存在#");
        var available = wallet.TotalMoney - wallet.LockMoney;
        if (available + money < 0)
            throw Oops.Oh("#可用余额不足#");

        var tableId = userId % 10;
        var src = sourceId ?? string.Empty;
        if (src.Length > 20)
            src = src[..20];

        var exists = await _db.Queryable<MoneyLog>()
            .Where(l => l.TableID == tableId && l.UserID == userId && l.Type == type && l.SourceID == src && l.SubType == 0)
            .AnyAsync(cancellationToken);
        if (exists)
            throw Oops.Oh("#已存在资金记录#");

        var logId = await NextLogIdAsync(cancellationToken);
        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        // 对齐旧版 ExecuteNonQuery(SP)：余额只允许 SP 改。
        var rows = await _db.Ado.ExecuteCommandAsync(
            "EXEC usr_AddMoneyLog @LogID,@SiteID,@UserID,@Money,@Type,@SubType,@SourceID,@IP,@Description,@Currency",
            new List<SugarParameter>
            {
                new("@LogID", logId),
                new("@SiteID", siteId),
                new("@UserID", userId),
                new("@Money", money),
                new("@Type", type),
                new("@SubType", (byte)0),
                new("@SourceID", src),
                new("@IP", ip),
                new("@Description", description ?? string.Empty),
                new("@Currency", currency)
            });
        if (rows == 0)
            throw Oops.Oh("#余额不足#");

        await _callbacks.AfterAddMoneyLogAsync(
            siteId,
            userId,
            money,
            type,
            currency,
            logId,
            description ?? string.Empty,
            flowRate,
            minFlow,
            eventId,
            reviewId,
            cancellationToken);
    }

    /// <summary>
    /// 取下一个资金流水号（对齐旧 site_MoneyLog MAX+1）。
    /// </summary>
    private async Task<int> NextLogIdAsync(CancellationToken cancellationToken)
    {
        try
        {
            var max = await _db.Ado.GetIntAsync("SELECT ISNULL(MAX(LogID),0) FROM site_MoneyLog");
            return max + 1;
        }
        catch
        {
            return Math.Abs((int)(DateTime.UtcNow.Ticks % 1_000_000_000)) + 1;
        }
    }
}

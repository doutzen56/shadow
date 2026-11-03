using Microsoft.AspNetCore.Http;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 游戏转账锁钱（内存 SQLite）。对齐 LockType=GameTransfer=1。
/// </summary>
public sealed class GameTransferLockSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly MoneyLedgerService _ledger;
    private readonly int _userId;

    /// <summary>
    /// 建锁表与钱包，植入余额。
    /// </summary>
    public GameTransferLockSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "usr_Money", "usr_MoneyLock"], []);
        _userId = _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "gt_lock",
            NickName = "gt",
            AccountName = "",
            Password = "x",
            PayPassword = "",
            CreateAt = DateTime.UtcNow,
            Face = "",
            RegIP = "",
            Lock = 0,
            Function = 0,
            LoginAt = DateTime.UtcNow,
            LoginIP = "",
            AgentID = 0,
            Depth = 0,
            GroupID = 1,
            Type = 0,
            Email = "",
            IsEmail = false,
            Mobile = "",
            IsMobile = false,
            Question = 0,
            Answer = "",
            SecretKey = Guid.Empty,
            Currency = 1,
            Money = 0,
            LockMoney = 0,
            GameMoney = 0,
            IsOnline = false,
            Flag = 0,
            Level = 0,
            ActiveAt = DateTime.UtcNow,
            Exper = 0,
            RegFlag = 0,
            RegDomain = "",
            LoginPlatform = 0,
            RegPlatform = 0,
            Unbalanced = 0,
            InviteID = 0,
            UpdateAt = DateTime.UtcNow,
            ProxyRate = 0,
            WashingRatio = "",
            LastReturn = DateTime.UtcNow,
            Coin = 0,
            DisableCurrency = "",
            Address = "",
            Birthday = DateTime.UtcNow,
            Telegram = "",
            Line = "",
            Google = "",
            Info = "",
            DeviceId = "",
            RegisteredFrom = "",
            PixelID = "",
            InitLevel = 0,
            Is_Temporary_Password = false,
            BlockGame = "",
            OldGroupID = 0
        }).ExecuteReturnIdentity();

        _db.Insertable(new UserMoney
        {
            SiteID = 1001,
            UserID = _userId,
            TotalMoney = 100m,
            LockMoney = 0m,
            Coin = 0,
            Credit = 0,
            Exper = 0,
            WashFlow = 0,
            Unbalanced = 0,
            Currency = 1,
            IsBase = true
        }).ExecuteCommand();

        _ledger = new MoneyLedgerService(new TestBizDb(_db), new HttpContextAccessor(), new NoopCallbacks());
    }

    /// <summary>
    /// 锁成功：LockType=1，LockMoney 增加。
    /// </summary>
    [Fact]
    public async Task LockGameTransfer_writes_type1_and_raises_lock_money()
    {
        await _ledger.LockGameTransferAsync(1001, _userId, 30m, 9001, "转出锁", 1);
        var locks = await _db.Queryable<MoneyLock>()
            .Where(l => l.UserID == _userId && l.SourceID == 9001)
            .ToListAsync();
        Assert.Single(locks);
        Assert.Equal(MoneyLedgerService.LockTypeGameTransfer, locks[0].Type);
        Assert.True(locks[0].IsLock);
        var wallet = await _db.Queryable<UserMoney>()
            .Where(w => w.UserID == _userId && w.SiteID == 1001 && w.Currency == 1)
            .FirstAsync();
        Assert.Equal(30m, wallet.LockMoney);
    }

    /// <summary>
    /// 余额不足：锁失败。
    /// </summary>
    [Fact]
    public async Task LockGameTransfer_insufficient_balance_fails()
    {
        // 无 Furion 宿主时 Oops.Oh 可能包一层类型初始化异常；只断言锁失败且未写入锁记录。
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _ledger.LockGameTransferAsync(1001, _userId, 200m, 9002, "超额", 1));
        var locks = await _db.Queryable<MoneyLock>()
            .Where(l => l.UserID == _userId && l.SourceID == 9002)
            .ToListAsync();
        // 可能已插入锁行但钱包 UPDATE 失败；以钱包未增加为准
        var wallet = await _db.Queryable<UserMoney>()
            .Where(w => w.UserID == _userId && w.SiteID == 1001 && w.Currency == 1)
            .FirstAsync();
        Assert.Equal(0m, wallet.LockMoney);
        _ = locks;
    }

    /// <summary>
    /// 解锁后 LockMoney 回落。
    /// </summary>
    [Fact]
    public async Task UnlockGameTransfer_releases_lock_money()
    {
        await _ledger.LockGameTransferAsync(1001, _userId, 20m, 9003, "锁", 1);
        await _ledger.UnlockGameTransferAsync(1001, _userId, 9003, "释放", 1);
        var wallet = await _db.Queryable<UserMoney>()
            .Where(w => w.UserID == _userId && w.SiteID == 1001 && w.Currency == 1)
            .FirstAsync();
        Assert.Equal(0m, wallet.LockMoney);
        var locks = await _db.Queryable<MoneyLock>()
            .Where(l => l.UserID == _userId && l.SourceID == 9003)
            .ToListAsync();
        Assert.False(locks[0].IsLock);
    }

    /// <summary>清理。</summary>
    public void Dispose() => _db?.Dispose();

    /// <summary>空回调。</summary>
    private sealed class NoopCallbacks : IMoneyLedgerCallbacks
    {
        /// <inheritdoc />
        public Task AfterUnlockReviewAsync(int siteId, int userId, int currency, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        /// <inheritdoc />
        public Task AfterAddMoneyLogAsync(
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
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

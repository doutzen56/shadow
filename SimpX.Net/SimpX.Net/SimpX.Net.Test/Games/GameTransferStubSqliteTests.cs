using Microsoft.AspNetCore.Http;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 游戏转账 Stub + 真锁钱（内存 SQLite）。
/// IN 成功：锁后扣款（type=8 负数）；失败：解锁且不扣款。
/// </summary>
public sealed class GameTransferStubSqliteTests : IDisposable
{
    private readonly SqlSugarClient _db;
    private readonly HybridMoneyLedger _money;
    private readonly int _userId;
    private const int SiteId = 1001;
    private const int Currency = 1;
    private const int GameType = 101;

    /// <summary>
    /// 建 Users / usr_Money / usr_MoneyLock / game_TransferOrder，植入余额。
    /// </summary>
    public GameTransferStubSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(
            ["Users", "usr_Money", "usr_MoneyLock", "game_TransferOrder"],
            []);
        _userId = _db.Insertable(new User
        {
            SiteID = SiteId,
            UserName = "gt_xfer",
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
            Currency = Currency,
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
            SiteID = SiteId,
            UserID = _userId,
            TotalMoney = 100m,
            LockMoney = 0m,
            Coin = 0,
            Credit = 0,
            Exper = 0,
            WashFlow = 0,
            Unbalanced = 0,
            Currency = Currency,
            IsBase = true
        }).ExecuteCommand();

        var real = new MoneyLedgerService(new TestBizDb(_db), new HttpContextAccessor(), new NoopCallbacks());
        _money = new HybridMoneyLedger(_db, real);
    }

    /// <summary>
    /// IN 成功：锁后扣款（type=8 负），LockMoney 归零，订单 Success。
    /// </summary>
    [Fact]
    public async Task TransferToGame_success_locks_then_debits()
    {
        var stub = new StubGameVendorTransfer { AlwaysSuccess = true };
        var svc = new GameTransferService(new TestBizDb(_db), _money, stub, Array.Empty<IGameVendorTransfer>());

        var result = await svc.TransferToGameAsync(SiteId, _userId, GameType, 30m, Currency);
        Assert.Equal(1, result.Success);

        var wallet = await _db.Queryable<UserMoney>()
            .Where(w => w.UserID == _userId && w.SiteID == SiteId && w.Currency == Currency)
            .FirstAsync();
        Assert.Equal(0m, wallet.LockMoney);
        Assert.Equal(70m, wallet.TotalMoney);

        Assert.Single(_money.Logs);
        Assert.Equal(MoneyLedgerService.MoneyTypeGameTransferIn, _money.Logs[0].Type);
        Assert.Equal(-30m, _money.Logs[0].Money);

        var order = await _db.Queryable<GameTransferOrder>()
            .Where(o => o.UserID == _userId && o.Action == GameTransferService.ActionIn)
            .FirstAsync();
        Assert.Equal(GameTransferService.StatusSuccess, order.Status);
        Assert.Equal(GameTransferService.ActionIn, order.Action);
    }

    /// <summary>
    /// IN 失败：解锁且不扣款，订单 Fail。
    /// </summary>
    [Fact]
    public async Task TransferToGame_fail_unlocks_without_debit()
    {
        var stub = new StubGameVendorTransfer { AlwaysSuccess = false, FailMessage = "#stub fail#" };
        var svc = new GameTransferService(new TestBizDb(_db), _money, stub, Array.Empty<IGameVendorTransfer>());

        var result = await svc.TransferToGameAsync(SiteId, _userId, GameType, 20m, Currency);
        Assert.Equal(0, result.Success);

        var wallet = await _db.Queryable<UserMoney>()
            .Where(w => w.UserID == _userId && w.SiteID == SiteId && w.Currency == Currency)
            .FirstAsync();
        Assert.Equal(0m, wallet.LockMoney);
        Assert.Equal(100m, wallet.TotalMoney);
        Assert.Empty(_money.Logs);

        var locks = await _db.Queryable<MoneyLock>()
            .Where(l => l.UserID == _userId && l.Type == MoneyLedgerService.LockTypeGameTransfer)
            .ToListAsync();
        Assert.Single(locks);
        Assert.False(locks[0].IsLock);

        var order = await _db.Queryable<GameTransferOrder>()
            .Where(o => o.UserID == _userId)
            .FirstAsync();
        Assert.Equal(GameTransferService.StatusFail, order.Status);
    }

    /// <summary>
    /// OUT 成功：中心钱包加款 type=5 正数，订单 Action=OUT Success。
    /// </summary>
    [Fact]
    public async Task TransferToCenter_success_credits()
    {
        var stub = new StubGameVendorTransfer { AlwaysSuccess = true };
        var svc = new GameTransferService(new TestBizDb(_db), _money, stub, Array.Empty<IGameVendorTransfer>());

        var result = await svc.TransferToCenterAsync(SiteId, _userId, GameType, 15m, Currency);
        Assert.Equal(1, result.Success);

        var wallet = await _db.Queryable<UserMoney>()
            .Where(w => w.UserID == _userId && w.SiteID == SiteId && w.Currency == Currency)
            .FirstAsync();
        Assert.Equal(115m, wallet.TotalMoney);

        Assert.Single(_money.Logs);
        Assert.Equal(MoneyLedgerService.MoneyTypeGameTransferOut, _money.Logs[0].Type);
        Assert.Equal(15m, _money.Logs[0].Money);

        var order = await _db.Queryable<GameTransferOrder>()
            .Where(o => o.UserID == _userId && o.Action == GameTransferService.ActionOut)
            .FirstAsync();
        Assert.Equal(GameTransferService.StatusSuccess, order.Status);
    }

    /// <summary>清理。</summary>
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 真锁钱 + 模拟扣款（SQLite 无 usr_AddMoneyLog）。
    /// </summary>
    private sealed class HybridMoneyLedger : IMoneyLedger
    {
        private readonly ISqlSugarClient _db;
        private readonly MoneyLedgerService _real;

        /// <summary>已写入的账变。</summary>
        public List<(decimal Money, int Type, string SourceId)> Logs { get; } = [];

        /// <summary>包装真账本。</summary>
        public HybridMoneyLedger(ISqlSugarClient db, MoneyLedgerService real)
        {
            _db = db;
            _real = real;
        }

        /// <inheritdoc />
        public Task LockReviewAsync(int siteId, int userId, decimal money, int sourceId, string description, int currency, CancellationToken cancellationToken = default) =>
            _real.LockReviewAsync(siteId, userId, money, sourceId, description, currency, cancellationToken);

        /// <inheritdoc />
        public Task UnlockReviewAsync(int siteId, int userId, int sourceId, string description, int currency, CancellationToken cancellationToken = default) =>
            _real.UnlockReviewAsync(siteId, userId, sourceId, description, currency, cancellationToken);

        /// <inheritdoc />
        public Task UnlockWithdrawAsync(int siteId, int userId, int sourceId, string description, int currency, CancellationToken cancellationToken = default) =>
            _real.UnlockWithdrawAsync(siteId, userId, sourceId, description, currency, cancellationToken);

        /// <inheritdoc />
        public Task LockGameTransferAsync(int siteId, int userId, decimal money, int sourceId, string description, int currency, CancellationToken cancellationToken = default) =>
            _real.LockGameTransferAsync(siteId, userId, money, sourceId, description, currency, cancellationToken);

        /// <inheritdoc />
        public Task UnlockGameTransferAsync(int siteId, int userId, int sourceId, string description, int currency, CancellationToken cancellationToken = default) =>
            _real.UnlockGameTransferAsync(siteId, userId, sourceId, description, currency, cancellationToken);

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
            var affected = await _db.Ado.ExecuteCommandAsync(
                "UPDATE usr_Money SET TotalMoney = TotalMoney + @Money WHERE SiteID=@SiteID AND UserID=@UserID AND Currency=@Currency AND (TotalMoney - LockMoney + @Money) >= 0",
                new List<SugarParameter>
                {
                    new("@SiteID", siteId),
                    new("@UserID", userId),
                    new("@Money", money),
                    new("@Currency", currency)
                });
            if (affected == 0)
                throw new InvalidOperationException("#可用余额不足#");
            Logs.Add((money, type, sourceId ?? string.Empty));
        }
    }

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

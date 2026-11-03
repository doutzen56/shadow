using SimpX.Net.Application.Shared;
using SimpX.Net.Caching;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 账变回调：刷新钱包缓存、旁路队列；无流水要求时不调洗码存储过程。禁写 BetWin30。
/// </summary>
public sealed class MoneyLedgerCallbackSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly MemorySimpXCache _cache = new();
    private readonly MemoryLedgerSideEffectQueues _queues = new();
    private readonly int _userId;
    private readonly int _upgradeUserId;

    /// <summary>
    /// 建钱包表、用户表并植入数据。
    /// </summary>
    public MoneyLedgerCallbackSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["usr_Money", "Users"], []);
        _userId = InsertUser("u88", lockFlags: 0);
        _upgradeUserId = InsertUser("u89", lockFlags: 256);

        _db.Insertable(new UserMoney
        {
            SiteID = 1001,
            UserID = _userId,
            Currency = 2,
            TotalMoney = 500,
            LockMoney = 50,
            WashFlow = 0
        }).ExecuteCommand();
        _db.Insertable(new UserMoney
        {
            SiteID = 1001,
            UserID = _upgradeUserId,
            Currency = 2,
            TotalMoney = 100,
            LockMoney = 0,
            WashFlow = 0
        }).ExecuteCommand();
    }

    /// <summary>
    /// 兑币类账变（无流水倍率）只刷新钱包缓存，并推进报表与资金/VIP 队列。
    /// </summary>
    [Fact]
    public async Task AfterAddMoneyLog_without_flow_refreshes_wallet_cache_and_enqueues()
    {
        var svc = new MoneyLedgerCallbackService(new TestBizDb(_db), _cache, _queues);
        await svc.AfterAddMoneyLogAsync(
            1001,
            _userId,
            100,
            MoneyLedgerService.MoneyTypeExchangeIn,
            2,
            logId: 41,
            description: "test",
            flowRate: 0,
            minFlow: 0,
            eventId: 0,
            reviewId: 0);

        var cached = _cache.Get<UserWalletCacheItem>(MoneyCacheKeys.UserWallet, $"{_userId}:2");
        Assert.NotNull(cached);
        Assert.Equal(500, cached!.TotalMoney);
        Assert.Equal(50, cached.LockMoney);

        Assert.True(_queues.MoneyLogs.TryPeek(out var logId));
        Assert.Equal(41, logId);
        Assert.True(_queues.UserMoneyScores.ContainsKey($"{_userId}_2"));
        Assert.True(_queues.UserVipScores.ContainsKey(_userId.ToString()));
    }

    /// <summary>
    /// 带 Upgrade 锁的会员：资金队列仍入，VIP 队列跳过。
    /// </summary>
    [Fact]
    public async Task AfterAddMoneyLog_upgrade_lock_skips_vip_queue()
    {
        var svc = new MoneyLedgerCallbackService(new TestBizDb(_db), _cache, _queues);
        await svc.AfterAddMoneyLogAsync(
            1001,
            _upgradeUserId,
            10,
            MoneyLedgerService.MoneyTypeExchangeIn,
            2,
            logId: 42,
            description: "upgrade",
            flowRate: 0,
            minFlow: 0,
            eventId: 0,
            reviewId: 0);

        Assert.True(_queues.UserMoneyScores.ContainsKey($"{_upgradeUserId}_2"));
        Assert.False(_queues.UserVipScores.ContainsKey(_upgradeUserId.ToString()));
        Assert.Contains(42, _queues.MoneyLogs);
    }

    /// <summary>
    /// 解锁后同样刷新缓存，不推旁路队列。
    /// </summary>
    [Fact]
    public async Task AfterUnlock_refreshes_wallet_cache()
    {
        var svc = new MoneyLedgerCallbackService(new TestBizDb(_db), _cache, _queues);
        await svc.AfterUnlockReviewAsync(1001, _userId, 2);

        var cached = _cache.Get<UserWalletCacheItem>(MoneyCacheKeys.UserWallet, $"{_userId}:2");
        Assert.NotNull(cached);
        Assert.Equal(500, cached!.TotalMoney);
        Assert.True(_queues.MoneyLogs.IsEmpty);
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    /// <summary>
    /// 插一条测试会员，返回自增编号。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="lockFlags">锁位</param>
    /// <returns>用户编号</returns>
    private int InsertUser(string userName, int lockFlags) =>
        _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = userName,
            NickName = userName,
            AccountName = "",
            Password = "x",
            PayPassword = "",
            CreateAt = DateTime.UtcNow,
            Face = "",
            RegIP = "",
            Lock = lockFlags,
            Function = 0,
            LoginAt = DateTime.UtcNow,
            LoginIP = "",
            AgentID = 0,
            Depth = 0,
            GroupID = 0,
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
            Unbalanced = 0
        }).ExecuteReturnIdentity();

    /// <summary>
    /// 内存缓存，测回调用。
    /// </summary>
    private sealed class MemorySimpXCache : ISimpXCache
    {
        private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);

        /// <inheritdoc />
        public void Add<T>(string cacheName, T value, TimeSpan expire) =>
            Add(cacheName, TryId(value), value, expire);

        /// <inheritdoc />
        public void Add<T>(string cacheName, object id, T? value, TimeSpan expire)
        {
            _ = expire;
            _store[Key(cacheName, id)] = value;
        }

        /// <inheritdoc />
        public Task<T?> GetOrAddAsync<T>(
            string cacheName,
            object? id,
            Func<CancellationToken, Task<T?>> factory,
            TimeSpan expire,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public T? GetOrAdd<T>(string cacheName, object? id, Func<T?> factory, TimeSpan expire) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public T? Get<T>(string cacheName, object? id)
        {
            if (_store.TryGetValue(Key(cacheName, id), out var v) && v is T t)
                return t;
            return default;
        }

        /// <inheritdoc />
        public void Remove(string cacheName, object? id) => _store.Remove(Key(cacheName, id));

        /// <inheritdoc />
        public void RemoveEntity<T>(string cacheName, T value) => Remove(cacheName, TryId(value));

        /// <inheritdoc />
        public void RemoveAll(string cacheName)
        {
            foreach (var k in _store.Keys.Where(k => k.StartsWith(cacheName + "|", StringComparison.Ordinal)).ToList())
                _store.Remove(k);
        }

        private static string Key(string cacheName, object? id) => $"{cacheName}|{id}";

        private static object? TryId<T>(T value)
        {
            var p = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("ID") ?? typeof(T).GetProperty("UserId");
            return p?.GetValue(value);
        }
    }
}

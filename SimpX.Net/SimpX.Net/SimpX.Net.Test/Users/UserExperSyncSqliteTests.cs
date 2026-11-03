using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>用户经验同步：Users.Exper ← usr_Money.Exper（内存 SQLite）。</summary>
public sealed class UserExperSyncSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly UserExperSyncService _svc;

    public UserExperSyncSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_Money] (
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [TotalMoney] REAL NOT NULL DEFAULT 0,
              [LockMoney] REAL NOT NULL DEFAULT 0,
              [Coin] REAL NOT NULL DEFAULT 0,
              [Credit] REAL NOT NULL DEFAULT 0,
              [Exper] INTEGER NOT NULL DEFAULT 0,
              [WashFlow] REAL NOT NULL DEFAULT 0,
              [Unbalanced] REAL NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [IsBase] INTEGER NOT NULL DEFAULT 0,
              PRIMARY KEY ([SiteID], [UserID], [Currency])
            );
            """);
        _svc = new UserExperSyncService(new TestBizDb(_db), NullLogger<UserExperSyncService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    /// <summary>优先用基础币种钱包的 Exper 写回 Users。</summary>
    [Fact]
    public async Task Sync_Prefers_IsBase_Wallet_Exper()
    {
        var userId = SeedUser(exper: 1);
        _db.Insertable(new UserMoney
        {
            SiteID = 1001,
            UserID = userId,
            Currency = 2,
            IsBase = false,
            Exper = 99
        }).ExecuteCommand();
        _db.Insertable(new UserMoney
        {
            SiteID = 1001,
            UserID = userId,
            Currency = 1,
            IsBase = true,
            Exper = 42
        }).ExecuteCommand();

        Assert.True(await _svc.SyncAsync(userId));
        var user = await _db.Queryable<User>().InSingleAsync(userId);
        Assert.Equal(42, user.Exper);
    }

    /// <summary>内存队列取出后交给同步服务。</summary>
    [Fact]
    public async Task Queue_Drain_Feeds_Sync()
    {
        var userId = SeedUser(exper: 0);
        _db.Insertable(new UserMoney
        {
            SiteID = 1001,
            UserID = userId,
            Currency = 1,
            IsBase = true,
            Exper = 7
        }).ExecuteCommand();

        var q = new MemoryLedgerSideEffectQueues();
        q.EnqueueUserExper(userId);
        q.EnqueueUserExper(userId);
        var due = q.TakeDueUserExper(10).Distinct().ToList();
        Assert.Single(due);
        Assert.True(await _svc.SyncAsync(due[0]));
        Assert.Equal(7, (await _db.Queryable<User>().InSingleAsync(userId)).Exper);
        Assert.Empty(q.TakeDueUserExper(10));
    }

    private int SeedUser(int exper)
    {
        return _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "exper_" + Guid.NewGuid().ToString("N")[..8],
            NickName = "n",
            Password = "x",
            Exper = exper,
            ActiveAt = DateTime.UtcNow.AddDays(-1)
        }).ExecuteReturnIdentity();
    }
}

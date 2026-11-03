using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>在线活动时间：ActiveAt 更新 + site_Online 快照（内存 SQLite）。</summary>
public sealed class UserActiveSyncSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly UserActiveSyncService _svc;

    public UserActiveSyncSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [site_Online] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [OnlineTime] TEXT NOT NULL,
              [Count] INTEGER NOT NULL DEFAULT 0,
              [RechargeCount] INTEGER NOT NULL DEFAULT 0,
              [PC] INTEGER NOT NULL DEFAULT 0,
              [Mobile] INTEGER NOT NULL DEFAULT 0,
              [Android] INTEGER NOT NULL DEFAULT 0,
              [IOS] INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS [rpt_UserDateMoney] (
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Date] TEXT NOT NULL,
              [Type] INTEGER NOT NULL,
              [Money] REAL NOT NULL DEFAULT 0,
              [Count] INTEGER NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              PRIMARY KEY ([SiteID], [UserID], [Date], [Type], [Currency])
            );
            """);
        _svc = new UserActiveSyncService(new TestBizDb(_db), NullLogger<UserActiveSyncService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    /// <summary>USER_ACTIVE 取出后写 ActiveAt。</summary>
    [Fact]
    public async Task UpdateActiveAt_From_Queue()
    {
        var userId = SeedUser(UserActiveSyncService.PlatformPc);
        var old = (await _db.Queryable<User>().InSingleAsync(userId)).ActiveAt;

        var q = new MemoryLedgerSideEffectQueues();
        q.EnqueueUserActive(userId);
        var due = q.TakeDueUserActive();
        Assert.Equal(new[] { userId }, due);
        Assert.Empty(q.TakeDueUserActive());

        var n = await _svc.UpdateActiveAtAsync(due);
        Assert.Equal(1, n);
        var user = await _db.Queryable<User>().InSingleAsync(userId);
        Assert.True(user.ActiveAt > old);
    }

    /// <summary>按站点写 site_Online，并统计端类型。</summary>
    [Fact]
    public async Task SaveOnlineSnapshot_Writes_SiteOnline()
    {
        var pc = SeedUser(UserActiveSyncService.PlatformPc);
        var mobile = SeedUser(UserActiveSyncService.PlatformMobile | UserActiveSyncService.PlatformAndroid);

        var count = await _svc.SaveOnlineSnapshotAsync([pc, mobile, mobile]);
        Assert.Equal(2, count);

        var rows = await _db.Queryable<SiteOnline>().ToListAsync();
        Assert.Single(rows);
        Assert.Equal(1001, rows[0].SiteID);
        Assert.Equal(2, rows[0].Count);
        Assert.Equal(1, rows[0].PC);
        Assert.Equal(1, rows[0].Mobile);
        Assert.Equal(1, rows[0].Android);
        Assert.Equal(0, rows[0].IOS);
    }

    private int SeedUser(int loginPlatform)
    {
        return _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "act_" + Guid.NewGuid().ToString("N")[..8],
            NickName = "n",
            Password = "x",
            LoginPlatform = loginPlatform,
            ActiveAt = DateTime.UtcNow.AddHours(-2)
        }).ExecuteReturnIdentity();
    }
}

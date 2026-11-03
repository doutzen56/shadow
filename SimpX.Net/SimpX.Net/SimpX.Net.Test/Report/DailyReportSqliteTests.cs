using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>日报 Job：空跑窗口 + 沉寂改组（内存 SQLite）。</summary>
public sealed class DailyReportSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly DailyReportService _svc;

    public DailyReportSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "usr_Group", "usr_LoginLog", "usr_Depth"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [MoneyLog] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Money] REAL NOT NULL DEFAULT 0,
              [LockMoney] REAL NOT NULL DEFAULT 0,
              [Balance] REAL NOT NULL DEFAULT 0,
              [CreateAt] TEXT NOT NULL,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [SubType] INTEGER NOT NULL DEFAULT 0,
              [SourceID] TEXT,
              [IP] TEXT,
              [LogDesc] TEXT,
              [TableID] INTEGER NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0
            );
            """);

        _svc = new DailyReportService(new TestBizDb(_db), NullLogger<DailyReportService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    [Fact]
    public async Task Tick_OffWindow_Is_Idle()
    {
        var result = await _svc.RunTickAsync(new DateTime(2026, 11, 2, 12, 30, 0));
        Assert.Equal(0, result.AgentUpdates);
        Assert.Equal(0, result.DormantMoved);
        Assert.Contains("空跑", result.Message);
    }

    [Fact]
    public async Task MoveDormant_Skips_RecentLogin_And_ExcludeGroup()
    {
        SeedGroup(10, "g10", member: 2);
        SeedGroup(DailyReportService.DormantTargetGroupId, "dormant", member: 0);
        SeedGroup(DailyReportService.DormantExcludeGroupId, "vip", member: 1);

        var old = DateTime.Now.AddDays(-120);
        var dormantId = SeedUser(groupId: 10, createAt: old, updateAt: old);
        var recentId = SeedUser(groupId: 10, createAt: old, updateAt: old);
        var excludedId = SeedUser(groupId: DailyReportService.DormantExcludeGroupId, createAt: old, updateAt: old);

        _db.Insertable(new LoginLog
        {
            SiteID = DailyReportService.DormantSiteId,
            UserID = recentId,
            LoginAt = DateTime.Now.AddDays(-1),
            IP = "127.0.0.1"
        }).ExecuteCommand();

        var moved = await _svc.MoveDormantUsersAsync(
            DailyReportService.DormantSiteId,
            DailyReportService.DormantExcludeGroupId,
            DailyReportService.DormantTargetGroupId,
            DailyReportService.DormantInactiveDays);

        Assert.Equal(1, moved);
        var dormant = await _db.Queryable<User>().InSingleAsync(dormantId);
        Assert.Equal(DailyReportService.DormantTargetGroupId, dormant.GroupID);
        Assert.Equal(10, dormant.OldGroupID);

        var recent = await _db.Queryable<User>().InSingleAsync(recentId);
        Assert.Equal(10, recent.GroupID);

        var excluded = await _db.Queryable<User>().InSingleAsync(excludedId);
        Assert.Equal(DailyReportService.DormantExcludeGroupId, excluded.GroupID);
    }

    [Fact]
    public async Task Tick_At_0205_Runs_Dormant()
    {
        SeedGroup(10, "g10", member: 1);
        SeedGroup(DailyReportService.DormantTargetGroupId, "dormant", member: 0);
        var old = DateTime.Now.AddDays(-120);
        var id = SeedUser(groupId: 10, createAt: old, updateAt: old);

        var result = await _svc.RunTickAsync(new DateTime(2026, 11, 2, 2, 5, 10));
        Assert.Equal(1, result.DormantMoved);
        var user = await _db.Queryable<User>().InSingleAsync(id);
        Assert.Equal(DailyReportService.DormantTargetGroupId, user.GroupID);
    }

    private int SeedUser(int groupId, DateTime createAt, DateTime updateAt)
    {
        return _db.Insertable(new User
        {
            SiteID = DailyReportService.DormantSiteId,
            UserName = "u_" + Guid.NewGuid().ToString("N")[..8],
            NickName = "n",
            Password = "x",
            GroupID = groupId,
            CreateAt = createAt,
            UpdateAt = updateAt
        }).ExecuteReturnIdentity();
    }

    private void SeedGroup(int id, string name, int member)
    {
        _db.Ado.ExecuteCommand(
            "INSERT INTO [usr_Group]([GroupID],[SiteID],[GroupName],[Member],[CreateAt],[IsDefault]) VALUES(@id,@site,@name,@member,@at,0)",
            new
            {
                id,
                site = DailyReportService.DormantSiteId,
                name,
                member,
                at = DateTime.Now
            });
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>代理层级更新日志：标完成（内存 SQLite；SP 在 SQLite 上会失败但仍标 Status）。</summary>
public sealed class UserAgentUpdateSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly UserAgentUpdateService _svc;

    public UserAgentUpdateSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_AgentUpdateLog] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [OldAgentParents] TEXT,
              [NewAgentParents] TEXT,
              [AdminID] INTEGER NOT NULL DEFAULT 0,
              [CreateAt] TEXT NOT NULL,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [FinishAt] TEXT
            );
            CREATE TABLE IF NOT EXISTS [Site_Currency] (
              [ID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [IsOpen] INTEGER NOT NULL DEFAULT 1,
              [Sort] INTEGER NOT NULL DEFAULT 0,
              [Accuracy] INTEGER NOT NULL DEFAULT 0,
              [SysOpen] INTEGER NOT NULL DEFAULT 1,
              [IsRegister] INTEGER NOT NULL DEFAULT 0
            );
            """);

        _svc = new UserAgentUpdateService(new TestBizDb(_db), NullLogger<UserAgentUpdateService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    [Fact]
    public async Task ProcessPending_Marks_Log_Status_True()
    {
        var now = DateTime.Now;
        _db.Insertable(new SiteCurrency
        {
            SiteID = 1002,
            Currency = 1,
            IsOpen = true,
            Sort = 1,
            Accuracy = 2,
            SysOpen = true,
            IsRegister = true
        }).ExecuteCommand();

        var logId = _db.Insertable(new UserAgentUpdateLog
        {
            SiteID = 1002,
            UserID = 88,
            OldAgentParents = "10,11",
            NewAgentParents = "11,12",
            AdminID = 1,
            CreateAt = now.AddMinutes(-5),
            Status = false,
            FinishAt = new DateTime(1900, 1, 1)
        }).ExecuteReturnIdentity();

        var done = await _svc.ProcessPendingAsync();
        Assert.Equal(1, done);

        var log = await _db.Queryable<UserAgentUpdateLog>().InSingleAsync(logId);
        Assert.True(log.Status);
        Assert.True(log.FinishAt > now.AddMinutes(-1));
    }

    [Fact]
    public async Task ProcessPending_Skips_Already_Done()
    {
        var now = DateTime.Now;
        _db.Insertable(new UserAgentUpdateLog
        {
            SiteID = 1002,
            UserID = 1,
            OldAgentParents = "1",
            NewAgentParents = "2",
            AdminID = 0,
            CreateAt = now.AddMinutes(-1),
            Status = true,
            FinishAt = now.AddMinutes(-1)
        }).ExecuteCommand();

        var done = await _svc.ProcessPendingAsync();
        Assert.Equal(0, done);
    }
}

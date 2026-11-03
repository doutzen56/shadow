using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Games;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>游戏日志采集延时检查（内存 SQLite）。</summary>
public sealed class GameLogDelayCheckSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly GameLogDelayCheckService _svc;

    public GameLogDelayCheckSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [game_Mark] (
              [SiteID] INTEGER NOT NULL,
              [Type] INTEGER NOT NULL,
              [Mark] INTEGER NOT NULL,
              [UpdateAt] TEXT NOT NULL,
              [DelayTime] INTEGER NOT NULL DEFAULT 0,
              PRIMARY KEY ([SiteID], [Type], [Mark])
            );
            """);

        _svc = new GameLogDelayCheckService(new TestBizDb(_db), NullLogger<GameLogDelayCheckService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    [Fact]
    public async Task CheckDelay_Counts_Stale_Marks()
    {
        _db.Insertable(new GameMark
        {
            SiteID = 1002,
            Type = 1,
            Mark = 0,
            UpdateAt = DateTime.Now.AddMinutes(-30),
            DelayTime = 10
        }).ExecuteCommand();
        _db.Insertable(new GameMark
        {
            SiteID = 1002,
            Type = 2,
            Mark = 0,
            UpdateAt = DateTime.Now.AddMinutes(-2),
            DelayTime = 10
        }).ExecuteCommand();
        _db.Insertable(new GameMark
        {
            SiteID = 1002,
            Type = 3,
            Mark = 0,
            UpdateAt = DateTime.Now.AddHours(-1),
            DelayTime = 0
        }).ExecuteCommand();

        var n = await _svc.CheckDelayAsync();
        Assert.Equal(1, n);
    }

    [Fact]
    public async Task CheckDelay_Empty_Returns_Zero()
    {
        var n = await _svc.CheckDelayAsync();
        Assert.Equal(0, n);
    }
}

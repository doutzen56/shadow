using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Games;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>系统游戏维护复位：库侧结束时间（内存 SQLite，无 Redis）。</summary>
public sealed class GameMaintainResetSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly GameMaintainResetService _svc;

    public GameMaintainResetSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [sys_GameSetting] (
              [Type] INTEGER PRIMARY KEY,
              [GameName] TEXT,
              [Setting] TEXT,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [DefaultRate] REAL NOT NULL DEFAULT 0,
              [Currencys] TEXT,
              [Languages] TEXT,
              [GameCategorys] TEXT,
              [Maintenance] TEXT,
              [MaintainPlan] TEXT,
              [Embedded] INTEGER NOT NULL DEFAULT 0
            );
            """);

        _svc = new GameMaintainResetService(new TestBizDb(_db), NullLogger<GameMaintainResetService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    [Fact]
    public async Task Reset_When_Maintenance_End_Passed()
    {
        var end = DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss");
        _db.Insertable(new SystemGameSetting
        {
            Type = 7,
            Name = "t",
            SettingString = "",
            Status = 2,
            Maintenance = $"{{\"ResumeAt\":\"{end}\"}}",
            Currencys = "",
            Languages = "",
            GameCategorys = ""
        }).ExecuteCommand();

        var n = await _svc.ResetExpiredAsync();
        Assert.Equal(1, n);

        var row = await _db.Queryable<SystemGameSetting>().InSingleAsync(7);
        Assert.Equal(GameMaintainResetService.StatusNormal, row.Status);
        Assert.Equal(string.Empty, row.Maintenance);
    }

    [Fact]
    public async Task Skip_When_No_End_And_No_Redis()
    {
        _db.Insertable(new SystemGameSetting
        {
            Type = 8,
            Name = "t2",
            SettingString = "",
            Status = 2,
            Maintenance = "Slot",
            Currencys = "",
            Languages = "",
            GameCategorys = ""
        }).ExecuteCommand();

        var n = await _svc.ResetExpiredAsync();
        Assert.Equal(0, n);

        var row = await _db.Queryable<SystemGameSetting>().InSingleAsync(8);
        Assert.Equal(2, row.Status);
    }

    [Fact]
    public void TryParse_ResumeAt_From_Json()
    {
        var ok = GameMaintainResetService.TryParseMaintenanceEnd(
            "{\"ResumeAt\":\"2026-01-01 12:00:00\"}",
            out var end);
        Assert.True(ok);
        Assert.Equal(new DateTime(2026, 1, 1, 12, 0, 0), end);
    }
}

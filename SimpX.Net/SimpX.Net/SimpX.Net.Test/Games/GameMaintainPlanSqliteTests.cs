using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Games;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>维护计划窗口评估 + 进入维护写库（内存 SQLite，无 Redis）。</summary>
public sealed class GameMaintainPlanSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly GameMaintainPlanService _svc;

    public GameMaintainPlanSqliteTests()
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
              [Embedded] INTEGER NOT NULL DEFAULT 0,
              [MaintainPlan] TEXT
            );
            CREATE TABLE IF NOT EXISTS [game_Setting] (
              [SiteID] INTEGER NOT NULL,
              [Game] INTEGER NOT NULL,
              [Name] TEXT,
              [IsOpen] INTEGER NOT NULL DEFAULT 0,
              [Credit] REAL NOT NULL DEFAULT 0,
              [Rate] REAL NOT NULL DEFAULT 0,
              [IsMonthly] INTEGER NOT NULL DEFAULT 0,
              [IsSelf] INTEGER NOT NULL DEFAULT 0,
              [Setting] TEXT,
              [IsFree] INTEGER NOT NULL DEFAULT 0,
              [Sort] INTEGER NOT NULL DEFAULT 0,
              [AgentRate] REAL NOT NULL DEFAULT 0,
              [IsWater] INTEGER NOT NULL DEFAULT 0,
              [Label] TEXT,
              [Backdrop] TEXT,
              [Logo] TEXT,
              [IsBackdrop] INTEGER NOT NULL DEFAULT 0,
              [DetailRate] TEXT,
              [IsDisable] INTEGER NOT NULL DEFAULT 0,
              [MinTransfer] INTEGER NOT NULL DEFAULT 0,
              [MaxTransfer] INTEGER NOT NULL DEFAULT 0,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [Maintenance] TEXT,
              PRIMARY KEY ([SiteID], [Game])
            );
            """);

        _svc = new GameMaintainPlanService(new TestBizDb(_db), NullLogger<GameMaintainPlanService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    [Fact]
    public void EvaluateWindow_Temporary_Enter_And_Resume()
    {
        var now = new DateTime(2026, 11, 2, 12, 0, 0);
        var plan = new GameMaintainPlanDto
        {
            Type = 7,
            Frequency = GameMaintainFrequency.Temporary,
            FrequencyValue = "2026-11-02",
            StartAt = "10:00",
            EndAt = "18:00",
            Maintenance = "Slot"
        };

        Assert.Equal(GameMaintainPlanWindow.Enter, GameMaintainPlanService.EvaluateWindow(plan, now));
        Assert.Equal(
            GameMaintainPlanWindow.Resume,
            GameMaintainPlanService.EvaluateWindow(plan, new DateTime(2026, 11, 2, 19, 0, 0)));
        Assert.Equal(
            GameMaintainPlanWindow.None,
            GameMaintainPlanService.EvaluateWindow(plan, new DateTime(2026, 11, 2, 9, 0, 0)));
    }

    [Fact]
    public void EvaluateWindow_EveryWeek_Uses_DayOfWeek()
    {
        // 2026-11-02 是 Monday = 1
        var now = new DateTime(2026, 11, 2, 12, 30, 0);
        var plan = new GameMaintainPlanDto
        {
            Type = 3,
            Frequency = GameMaintainFrequency.EveryWeek,
            FrequencyValue = "1",
            StartAt = "12:00",
            EndAt = "13:00"
        };

        Assert.Equal(GameMaintainPlanWindow.Enter, GameMaintainPlanService.EvaluateWindow(plan, now));
        Assert.Equal(
            GameMaintainPlanWindow.None,
            GameMaintainPlanService.EvaluateWindow(plan, new DateTime(2026, 11, 3, 12, 30, 0)));
    }

    [Fact]
    public async Task Run_Enter_Maintain_Updates_System_And_Site()
    {
        // 跨到次日结束，避免接近 24:00 或跨日时窗口判空。
        var now = DateTime.Now;
        var endDate = now.Date.AddDays(1).ToString("yyyy-MM-dd");
        var planJson =
            $"{{\"Type\":9,\"Frequency\":0,\"FrequencyValue\":\"{now:yyyy-MM-dd}\",\"EndDate\":\"{endDate}\",\"StartAt\":\"00:00\",\"EndAt\":\"23:59\",\"Maintenance\":\"Slot\"}}";

        _db.Insertable(new SystemGameSetting
        {
            Type = 9,
            Name = "t",
            SettingString = "",
            Status = 1,
            MaintainPlan = planJson,
            Maintenance = "",
            Currencys = "",
            Languages = "",
            GameCategorys = "Slot"
        }).ExecuteCommand();

        _db.Insertable(new GameSetting
        {
            SiteID = 1001,
            Game = 9,
            Name = "t",
            IsOpen = true,
            Status = 1,
            Maintenance = "",
            SettingString = "",
            Label = "",
            Backdrop = "",
            Logo = "",
            DetailRate = ""
        }).ExecuteCommand();

        var result = await _svc.RunAsync();
        Assert.True(result.Entered >= 1);

        var sys = await _db.Queryable<SystemGameSetting>().InSingleAsync(9);
        Assert.Equal(GameMaintainStatus.Maintain, sys.Status);
        Assert.Equal("Slot", sys.Maintenance);

        var site = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == 1001 && g.Game == 9)
            .FirstAsync();
        Assert.Equal(GameMaintainStatus.Maintain, site.Status);
        Assert.Contains("转账 OUT 后置大模块", result.Message);
    }
}

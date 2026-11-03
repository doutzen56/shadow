using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>游戏入口队列消费 + site_GameList 最小 upsert（内存 SQLite）。</summary>
public sealed class GameEntranceSyncSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly MemoryGameEntranceQueue _queue;
    private readonly GameEntranceSyncService _svc;

    public GameEntranceSyncSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [Site] (
              [SiteID] INTEGER PRIMARY KEY,
              [SiteName] TEXT,
              [CreateAt] TEXT,
              [NotifyAt] TEXT,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [Wallet] INTEGER NOT NULL DEFAULT 0,
              [IsCurrencySelect] INTEGER NOT NULL DEFAULT 0,
              [APIKey] TEXT,
              [Setting] TEXT,
              [APPSetting] TEXT,
              [PCSetting] TEXT,
              [GameCredit] REAL NOT NULL DEFAULT 0,
              [NotifyCredit] INTEGER NOT NULL DEFAULT 0,
              [AdminDomain] TEXT,
              [HomeTemplateName] TEXT
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
            CREATE TABLE IF NOT EXISTS [site_GameList] (
              [ID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [GameName] TEXT,
              [GameCode] TEXT,
              [Name] TEXT,
              [Type] INTEGER NOT NULL,
              [Category] INTEGER NOT NULL DEFAULT 0,
              [Code] TEXT NOT NULL,
              [IsEnable] INTEGER NOT NULL DEFAULT 0,
              [PcEnable] INTEGER NOT NULL DEFAULT 0,
              [AppEnable] INTEGER NOT NULL DEFAULT 0,
              [RecommendEnable] INTEGER NOT NULL DEFAULT 0,
              [ShowImg] TEXT,
              [Sort] INTEGER NOT NULL DEFAULT 0,
              [Tags] TEXT,
              [Recommend] INTEGER NOT NULL DEFAULT 0,
              [IsJack] INTEGER NOT NULL DEFAULT 0,
              [Remark] TEXT,
              [UpdateAt] TEXT,
              [CreateAt] TEXT,
              [GameTag] INTEGER NOT NULL DEFAULT 0,
              [NewEnable] INTEGER NOT NULL DEFAULT 0,
              [New] INTEGER NOT NULL DEFAULT 0
            );
            """);

        _queue = new MemoryGameEntranceQueue();
        _svc = new GameEntranceSyncService(
            new TestBizDb(_db),
            _queue,
            NullLogger<GameEntranceSyncService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    [Fact]
    public void TryParsePayload_Reads_Type_Code_Status()
    {
        var ok = GameEntranceQueue.TryParsePayload(
            """{"Type":12,"GameCode":"abc","GameName":"Demo","Status":1,"Category":2}""",
            out var payload);
        Assert.True(ok);
        Assert.Equal(12, payload.Type);
        Assert.Equal("abc", payload.Code);
        Assert.True(payload.IsOpen);
        Assert.Equal(2, payload.Category);
    }

    [Fact]
    public async Task Drain_Upserts_SiteGameList_Open_Flags()
    {
        _db.Insertable(new Site
        {
            ID = 2001,
            Name = "s",
            Status = GameEntranceSyncService.SiteStatusNormal,
            CreateAt = DateTime.Now,
            NotifyAt = DateTime.Now,
            SettingString = "",
            APPSettingString = "",
            PCSettingString = "",
            APIKey = Guid.NewGuid()
        }).ExecuteCommand();

        _db.Insertable(new GameSetting
        {
            SiteID = 2001,
            Game = 12,
            Name = "g",
            IsOpen = true,
            Status = 1,
            SettingString = "",
            Label = "",
            Backdrop = "",
            Logo = "",
            DetailRate = "",
            Maintenance = ""
        }).ExecuteCommand();

        _queue.Enqueue("""{"Type":12,"Code":"slot01","Name":"Slot One","Status":1,"Category":1}""");

        var result = await _svc.DrainAndSyncAsync();
        Assert.Equal(1, result.Drained);
        Assert.Equal(1, result.Synced);
        Assert.True(result.RowsTouched >= 1);

        var row = await _db.Queryable<SiteGameList>()
            .Where(g => g.SiteID == 2001 && g.Type == 12 && g.Code == "slot01")
            .FirstAsync();
        Assert.True(row.IsEnable);
        Assert.True(row.PcEnable);
        Assert.Equal("Slot One", row.Name);
        Assert.Contains("SystemGameUpdate", result.Message);
    }

    [Fact]
    public async Task Drain_Close_Disables_Existing_Row()
    {
        _db.Insertable(new Site
        {
            ID = 2002,
            Name = "s2",
            Status = GameEntranceSyncService.SiteStatusNormal,
            CreateAt = DateTime.Now,
            NotifyAt = DateTime.Now,
            SettingString = "",
            APPSettingString = "",
            PCSettingString = "",
            APIKey = Guid.NewGuid()
        }).ExecuteCommand();

        _db.Insertable(new GameSetting
        {
            SiteID = 2002,
            Game = 5,
            Name = "g",
            IsOpen = true,
            Status = 1,
            SettingString = "",
            Label = "",
            Backdrop = "",
            Logo = "",
            DetailRate = "",
            Maintenance = ""
        }).ExecuteCommand();

        _db.Insertable(new SiteGameList
        {
            SiteID = 2002,
            Type = 5,
            Code = "x",
            GameName = "X",
            Name = "X",
            IsEnable = true,
            PcEnable = true,
            AppEnable = true,
            ShowImg = "",
            Tags = "",
            Remark = "",
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now
        }).ExecuteCommand();

        _queue.Enqueue("""{"Type":5,"Code":"x","Status":0}""");
        var result = await _svc.DrainAndSyncAsync();
        Assert.Equal(1, result.Synced);

        var row = await _db.Queryable<SiteGameList>()
            .Where(g => g.SiteID == 2002 && g.Type == 5 && g.Code == "x")
            .FirstAsync();
        Assert.False(row.IsEnable);
        Assert.False(row.AppEnable);
    }
}

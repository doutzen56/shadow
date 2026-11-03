using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>用户游戏日结：主表/代理表/时区表/对战（内存 SQLite）。</summary>
public sealed class UserGameDateSyncSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly UserGameDateSyncService _svc;
    private readonly FakeEsAggregate _es = new();

    public UserGameDateSyncSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [rpt_UserDateGame] (
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Date] TEXT NOT NULL,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [Category] INTEGER NOT NULL DEFAULT 0,
              [BetAmount] REAL NOT NULL DEFAULT 0,
              [Money] REAL NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              PRIMARY KEY ([SiteID], [UserID], [Date], [Type], [Category], [Currency])
            );
            CREATE TABLE IF NOT EXISTS [rpt_UserDateGameProxy] (
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Date] TEXT NOT NULL,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [Category] INTEGER NOT NULL DEFAULT 0,
              [BetAmount] REAL NOT NULL DEFAULT 0,
              [Money] REAL NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [OrderCount] INTEGER NOT NULL DEFAULT 0,
              PRIMARY KEY ([SiteID], [UserID], [Date], [Type], [Category], [Currency])
            );
            CREATE TABLE IF NOT EXISTS [rpt_UserDateGame_UETimeZone] (
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Date] TEXT NOT NULL,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [Category] INTEGER NOT NULL DEFAULT 0,
              [BetAmount] REAL NOT NULL DEFAULT 0,
              [Money] REAL NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [TimeType] INTEGER NOT NULL DEFAULT 0,
              [OrderCount] INTEGER NOT NULL DEFAULT 0,
              PRIMARY KEY ([SiteID], [UserID], [Date], [Type], [Category], [Currency], [TimeType])
            );
            CREATE TABLE IF NOT EXISTS [rpt_UserDateGameBattle] (
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Date] TEXT NOT NULL,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [Category] INTEGER NOT NULL DEFAULT 0,
              [BetAmount] REAL NOT NULL DEFAULT 0,
              [Money] REAL NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [OrderCount] INTEGER NOT NULL DEFAULT 0,
              PRIMARY KEY ([SiteID], [UserID], [Date], [Type], [Category], [Currency])
            );
            CREATE TABLE IF NOT EXISTS [game_UETimeZone] (
              [ID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [GameName] TEXT,
              [Hour] INTEGER NOT NULL DEFAULT 0
            );
            """);

        _svc = new UserGameDateSyncService(
            new TestBizDb(_db),
            _es,
            NullLogger<UserGameDateSyncService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    [Fact]
    public async Task Upsert_Member_To_Main_And_Proxy_To_ProxyTable()
    {
        var memberId = SeedUser(0);
        var proxyId = SeedUser(UserGameDateSyncService.UserTypeProxyAgent);

        _es.Buckets =
        [
            Bucket(1002, memberId, bet: 100, money: -5),
            Bucket(1002, proxyId, bet: 999, money: 1)
        ];

        var result = await _svc.SyncAsync();
        Assert.False(result.NotConfigured);
        Assert.True(result.MainUpserted >= 1);
        Assert.True(result.ProxyUpserted >= 1);

        var main = await _db.Queryable<UserDateGame>().ToListAsync();
        Assert.Single(main);
        Assert.Equal(memberId, main[0].UserID);
        Assert.Equal(100m, main[0].BetMoney);

        var proxy = await _db.Queryable<UserDateGameProxy>().ToListAsync();
        Assert.Single(proxy);
        Assert.Equal(proxyId, proxy[0].UserID);
        Assert.Equal(999m, proxy[0].BetMoney);
    }

    [Fact]
    public async Task Battle_Buckets_Upsert()
    {
        var uid = SeedUser(0);
        _es.BattleBuckets =
        [
            new UserGameDateEsBucket
            {
                SiteID = 1002,
                UserID = uid,
                Category = GameOrderSummaryService.CategoryFantasy,
                Type = 141,
                Currency = 1,
                BetAmount = 50,
                Money = 3,
                OrderCount = 2
            }
        ];
        // 主聚合空；探活仍走 AggregateAsync
        _es.Buckets = [];

        var result = await _svc.SyncAsync();
        Assert.True(result.BattleUpserted >= 1);
        var rows = await _db.Queryable<UserDateGameBattle>().ToListAsync();
        Assert.Single(rows);
        Assert.Equal(uid, rows[0].UserID);
        Assert.Equal(50m, rows[0].BetMoney);
        Assert.Equal(GameOrderSummaryService.CategoryFantasy, rows[0].Category);
    }

    [Fact]
    public async Task NotConfigured_Returns_Zero()
    {
        _es.NotConfigured = true;
        var result = await _svc.SyncAsync();
        Assert.True(result.NotConfigured);
        Assert.Equal(0, result.Upserted);
    }

    private int SeedUser(int type) =>
        _db.Insertable(new User
        {
            SiteID = 1002,
            UserName = "u_" + Guid.NewGuid().ToString("N")[..6],
            NickName = "n",
            Password = "x",
            Type = type,
            CreateAt = DateTime.UtcNow
        }).ExecuteReturnIdentity();

    private static UserGameDateEsBucket Bucket(int siteId, int userId, decimal bet, decimal money) => new()
    {
        SiteID = siteId,
        UserID = userId,
        Category = 2,
        Type = 10,
        Currency = 1,
        BetAmount = bet,
        BetMoney = bet,
        Money = money,
        OrderCount = 1
    };

    private sealed class FakeEsAggregate : IUserGameDateEsAggregate
    {
        public List<UserGameDateEsBucket> Buckets { get; set; } = new();
        public List<UserGameDateEsBucket> BattleBuckets { get; set; } = new();
        public bool NotConfigured { get; set; }

        public Task<UserGameDateEsAggregateResult> AggregateAsync(
            DateTime startBj,
            DateTime endBj,
            IReadOnlyCollection<string> typeNames = null,
            CancellationToken cancellationToken = default)
        {
            if (NotConfigured)
            {
                return Task.FromResult(new UserGameDateEsAggregateResult
                {
                    NotConfigured = true,
                    Message = "fake"
                });
            }

            return Task.FromResult(new UserGameDateEsAggregateResult { Buckets = Buckets });
        }

        public Task<UserGameDateEsAggregateResult> AggregateBattleAsync(
            DateTime startBj,
            DateTime endBj,
            CancellationToken cancellationToken = default)
        {
            if (NotConfigured)
            {
                return Task.FromResult(new UserGameDateEsAggregateResult
                {
                    NotConfigured = true,
                    Message = "fake"
                });
            }

            return Task.FromResult(new UserGameDateEsAggregateResult { Buckets = BattleBuckets });
        }
    }
}

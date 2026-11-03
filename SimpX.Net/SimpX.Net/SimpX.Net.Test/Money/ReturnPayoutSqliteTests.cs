using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Agents;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 返点生成与发放（内存 SQLite + 假账本）。复用 usr_RebateLog Type=AgentReturn。
/// </summary>
public sealed class ReturnPayoutSqliteTests : IDisposable
{
    private const int SiteId = 1001;
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(SiteId, "ar.test");
    private readonly RecordingMoneyLedger _money = new();
    private readonly ReturnPayoutService _svc;

    /// <summary>建表。</summary>
    public ReturnPayoutSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_RebateLog] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [PlanID] INTEGER NOT NULL DEFAULT 0,
              [UserID] INTEGER NOT NULL,
              [StartAt] TEXT,
              [EndAt] TEXT,
              [Money] REAL NOT NULL,
              [CreateAt] TEXT NOT NULL,
              [CheckAt] TEXT,
              [Detail] TEXT,
              [Type] TEXT,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [FaildMsg] TEXT,
              [RebateID] INTEGER NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [SortID] INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS [agent_User] (
              [AgentID] INTEGER PRIMARY KEY,
              [SiteID] INTEGER NOT NULL,
              [ParentID] INTEGER NOT NULL DEFAULT 0,
              [CreateAt] TEXT,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [CommissionID] INTEGER NOT NULL DEFAULT 0,
              [ReturnID] INTEGER NOT NULL DEFAULT 0,
              [Permission] INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS [usr_Depth] (
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [ChildID] INTEGER NOT NULL,
              [Depth] INTEGER NOT NULL,
              PRIMARY KEY ([SiteID], [UserID], [ChildID])
            );
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
            """);
        _svc = new ReturnPayoutService(new TestBizDb(_db), _site, _money);
    }

    /// <summary>区间与已有非失败 AgentReturn 记录相交 → 拒。</summary>
    [Fact]
    public async Task Create_rejects_overlapping_range()
    {
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        var agentId = InsertUser("u_overlap");
        _db.Insertable(new UserRebateLog
        {
            SiteID = SiteId,
            PlanID = 0,
            UserID = agentId,
            StartAt = new DateTime(2026, 10, 1),
            EndAt = new DateTime(2026, 10, 10),
            Money = 1,
            CreateAt = now,
            CheckAt = DateTime.MinValue,
            Detail = "",
            Type = ReturnPayoutService.LogTypeAgentReturn,
            Status = ReturnPayoutService.StatusNone,
            FaildMsg = "",
            RebateID = 1,
            Currency = 1,
            SortID = 0
        }).ExecuteCommand();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CreateAsync(new DateTime(2026, 10, 8), new DateTime(2026, 10, 12), now));
    }

    /// <summary>有 ReturnID 代理 + 下级投注 → 生成待发；Pay 仍可写账本。</summary>
    [Fact]
    public async Task Create_inserts_one_log_and_Pay_still_works()
    {
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        var start = new DateTime(2026, 10, 1);
        var end = new DateTime(2026, 10, 7);

        var agentId = InsertUser("agent_ar");
        var childId = InsertUser("child_ar");

        _db.Insertable(new AgentUser
        {
            ID = agentId,
            SiteID = SiteId,
            ParentID = 0,
            CreateAt = now,
            Status = AgentUser.AgentStatus.Normal,
            CommissionID = 0,
            ReturnID = 9,
            Permission = 0
        }).ExecuteCommand();

        _db.Insertable(new UserDepth
        {
            SiteID = SiteId,
            UserID = agentId,
            ChildID = childId,
            Depth = 1
        }).ExecuteCommand();

        // 下级投注 1000 → Money = 1000 × 0.01 = 10
        _db.Insertable(new UserDateGame
        {
            SiteID = SiteId,
            UserID = childId,
            Date = new DateTime(2026, 10, 3),
            Type = 0,
            Category = 0,
            BetMoney = 1000m,
            Money = 0,
            Currency = 1
        }).ExecuteCommand();

        var result = await _svc.CreateAsync(start, end, now);
        Assert.Equal(1, result.Success);
        Assert.Equal(0, result.Failed);

        var logs = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == SiteId && l.UserID == agentId && l.Type == ReturnPayoutService.LogTypeAgentReturn)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(10m, logs[0].Money);
        Assert.Equal(ReturnPayoutService.StatusNone, logs[0].Status);
        Assert.Equal(9, logs[0].RebateID);

        var ok = await _svc.PayAsync(logs[0].ID);
        Assert.True(ok);
        Assert.Contains(_money.Logs, l => l.Type == ReturnPayoutService.MoneyTypeAgentReturn && l.Money == 10m);
        var paid = await _db.Queryable<UserRebateLog>().InSingleAsync(logs[0].ID);
        Assert.Equal(ReturnPayoutService.StatusPaid, paid.Status);
    }

    /// <summary>截止日不能到今天。</summary>
    [Fact]
    public async Task Create_rejects_when_end_is_today()
    {
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CreateAsync(new DateTime(2026, 10, 18), new DateTime(2026, 10, 20), now));
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private int InsertUser(string name)
    {
        return _db.Insertable(new User
        {
            SiteID = SiteId,
            UserName = name,
            NickName = name,
            AccountName = "",
            Password = "x",
            PayPassword = "",
            CreateAt = DateTime.UtcNow,
            Face = "",
            RegIP = "",
            Lock = 0,
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
            Unbalanced = 0,
            InviteID = 0,
            UpdateAt = DateTime.UtcNow,
            ProxyRate = 0,
            WashingRatio = "",
            LastReturn = DateTime.UtcNow,
            Coin = 0,
            DisableCurrency = "",
            Address = "",
            Birthday = DateTime.UtcNow,
            Telegram = "",
            Line = "",
            Google = "",
            Info = "",
            DeviceId = "",
            RegisteredFrom = "",
            PixelID = "",
            InitLevel = 0,
            Is_Temporary_Password = false,
            BlockGame = "",
            OldGroupID = 0
        }).ExecuteReturnIdentity();
    }

    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        private readonly int _siteId;
        private readonly string _host;

        public FixedSiteAccessor(int siteId, string host)
        {
            _siteId = siteId;
            _host = host;
        }

        public int? SiteId => _siteId;
        public string Host => _host;
        public int RequireSiteId() => _siteId;
    }

}

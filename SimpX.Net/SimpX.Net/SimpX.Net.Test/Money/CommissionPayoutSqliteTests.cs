using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Agents;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 佣金生成与发放（内存 SQLite + 假账本）。
/// </summary>
public sealed class CommissionPayoutSqliteTests : IDisposable
{
    private const int SiteId = 1001;
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(SiteId, "cm.test");
    private readonly RecordingMoneyLedger _money = new();
    private readonly CommissionPayoutService _svc;

    /// <summary>建表。</summary>
    public CommissionPayoutSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_CommissionLog] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [CommID] INTEGER NOT NULL DEFAULT 0,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [StartDate] TEXT,
              [EndDate] TEXT,
              [ValidUserCount] INTEGER NOT NULL DEFAULT 0,
              [RechargeWithdrawFee] REAL NOT NULL DEFAULT 0,
              [ActivityMoney] REAL NOT NULL DEFAULT 0,
              [Money] REAL NOT NULL,
              [CreateAt] TEXT,
              [CheckAt] TEXT,
              [Detail] TEXT,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0
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
            CREATE TABLE IF NOT EXISTS [agent_Commission] (
              [CommissionID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [CommissionName] TEXT,
              [Award] REAL NOT NULL DEFAULT 0,
              [Return] REAL NOT NULL DEFAULT 0,
              [Cost] REAL NOT NULL DEFAULT 0,
              [MemberMoney] REAL NOT NULL DEFAULT 0,
              [Setting] TEXT,
              [IsOpen] INTEGER NOT NULL DEFAULT 1,
              [IsContract] INTEGER NOT NULL DEFAULT 0,
              [AgentCount] INTEGER NOT NULL DEFAULT 0,
              [MaxRate] REAL NOT NULL DEFAULT 0,
              [CreateAt] TEXT
            );
            CREATE TABLE IF NOT EXISTS [usr_Depth] (
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [ChildID] INTEGER NOT NULL,
              [Depth] INTEGER NOT NULL,
              PRIMARY KEY ([SiteID], [UserID], [ChildID])
            );
            CREATE TABLE IF NOT EXISTS [rpt_AgentStatistics] (
              [Date] TEXT NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Currency] INTEGER NOT NULL,
              [SiteID] INTEGER NOT NULL,
              [GameMoney] REAL NOT NULL DEFAULT 0,
              [MoneyEvent] REAL NOT NULL DEFAULT 0,
              [MoneyRebate] REAL NOT NULL DEFAULT 0,
              [RechargeFee] REAL NOT NULL DEFAULT 0,
              [WithdrawFee] REAL NOT NULL DEFAULT 0,
              PRIMARY KEY ([Date], [UserID], [Currency])
            );
            """);
        _svc = new CommissionPayoutService(new TestBizDb(_db), _site, _money);
    }

    /// <summary>起始晚于截止 → 门禁。</summary>
    [Fact]
    public async Task Create_rejects_when_start_after_end()
    {
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CreateAsync(new DateTime(2026, 10, 10), new DateTime(2026, 10, 5), now));
    }

    /// <summary>截止日不能到今天。</summary>
    [Fact]
    public async Task Create_rejects_when_end_is_today()
    {
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CreateAsync(new DateTime(2026, 10, 18), new DateTime(2026, 10, 20), now));
    }

    /// <summary>凌晨 1 点前不能生成。</summary>
    [Fact]
    public async Task Create_rejects_before_one_am()
    {
        var now = new DateTime(2026, 10, 20, 0, 30, 0);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CreateAsync(new DateTime(2026, 10, 10), new DateTime(2026, 10, 15), now));
    }

    /// <summary>区间与已有非失败记录相交 → 拒。</summary>
    [Fact]
    public async Task Create_rejects_overlapping_range()
    {
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        var agentId = InsertUser("u_overlap");
        _db.Insertable(new UserCommissionLog
        {
            CommID = 1,
            SiteID = SiteId,
            UserID = agentId,
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 10),
            Money = 1,
            CreateAt = now,
            CheckAt = DateTime.MinValue,
            Detail = "",
            Status = CommissionPayoutService.StatusNone,
            Currency = 1
        }).ExecuteCommand();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CreateAsync(new DateTime(2026, 10, 8), new DateTime(2026, 10, 12), now));
    }

    /// <summary>有代理 + Depth=1 报表 → 生成 1 条待发；Pay 仍可写账本。</summary>
    [Fact]
    public async Task Create_inserts_one_log_and_Pay_still_works()
    {
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        var start = new DateTime(2026, 10, 1);
        var end = new DateTime(2026, 10, 7);

        var agentId = InsertUser("agent_cm");
        var childId = InsertUser("child_cm");

        var planId = _db.Insertable(new AgentCommission
        {
            SiteID = SiteId,
            Name = "plan-a",
            Award = 0.1m,
            Return = 0.1m,
            MemberMoney = 0,
            SettingString = """{"Rate":0.5}"""
        }).ExecuteReturnIdentity();

        _db.Insertable(new AgentUser
        {
            ID = agentId,
            SiteID = SiteId,
            ParentID = 0,
            CreateAt = now,
            Status = AgentUser.AgentStatus.Normal,
            CommissionID = planId,
            ReturnID = 0,
            Permission = 0
        }).ExecuteCommand();

        _db.Insertable(new UserDepth
        {
            SiteID = SiteId,
            UserID = agentId,
            ChildID = childId,
            Depth = 1
        }).ExecuteCommand();

        // 下级游戏盈亏 -100 → detail.GameMoney=100 → Money=50
        _db.Insertable(new ReportAgentStatis
        {
            Date = new DateTime(2026, 10, 3),
            UserID = childId,
            Currency = 1,
            SiteID = SiteId,
            GameMoney = -100m,
            MoneyEvent = 0,
            MoneyRebate = 0,
            RechargeFee = 0,
            WithdrawFee = 0
        }).ExecuteCommand();

        var result = await _svc.CreateAsync(start, end, now);
        Assert.Equal(1, result.Success);
        Assert.Equal(0, result.Failed);

        var logs = await _db.Queryable<UserCommissionLog>()
            .Where(l => l.SiteID == SiteId && l.UserID == agentId)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(50m, logs[0].Money);
        Assert.Equal(CommissionPayoutService.StatusNone, logs[0].Status);

        var ok = await _svc.PayAsync(logs[0].ID);
        Assert.True(ok);
        Assert.Contains(_money.Logs, l => l.Type == CommissionPayoutService.MoneyTypeCommission && l.Money == 50m);
        var paid = await _db.Queryable<UserCommissionLog>().InSingleAsync(logs[0].ID);
        Assert.Equal(CommissionPayoutService.StatusPaid, paid.Status);
    }

    /// <summary>Setting 无 Rate 时走 Award/Return 简化公式，Money≤0 不插行。</summary>
    [Fact]
    public async Task Create_skips_when_money_not_positive()
    {
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        var agentId = InsertUser("agent_zero");
        var childId = InsertUser("child_zero");
        var planId = _db.Insertable(new AgentCommission
        {
            SiteID = SiteId,
            Name = "plan-z",
            Award = 1m,
            Return = 1m,
            MemberMoney = 0,
            SettingString = ""
        }).ExecuteReturnIdentity();
        _db.Insertable(new AgentUser
        {
            ID = agentId,
            SiteID = SiteId,
            ParentID = 0,
            CreateAt = now,
            Status = AgentUser.AgentStatus.Normal,
            CommissionID = planId
        }).ExecuteCommand();
        _db.Insertable(new UserDepth
        {
            SiteID = SiteId,
            UserID = agentId,
            ChildID = childId,
            Depth = 1
        }).ExecuteCommand();
        // 游戏盈亏为正（玩家赢）→ -GameMoney 为负，佣金为 0
        _db.Insertable(new ReportAgentStatis
        {
            Date = new DateTime(2026, 10, 3),
            UserID = childId,
            Currency = 1,
            SiteID = SiteId,
            GameMoney = 50m
        }).ExecuteCommand();

        var result = await _svc.CreateAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 7), now);
        Assert.Equal(0, result.Success);
        var count = await _db.Queryable<UserCommissionLog>().Where(l => l.UserID == agentId).CountAsync();
        Assert.Equal(0, count);
    }

    /// <summary>两档门槛：未达高档用低 Rate；达标用高 Rate；并扣 Event×Award。</summary>
    [Fact]
    public void Ladder_picks_highest_qualifying_tier()
    {
        const string setting =
            """[{"Money":0,"Member":0,"Rate":0.1},{"Money":100,"Member":0,"Rate":0.5}]""";

        Assert.Equal(0.1m, CommissionPayoutService.TrySelectLadderRate(setting, gameMoney: 50m, userCount: 0));
        Assert.Equal(0.5m, CommissionPayoutService.TrySelectLadderRate(setting, gameMoney: 200m, userCount: 0));

        var plan = new AgentCommission
        {
            Award = 0.2m,
            Return = 0.1m,
            SettingString = setting
        };
        // 低档：0.1×50 − 10×0.2 − 0 − 0 = 3
        Assert.Equal(3m, CommissionPayoutService.ComputeCommissionMoney(plan, 50m, sumEvent: 10m, sumRebate: 0m, fees: 0m));
        // 高档：0.5×200 − 10×0.2 − 5×0.1 − 1 = 100 − 2 − 0.5 − 1 = 96.5
        Assert.Equal(96.5m, CommissionPayoutService.ComputeCommissionMoney(plan, 200m, sumEvent: 10m, sumRebate: 5m, fees: 1m));
    }

    /// <summary>两档门槛写入日志：GameMoney=200 走高档 Rate=0.5 → Money=100。</summary>
    [Fact]
    public async Task Create_uses_higher_ladder_when_threshold_met()
    {
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        var agentId = InsertUser("agent_ladder");
        var childId = InsertUser("child_ladder");
        var planId = _db.Insertable(new AgentCommission
        {
            SiteID = SiteId,
            Name = "plan-ladder",
            Award = 0m,
            Return = 0m,
            MemberMoney = 0,
            SettingString = """[{"Money":0,"Member":0,"Rate":0.1},{"Money":100,"Member":0,"Rate":0.5}]"""
        }).ExecuteReturnIdentity();
        _db.Insertable(new AgentUser
        {
            ID = agentId,
            SiteID = SiteId,
            ParentID = 0,
            CreateAt = now,
            Status = AgentUser.AgentStatus.Normal,
            CommissionID = planId
        }).ExecuteCommand();
        _db.Insertable(new UserDepth
        {
            SiteID = SiteId,
            UserID = agentId,
            ChildID = childId,
            Depth = 1
        }).ExecuteCommand();
        // 下级 GameMoney=-200 → detail.GameMoney=200 ≥ 100 → Rate=0.5 → Money=100
        _db.Insertable(new ReportAgentStatis
        {
            Date = new DateTime(2026, 10, 3),
            UserID = childId,
            Currency = 1,
            SiteID = SiteId,
            GameMoney = -200m
        }).ExecuteCommand();

        var result = await _svc.CreateAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 7), now);
        Assert.Equal(1, result.Success);
        var log = await _db.Queryable<UserCommissionLog>().Where(l => l.UserID == agentId).FirstAsync();
        Assert.Equal(100m, log.Money);
    }

    /// <summary>旧 AgentStatus.Normal 数值为 1。</summary>
    [Fact]
    public void AgentStatus_Normal_is_one()
    {
        Assert.Equal(1, (int)AgentUser.AgentStatus.Normal);
        Assert.Equal(0, (int)AgentUser.AgentStatus.None);
        Assert.Equal(2, (int)AgentUser.AgentStatus.Locked);
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

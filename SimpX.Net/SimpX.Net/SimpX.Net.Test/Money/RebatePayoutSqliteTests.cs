using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 返水计划列表与发放（内存 SQLite + 假账本）。禁写 BetWin30。
/// </summary>
public sealed class RebatePayoutSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "rebate.test");
    private readonly RecordingMoneyLedger _money = new();
    private readonly RebatePayoutService _svc;
    private readonly int _planId;
    private readonly int _userId;

    /// <summary>
    /// 建计划与待发放日志。
    /// </summary>
    public RebatePayoutSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_RebatePlan] (
              [PlanID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [PlanName] TEXT,
              [CreateAt] TEXT NOT NULL,
              [AdminID] INTEGER NOT NULL DEFAULT 0,
              [Money] REAL NOT NULL DEFAULT 0,
              [Paid] REAL NOT NULL DEFAULT 0,
              [Success] INTEGER NOT NULL DEFAULT 0,
              [Faild] INTEGER NOT NULL DEFAULT 0,
              [Total] INTEGER NOT NULL DEFAULT 0,
              [StartAt] TEXT,
              [EndAt] TEXT,
              [FlowRate] REAL NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [FromCurrency] INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS [usr_RebateLog] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [PlanID] INTEGER NOT NULL,
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
            """);

        _userId = _db.Insertable(BuildUser("u_rb")).ExecuteReturnIdentity();
        _planId = _db.Insertable(new UserRebatePlan
        {
            SiteID = 1001,
            Name = "周返",
            CreateAt = DateTime.UtcNow,
            AdminID = 1,
            Money = 30,
            Paid = 0,
            Success = 0,
            Faild = 0,
            Total = 2,
            StartAt = DateTime.UtcNow.AddDays(-7),
            EndAt = DateTime.UtcNow,
            FlowRate = 1,
            Currency = 1,
            FromCurrency = 1
        }).ExecuteReturnIdentity();

        _db.Insertable(new UserRebateLog
        {
            SiteID = 1001,
            PlanID = _planId,
            UserID = _userId,
            StartAt = DateTime.UtcNow.AddDays(-7),
            EndAt = DateTime.UtcNow,
            Money = 12,
            CreateAt = DateTime.UtcNow,
            CheckAt = DateTime.UtcNow,
            Detail = "",
            Type = "",
            Status = RebatePayoutService.LogStatusNone,
            FaildMsg = "",
            RebateID = 0,
            Currency = 1,
            SortID = 0
        }).ExecuteCommand();
        _db.Insertable(new UserRebateLog
        {
            SiteID = 1001,
            PlanID = _planId,
            UserID = _userId,
            StartAt = DateTime.UtcNow.AddDays(-7),
            EndAt = DateTime.UtcNow,
            Money = 18,
            CreateAt = DateTime.UtcNow,
            CheckAt = DateTime.UtcNow,
            Detail = "",
            Type = "",
            Status = RebatePayoutService.LogStatusNone,
            FaildMsg = "",
            RebateID = 0,
            Currency = 1,
            SortID = 1
        }).ExecuteCommand();

        _svc = new RebatePayoutService(new TestBizDb(_db), _site, _money);
    }

    /// <summary>
    /// 计划列表含本站计划。
    /// </summary>
    [Fact]
    public async Task ListPlans_returns_site_plan()
    {
        var list = await _svc.ListPlansAsync();
        Assert.Contains(list, x => x.ID == _planId && x.Name == "周返");
    }

    /// <summary>
    /// 发放待审日志写账本并更新计划成功数。
    /// </summary>
    [Fact]
    public async Task PayPlan_pays_pending_logs()
    {
        var ok = await _svc.PayPlanAsync(_planId);
        Assert.Equal(2, ok);
        Assert.Equal(2, _money.Logs.Count);
        Assert.All(_money.Logs, l => Assert.Equal(RebatePayoutService.MoneyTypeRebate, l.Type));

        var plan = await _db.Queryable<UserRebatePlan>().InSingleAsync(_planId);
        Assert.Equal(2, plan.Success);
        Assert.Equal(30m, plan.Paid);

        var pending = await _db.Queryable<UserRebateLog>()
            .Where(l => l.PlanID == _planId && l.Status == RebatePayoutService.LogStatusNone)
            .CountAsync();
        Assert.Equal(0, pending);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private static User BuildUser(string name) => new()
    {
        SiteID = 1001,
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
    };

    /// <summary>假账本。</summary>

    /// <summary>定站。</summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>固定站。</summary>
        public FixedSiteAccessor(int? siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        /// <inheritdoc />
        public int? SiteId { get; }

        /// <inheritdoc />
        public string Host { get; }

        /// <inheritdoc />
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

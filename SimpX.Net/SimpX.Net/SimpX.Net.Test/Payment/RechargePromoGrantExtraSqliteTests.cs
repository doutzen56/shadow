using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 阶梯充值与首充发奖（内存 SQLite）。
/// </summary>
public sealed class RechargePromoGrantExtraSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly int _uid;
    private readonly RechargePromoGrantService _svc;

    /// <summary>
    /// 建表与用户。
    /// </summary>
    public RechargePromoGrantExtraSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site", "site_Event"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_RechargeOrder] (
              [OrderID] TEXT NOT NULL PRIMARY KEY,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Money] REAL NOT NULL,
              [Fee] REAL NOT NULL DEFAULT 0,
              [Cost] REAL NOT NULL DEFAULT 0,
              [CreateAt] TEXT NOT NULL,
              [PayID] INTEGER NOT NULL DEFAULT 0,
              [IsPay] INTEGER NOT NULL DEFAULT 0,
              [PayAt] TEXT,
              [Paid] REAL NOT NULL DEFAULT 0,
              [SystemID] TEXT,
              [AdminID] INTEGER NOT NULL DEFAULT 0,
              [Count] INTEGER NOT NULL DEFAULT 0,
              [Remark] TEXT,
              [IP] TEXT,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [FromCurrency] INTEGER NOT NULL DEFAULT 0,
              [IsCheckFail] INTEGER NOT NULL DEFAULT 0,
              [ActiveID] TEXT,
              [IsDeleted] INTEGER NOT NULL DEFAULT 0,
              [IsProxyRecharge] INTEGER NOT NULL DEFAULT 0,
              [PayAccount] TEXT
            );
            CREATE TABLE IF NOT EXISTS [usr_EventLog] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [EventID] INTEGER NOT NULL,
              [Type] INTEGER NOT NULL,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [CreateAt] TEXT NOT NULL,
              [Reward] REAL NOT NULL DEFAULT 0,
              [Coin] INTEGER NOT NULL DEFAULT 0,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [ErrorMsg] TEXT,
              [IP] TEXT,
              [IPValue] INTEGER NOT NULL DEFAULT 0,
              [Put] INTEGER NOT NULL DEFAULT 0,
              [Flow] REAL NOT NULL DEFAULT 0,
              [ExpectedAt] TEXT,
              [ReleaseAt] TEXT,
              [SourceID] TEXT,
              [CheckMessage] TEXT,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [IsAuto] INTEGER NOT NULL DEFAULT 0,
              [ReviewerID] INTEGER NOT NULL DEFAULT 0
            );
            """);
        _uid = _db.Insertable(BuildUser("u_extra")).ExecuteReturnIdentity();
        _svc = new RechargePromoGrantService(new TestBizDb(_db));
    }

    /// <summary>
    /// 阶梯优惠取最高满足档，固定奖。
    /// </summary>
    [Fact]
    public async Task Grant_by_level_picks_highest_tier()
    {
        var eventId = InsertEvent(
            RechargePromoGrantService.EventTypeRechargeByLevel,
            "ReturnWay=Fixed&Payment=1&UserGroup=1&Put=Manual&IsAuto=Manual&DataSetting=" +
            Uri.EscapeDataString("""{"Items":[{"ID":1,"Name":"档1","needMoney":50,"Money":5,"WashFlow":2,"AwardPoint":0},{"ID":2,"Name":"档2","needMoney":100,"Money":15,"WashFlow":3,"AwardPoint":1}]}"""));

        InsertPaidOrder("RO-LVL-1", paid: 120m, activeId: eventId.ToString(), count: 2);
        Assert.Equal(1, await _svc.GrantForOrderAsync("RO-LVL-1"));

        var log = (await _db.Queryable<EventLog>().Where(e => e.SourceID == "RO-LVL-1").ToListAsync()).Single();
        Assert.Equal(RechargePromoGrantService.EventTypeRechargeByLevel, log.Type);
        Assert.Equal(15m, log.Reward);
        Assert.Equal(45m, log.Flow);
        Assert.Equal((short)1, log.Coin);
    }

    /// <summary>
    /// 首充 Count=1 写日志；SourceID 为日期-档-用户。
    /// </summary>
    [Fact]
    public async Task Grant_first_recharge_on_count_one()
    {
        var eventId = InsertEvent(
            RechargePromoGrantService.EventTypeFirstRecharge,
            "ReturnWay=Fixed&Payment=1&UserGroup=1&Put=Automatic&IsAuto=Automatic&DataSetting=" +
            Uri.EscapeDataString("""{"Items":[{"ID":3,"Name":"首档","needMoney":100,"Money":20,"IsFixedAmount":"0"}]}"""));

        InsertPaidOrder("RO-FR-1", paid: 100m, activeId: eventId.ToString(), count: 1);
        Assert.Equal(1, await _svc.GrantForOrderAsync("RO-FR-1"));

        var log = (await _db.Queryable<EventLog>().Where(e => e.EventID == eventId).ToListAsync()).Single();
        Assert.Equal(RechargePromoGrantService.EventTypeFirstRecharge, log.Type);
        Assert.Equal(20m, log.Reward);
        Assert.Contains($"-{_uid}", log.SourceID);
        Assert.StartsWith(DateTime.Now.ToString("yyyyMMdd"), log.SourceID);
    }

    /// <summary>
    /// 同订单可同时写充值优惠与阶梯两条（不同 EventID）。
    /// </summary>
    [Fact]
    public async Task Grant_recharge_and_level_both()
    {
        var rechargeId = InsertEvent(
            RechargePromoGrantService.EventTypeRecharge,
            "Reward=10&AmountWithdrawTimes=1&Payment=1&UserGroup=1&Put=Manual&FixedReward=0&MinMoney=0&Withdraw=AmountTimes");
        var levelId = InsertEvent(
            RechargePromoGrantService.EventTypeRechargeByLevel,
            "ReturnWay=Fixed&Payment=1&UserGroup=1&Put=Manual&DataSetting=" +
            Uri.EscapeDataString("""{"Items":[{"ID":1,"Name":"L","needMoney":10,"Money":3,"WashFlow":1,"AwardPoint":0}]}"""));

        InsertPaidOrder("RO-BOTH-1", paid: 100m, activeId: $"{rechargeId},{levelId}", count: 2);
        Assert.Equal(2, await _svc.GrantForOrderAsync("RO-BOTH-1"));
        Assert.Equal(2, await _db.Queryable<EventLog>().Where(e => e.SourceID == "RO-BOTH-1").CountAsync());
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private int InsertEvent(int type, string setting)
    {
        return _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "e" + type,
            SubTitle = "",
            Tag = "",
            Cover = "",
            ColID = 0,
            CreateAt = DateTime.UtcNow.AddDays(-1),
            StartAt = DateTime.UtcNow.AddDays(-1),
            ExpireAt = DateTime.UtcNow.AddDays(30),
            Description = "",
            Content = "",
            Member = 0,
            SettingString = setting,
            Type = type,
            IsVisible = true,
            IsOpen = true,
            IsLoginBeforeOpen = false,
            IsLoginAfterOpen = false,
            IsQuickEntrance = false,
            IsDetail = true,
            IsNewerTask = false,
            JumpType = 0,
            Status = 0,
            Views = "",
            Skin = "",
            Sort = 0,
            Currency = 1,
            SupportCurrency = "",
            FilterGame = "",
            FilterPlatform = "",
            Demo = "",
            Background = ""
        }).ExecuteReturnIdentity();
    }

    private void InsertPaidOrder(string id, decimal paid, string activeId, int count)
    {
        _db.Insertable(new RechargeOrder
        {
            ID = id,
            SiteID = 1001,
            UserID = _uid,
            Money = paid,
            Fee = 0,
            Cost = 0,
            CreateAt = DateTime.UtcNow,
            PayID = 1,
            IsPay = true,
            PayAt = DateTime.UtcNow,
            Paid = paid,
            SystemID = "GW",
            AdminID = 1,
            Count = count,
            Remark = "",
            IP = "1.1.1.1",
            Currency = 1,
            FromCurrency = 1,
            IsCheckFail = false,
            ActiveID = activeId,
            IsDeleted = false,
            IsProxyRecharge = false,
            PayAccount = ""
        }).ExecuteCommand();
    }

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
        GroupID = 1,
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
}

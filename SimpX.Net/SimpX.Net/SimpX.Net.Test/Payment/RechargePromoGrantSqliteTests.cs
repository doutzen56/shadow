using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 充值活动最小发奖：按 ActiveID 写 usr_EventLog。
/// </summary>
public sealed class RechargePromoGrantSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly int _uid;
    private readonly int _eventId;
    private readonly RechargePromoGrantService _svc;

    /// <summary>
    /// 建订单、活动与用户。
    /// </summary>
    public RechargePromoGrantSqliteTests()
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

        _uid = _db.Insertable(BuildUser("u_promo")).ExecuteReturnIdentity();
        _eventId = _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "充值送",
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
            SettingString = "Reward=10&AmountWithdrawTimes=3&Payment=1&UserGroup=1&Put=Manual&IsAuto=Manual&MinMoney=0&FixedReward=0&Withdraw=AmountTimes",
            Type = RechargePromoGrantService.EventTypeRecharge,
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

        _db.Insertable(new RechargeOrder
        {
            ID = "RO-PROMO-1",
            SiteID = 1001,
            UserID = _uid,
            Money = 100m,
            Fee = 0,
            Cost = 0,
            CreateAt = DateTime.UtcNow,
            PayID = 1,
            IsPay = true,
            PayAt = DateTime.UtcNow,
            Paid = 100m,
            SystemID = "GW-P",
            AdminID = 1,
            Count = 1,
            Remark = "",
            IP = "2.2.2.2",
            Currency = 1,
            FromCurrency = 1,
            IsCheckFail = false,
            ActiveID = _eventId.ToString(),
            IsDeleted = false,
            IsProxyRecharge = false,
            PayAccount = ""
        }).ExecuteCommand();

        _svc = new RechargePromoGrantService(new TestBizDb(_db));
    }

    /// <summary>
    /// 首次按订单写入 10% 奖金日志；重复调用不重复写。
    /// </summary>
    [Fact]
    public async Task Grant_writes_event_log_once()
    {
        var n1 = await _svc.GrantForOrderAsync("RO-PROMO-1");
        Assert.Equal(1, n1);

        var logs = await _db.Queryable<EventLog>().Where(e => e.SourceID == "RO-PROMO-1").ToListAsync();
        Assert.Single(logs);
        Assert.Equal(10m, logs[0].Reward);
        Assert.Equal(30m, logs[0].Flow);
        Assert.Equal(_eventId, logs[0].EventID);
        Assert.Equal(RechargePromoGrantService.StatusNone, logs[0].Status);

        var n2 = await _svc.GrantForOrderAsync("RO-PROMO-1");
        Assert.Equal(0, n2);
        Assert.Equal(1, await _db.Queryable<EventLog>().Where(e => e.SourceID == "RO-PROMO-1").CountAsync());
    }

    /// <summary>
    /// ActiveID=0 不写日志。
    /// </summary>
    [Fact]
    public async Task Grant_skips_when_active_zero()
    {
        _db.Updateable<RechargeOrder>()
            .SetColumns(o => o.ActiveID == "0")
            .Where(o => o.ID == "RO-PROMO-1")
            .ExecuteCommand();
        Assert.Equal(0, await _svc.GrantForOrderAsync("RO-PROMO-1"));
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

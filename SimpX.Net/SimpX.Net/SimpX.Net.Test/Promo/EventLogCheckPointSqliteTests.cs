using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 活动审过发放积分。
/// </summary>
public sealed class EventLogCheckPointSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly EventLogCheckService _svc;
    private readonly int _uid;
    private readonly int _eventId;

    /// <summary>
    /// 建用户、活动、积分表。
    /// </summary>
    public EventLogCheckPointSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site", "site_Event"], []);
        _db.Ado.ExecuteCommand(
            """
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
            CREATE TABLE IF NOT EXISTS [usr_PointLog] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Point] REAL NOT NULL,
              [Balance] REAL NOT NULL,
              [Type] INTEGER NOT NULL,
              [EventType] INTEGER NOT NULL DEFAULT 0,
              [Content] TEXT,
              [AContent] TEXT,
              [BContent] TEXT,
              [CreateAt] TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS [usr_PointLock] (
              [LockID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [SourceID] INTEGER NOT NULL DEFAULT 0,
              [Money] REAL NOT NULL DEFAULT 0,
              [IsLock] INTEGER NOT NULL DEFAULT 0,
              [CreateAt] TEXT,
              [UnLockAt] TEXT,
              [LockDesc] TEXT,
              [UnLockDesc] TEXT
            );
            CREATE TABLE IF NOT EXISTS [usr_Message] (
              [MsgID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [SendID] INTEGER NOT NULL DEFAULT 0,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [CreateAt] TEXT NOT NULL,
              [ReadAt] TEXT,
              [Title] TEXT,
              [Content] TEXT,
              [IsRead] INTEGER NOT NULL DEFAULT 0,
              [SupportCurrency] TEXT
            );
            """);

        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "pt",
            CreateAt = DateTime.UtcNow,
            NotifyAt = DateTime.UtcNow,
            Status = 0,
            Currency = 1,
            Wallet = 0,
            APIKey = Guid.NewGuid(),
            SettingString = "",
            APPSettingString = "",
            PCSettingString = "",
            AdminDomain = "",
            HomeTemplateName = ""
        }).ExecuteCommand();

        _uid = _db.Insertable(BuildUser("u_pt", coin: 5)).ExecuteReturnIdentity();
        _eventId = _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "积分活动",
            SubTitle = "",
            Tag = "",
            Cover = "",
            ColID = 0,
            CreateAt = DateTime.UtcNow,
            StartAt = DateTime.UtcNow.AddDays(-1),
            ExpireAt = DateTime.UtcNow.AddDays(10),
            Description = "",
            Content = "",
            Member = 0,
            SettingString = "",
            Type = 20,
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

        var points = new UserPointService(new TestBizDb(_db));
        var messages = new UserMessageNotifyService(new TestBizDb(_db));
        _svc = new EventLogCheckService(new TestBizDb(_db), _money, messages, points);
    }

    /// <summary>
    /// 审过：奖金入账 + 积分累加 + 站内信含积分。
    /// </summary>
    [Fact]
    public async Task Check_success_grants_coin_points()
    {
        var logId = _db.Insertable(new EventLog
        {
            EventID = _eventId,
            Type = 20,
            SiteID = 1001,
            UserID = _uid,
            CreateAt = DateTime.Now.AddMinutes(-1),
            Reward = 10,
            Coin = 3,
            Status = 0,
            ErrorMsg = "",
            IP = "",
            Put = 0,
            Flow = 10,
            ExpectedAt = DateTime.Now,
            ReleaseAt = DateTime.MinValue,
            SourceID = "RO-PT-1",
            CheckMessage = "",
            Currency = 1,
            IsAuto = 0
        }).ExecuteReturnIdentity();

        await _svc.CheckAsync(logId, EventLogCheckService.StatusSuccess, reviewerId: 1);

        Assert.Single(_money.Logs);
        Assert.Equal(10m, _money.Logs[0].Money);
        Assert.Equal(220, _money.Logs[0].Type); // 200+20

        var user = await _db.Queryable<User>().InSingleAsync(_uid);
        Assert.Equal(8m, user.Coin);

        var pl = await _db.Queryable<UserPointLog>().Where(p => p.UserID == _uid).ToListAsync();
        Assert.Single(pl);
        Assert.Equal(3m, pl[0].Point);
        Assert.Equal(UserPointService.PointTypeEvent, pl[0].Type);

        var msg = (await _db.Queryable<UserMessage>().Where(m => m.UserID == _uid).ToListAsync()).Single();
        Assert.Contains("#积分#", msg.Content);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private static User BuildUser(string name, decimal coin) => new()
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
        Coin = coin,
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

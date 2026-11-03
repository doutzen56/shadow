using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 活动日志审过加款与自动审（内存 SQLite + 假账本）。
/// </summary>
public sealed class EventLogCheckSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly EventLogCheckService _svc;
    private readonly int _uid;
    private readonly int _eventId;

    /// <summary>
    /// 建用户、站点活动、待审日志表。
    /// </summary>
    public EventLogCheckSqliteTests()
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

        EnsureSite();
        _uid = _db.Insertable(BuildUser("u_evchk")).ExecuteReturnIdentity();
        _eventId = _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "充值送测",
            SubTitle = "",
            Tag = "",
            Cover = "",
            ColID = 0,
            CreateAt = DateTime.UtcNow.AddDays(-1),
            StartAt = DateTime.UtcNow.AddDays(-1),
            ExpireAt = DateTime.UtcNow.AddDays(10),
            Description = "",
            Content = "",
            Member = 0,
            SettingString = "",
            Type = 9,
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

        var messages = new UserMessageNotifyService(new TestBizDb(_db));
        _svc = new EventLogCheckService(new TestBizDb(_db), _money, messages);
    }

    /// <summary>
    /// 通过审核：账变类型 209，流水倍率=Flow/Reward，写站内信。
    /// </summary>
    [Fact]
    public async Task Check_success_credits_money_type_209()
    {
        var logId = InsertPendingLog(reward: 10m, flow: 30m, put: 0, isAuto: 0, createAt: DateTime.Now.AddMinutes(-1));

        await _svc.CheckAsync(logId, EventLogCheckService.StatusSuccess, reviewerId: 9);

        Assert.Single(_money.Logs);
        Assert.Equal(10m, _money.Logs[0].Money);
        Assert.Equal(209, _money.Logs[0].Type);
        Assert.Equal(3m, _money.Logs[0].FlowRate);
        Assert.Equal(_eventId, _money.Logs[0].EventId);

        var log = await _db.Queryable<EventLog>().InSingleAsync(logId);
        Assert.Equal(EventLogCheckService.StatusSuccess, log.Status);
        Assert.Equal(9, log.ReviewerID);

        var msgs = await _db.Queryable<UserMessage>().Where(m => m.UserID == _uid).ToListAsync();
        Assert.Single(msgs);
        Assert.Contains("#到帐#", msgs[0].Content);
    }

    /// <summary>
    /// 拒绝：不加款，写失败站内信。
    /// </summary>
    [Fact]
    public async Task Check_reject_no_money()
    {
        var logId = InsertPendingLog(reward: 10m, flow: 0, put: 0, isAuto: 0, createAt: DateTime.Now.AddMinutes(-1));
        await _svc.CheckAsync(logId, EventLogCheckService.StatusFailed, checkMessage: "不符", reviewerId: 1);
        Assert.Empty(_money.Logs);
        var msgs = await _db.Queryable<UserMessage>().Where(m => m.UserID == _uid).ToListAsync();
        Assert.Single(msgs);
        Assert.Contains("#失败#", msgs[0].Content);
    }

    /// <summary>
    /// 重复审核拒绝。
    /// </summary>
    [Fact]
    public async Task Check_twice_rejects()
    {
        var logId = InsertPendingLog(reward: 5m, flow: 5m, put: 0, isAuto: 0, createAt: DateTime.Now.AddMinutes(-1));
        await _svc.CheckAsync(logId, EventLogCheckService.StatusSuccess, reviewerId: 1);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CheckAsync(logId, EventLogCheckService.StatusSuccess, reviewerId: 1));
        Assert.Single(_money.Logs);
    }

    /// <summary>
    /// 自动审：Put/IsAuto=自动且创建超过 10 秒才发放。
    /// </summary>
    [Fact]
    public async Task AutoCheck_only_due_automatic()
    {
        var dueId = InsertPendingLog(
            reward: 8m,
            flow: 8m,
            put: EventLogCheckService.PutAutomatic,
            isAuto: EventLogCheckService.PutAutomatic,
            createAt: DateTime.Now.AddSeconds(-30));
        InsertPendingLog(
            reward: 7m,
            flow: 7m,
            put: EventLogCheckService.PutAutomatic,
            isAuto: EventLogCheckService.PutAutomatic,
            createAt: DateTime.Now); // 太新，不审
        InsertPendingLog(
            reward: 6m,
            flow: 6m,
            put: 0,
            isAuto: 0,
            createAt: DateTime.Now.AddMinutes(-1)); // 手动，不自动审

        var n = await _svc.AutoCheckDueAsync(50);
        Assert.Equal(1, n);
        Assert.Single(_money.Logs);
        Assert.Equal(8m, _money.Logs[0].Money);

        var due = await _db.Queryable<EventLog>().InSingleAsync(dueId);
        Assert.Equal(EventLogCheckService.StatusSuccess, due.Status);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private int InsertPendingLog(decimal reward, decimal flow, int put, int isAuto, DateTime createAt)
    {
        return _db.Insertable(new EventLog
        {
            EventID = _eventId,
            Type = 9,
            SiteID = 1001,
            UserID = _uid,
            CreateAt = createAt,
            Reward = reward,
            Coin = 0,
            Status = EventLogCheckService.StatusNone,
            ErrorMsg = "",
            IP = "1.1.1.1",
            IPValue = 0,
            Put = put,
            Flow = flow,
            ExpectedAt = createAt.AddSeconds(-1),
            ReleaseAt = DateTime.MinValue,
            SourceID = "RO-X-" + Guid.NewGuid().ToString("N")[..8],
            CheckMessage = "",
            Currency = 1,
            IsAuto = isAuto,
            ReviewerID = 0
        }).ExecuteReturnIdentity();
    }

    private void EnsureSite()
    {
        if (_db.Queryable<Site>().Any(s => s.ID == 1001))
            return;
        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "evchk",
            CreateAt = DateTime.UtcNow,
            NotifyAt = DateTime.UtcNow,
            Status = 0,
            Currency = 1,
            Wallet = 0,
            IsCurrencySelect = false,
            APIKey = Guid.NewGuid(),
            SettingString = "",
            APPSettingString = "",
            PCSettingString = "",
            GameCredit = 0,
            NotifyCredit = 0,
            AdminDomain = "",
            HomeTemplateName = ""
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

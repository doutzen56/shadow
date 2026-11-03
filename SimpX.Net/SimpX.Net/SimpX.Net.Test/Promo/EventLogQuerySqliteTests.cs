using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 活动日志列表查询。
/// </summary>
public sealed class EventLogQuerySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly EventLogService _svc;
    private readonly int _uid;
    private readonly int _eventId;

    /// <summary>
    /// 建待审日志。
    /// </summary>
    public EventLogQuerySqliteTests()
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
            """);

        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "q",
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

        _uid = _db.Insertable(BuildUser("u_q")).ExecuteReturnIdentity();
        _eventId = _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "列表活动",
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

        _db.Insertable(new EventLog
        {
            EventID = _eventId,
            Type = 9,
            SiteID = 1001,
            UserID = _uid,
            CreateAt = DateTime.Now.AddMinutes(-5),
            Reward = 10,
            Status = 0,
            ErrorMsg = "",
            IP = "",
            Put = 0,
            Flow = 10,
            ExpectedAt = DateTime.Now,
            ReleaseAt = DateTime.MinValue,
            SourceID = "RO-Q-1",
            CheckMessage = "",
            Currency = 1,
            IsAuto = 0
        }).ExecuteCommand();

        _svc = new EventLogService(new TestBizDb(_db), new FixedSiteAccessor(1001, "q.test"));
    }

    /// <summary>
    /// 按状态筛待审，带出用户名与活动标题。
    /// </summary>
    [Fact]
    public async Task List_pending_includes_names()
    {
        var rows = await _svc.ListAsync(new EventLogListInput { Status = "0" });
        Assert.Single(rows);
        Assert.Equal("u_q", rows[0].UserName);
        Assert.Equal("列表活动", rows[0].EventTitle);
        Assert.Equal("RO-Q-1", rows[0].SourceID);
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

    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("no site");
    }
}

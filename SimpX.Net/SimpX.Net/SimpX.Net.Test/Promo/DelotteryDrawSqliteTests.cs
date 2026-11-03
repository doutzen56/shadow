using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 转盘开奖（内存 SQLite；随机源固定）。
/// </summary>
public sealed class DelotteryDrawSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly DelotteryDrawService _svc;
    private readonly int _userId;
    private readonly int _eventId;

    /// <summary>
    /// 建站、活动、会员，并预存 2 次抽奖机会。
    /// </summary>
    public DelotteryDrawSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(
            ["Users", "Site", "site_Event", "usr_EventLog", "usr_DelotteryLog"],
            []);
        EnsureSite();
        _userId = _db.Insertable(BuildUser()).ExecuteReturnIdentity();
        _eventId = EnsureEvent();
        _db.Insertable(new UserDelotteryLog
        {
            SiteID = 1001,
            UserID = _userId,
            Point = 2,
            Balance = 2,
            Type = AutoEventStatsService.DelotteryTypeBet,
            Content = "seed",
            CreateAt = DateTime.UtcNow
        }).ExecuteCommand();
        // 随机恒取 0 → 袋中第一项（Rate 堆出来的现金奖）
        var biz = new TestBizDb(_db);
        _svc = new DelotteryDrawService(biz, new UserActivityLockService(biz), _ => 0);
    }

    /// <summary>
    /// 有次数可抽现金，写待审并扣次。
    /// </summary>
    [Fact]
    public async Task Draw_money_writes_pending_and_consumes_count()
    {
        var now = new DateTime(2026, 6, 2, 15, 0, 0);
        var result = await _svc.DrawAsync(1001, _userId, _eventId, now: now);
        Assert.True(result.Success);
        Assert.Equal(DelotteryDrawService.PrizeMoney, result.ItemType);
        Assert.Equal(8m, result.Reward);
        Assert.Equal(1m, result.Balance);
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.UserID == _userId && l.Type == DelotteryDrawService.EventTypeDelottery)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(8m, logs[0].Reward);
    }

    /// <summary>
    /// 次数用尽失败。
    /// </summary>
    [Fact]
    public async Task Draw_fails_when_no_balance()
    {
        var now = new DateTime(2026, 6, 2, 15, 0, 0);
        Assert.True((await _svc.DrawAsync(1001, _userId, _eventId, now: now)).Success);
        Assert.True((await _svc.DrawAsync(1001, _userId, _eventId, now: now)).Success);
        var fail = await _svc.DrawAsync(1001, _userId, _eventId, now: now);
        Assert.False(fail.Success);
        Assert.Contains("用完", fail.ErrorMessage);
    }

    /// <summary>
    /// 清理。
    /// </summary>
    public void Dispose() => _db?.Dispose();

    private void EnsureSite()
    {
        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "dl",
            CreateAt = DateTime.UtcNow,
            NotifyAt = DateTime.UtcNow,
            Status = 0,
            Currency = 1,
            Wallet = 1,
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

    private int EnsureEvent() =>
        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "转盘",
            SubTitle = "",
            Tag = "",
            Cover = "",
            ColID = 0,
            CreateAt = new DateTime(2026, 1, 1),
            StartAt = new DateTime(2026, 1, 1),
            ExpireAt = new DateTime(2027, 1, 1),
            Description = "",
            Content = "",
            Member = 0,
            SettingString =
                """{"WashFlow":1,"Cash":0.7,"UserGroup":[1],"DataSetting":"{\"Items\":[{\"ID\":1,\"Name\":\"现金8\",\"Type\":1,\"Value\":\"8\",\"Rate\":10,\"Count\":-1},{\"ID\":2,\"Name\":\"再来一次\",\"Type\":0,\"Value\":\"0\",\"Rate\":1,\"Count\":-1}]}"}""",
            Type = DelotteryDrawService.EventTypeDelottery,
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
            Demo = ""
        }).ExecuteReturnIdentity();

    private static User BuildUser() => new()
    {
        SiteID = 1001,
        UserName = "draw",
        NickName = "draw",
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
        AgentID = 9,
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

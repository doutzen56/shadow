using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 生日/投注定时积分（内存 SQLite）。
/// </summary>
public sealed class ScheduledPointSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly ScheduledPointService _svc;
    private readonly int _uid;

    /// <summary>
    /// 建站、积分项、生日会员与昨日投注。
    /// </summary>
    public ScheduledPointSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(
            ["Users", "Site", "site_VipConfig", "usr_PointLog", "usr_PointLock", "rpt_UserDateGame"],
            []);
        EnsureSite();
        EnsurePointConfig();
        _uid = _db.Insertable(BuildUser("u_pt", birthday: new DateTime(1991, 5, 20), lockFlags: 0))
            .ExecuteReturnIdentity();
        _svc = new ScheduledPointService(new TestBizDb(_db), new UserPointService(new TestBizDb(_db)));
    }

    /// <summary>
    /// 生日固定分：当天发一次，幂等。
    /// </summary>
    [Fact]
    public async Task Birthday_fixed_once_a_day()
    {
        var now = new DateTime(2026, 5, 20, 9, 0, 0);
        Assert.Equal(1, await _svc.GrantBirthdayPointsAsync(now: now));
        var user = await _db.Queryable<User>().InSingleAsync(_uid);
        Assert.Equal(8m, user.Coin);
        Assert.Equal(0, await _svc.GrantBirthdayPointsAsync(now: now));
        Assert.Equal(8m, (await _db.Queryable<User>().InSingleAsync(_uid)).Coin);
    }

    /// <summary>
    /// 投注按比例：昨日流水 × 比例。
    /// </summary>
    [Fact]
    public async Task Betting_rate_from_yesterday()
    {
        var now = new DateTime(2026, 5, 21, 19, 0, 0);
        _db.Insertable(new UserDateGame
        {
            SiteID = 1001,
            UserID = _uid,
            Date = now.Date.AddDays(-1),
            Type = 1,
            Category = 0,
            BetMoney = 1000,
            Money = 0,
            Currency = 1
        }).ExecuteCommand();

        Assert.Equal(1, await _svc.GrantBettingPointsAsync(now: now));
        var user = await _db.Queryable<User>().InSingleAsync(_uid);
        Assert.Equal(10m, user.Coin); // 1000 * 0.01
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
            Name = "pt",
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

    private void EnsurePointConfig()
    {
        const string xml = """
            <root>
              <item Name="Birthday" Status="Available" Method="OnceADay" Type="Fixed" Description="生日积分" Value="8" />
              <item Name="Betting" Status="Available" Method="Each" Type="Rate" Description="投注积分" Value="0.01" />
            </root>
            """;
        _db.Insertable(new SiteVipConfig
        {
            ID = 1001,
            CreateAt = DateTime.UtcNow,
            VIPConditionSetting = "",
            VIPRightSetting = "",
            PointItemSettingString = xml,
            ExperItemSettingString = "",
            RuleClause = "",
            PointRule = "",
            AutoReturnRule = "",
            Currency = 1
        }).ExecuteCommand();
    }

    private static User BuildUser(string name, DateTime birthday, int lockFlags) => new()
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
        Lock = lockFlags,
        Function = 0,
        LoginAt = DateTime.UtcNow,
        LoginIP = "",
        AgentID = 1,
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
        Birthday = birthday,
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

using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// VIP 周/月/生日礼金（内存 SQLite + 假账本；注入固定时钟）。
/// </summary>
public sealed class VipPeriodicGiftSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly VipPeriodicGiftService _svc;
    private readonly int _uid;

    /// <summary>
    /// 建站、VIP 特权、会员与上周/上月统计。
    /// </summary>
    public VipPeriodicGiftSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(
            [
                "Users", "Site", "site_VipConfig", "usr_VipGiftLog",
                "rpt_UserDateMoney", "rpt_UserDateGame", "usr_RechargeOrder"
            ],
            []);

        EnsureSite();
        EnsureVipConfig();
        _uid = _db.Insertable(BuildUser("u_gift", level: 1, birthday: new DateTime(1990, 3, 15))).ExecuteReturnIdentity();
        _svc = new VipPeriodicGiftService(new TestBizDb(_db), _money);
    }

    /// <summary>
    /// 周一发放窗口内：上周充值/流水达标发每周礼金，Mark 幂等。
    /// </summary>
    [Fact]
    public async Task GrantEveryWeek_in_window_pays_once()
    {
        // 2026-03-16 周一 10:05；上周窗口 2026-03-09 ~ 2026-03-16
        var now = new DateTime(2026, 3, 16, 10, 5, 0);
        var weekStart = VipPeriodicGiftService.FirstDayOfWeek(now.Date);
        var lastWeekStart = weekStart.AddDays(-7);
        SeedPeriodStats(_uid, lastWeekStart, recharge: 200, bet: 80, orderCount: 2);

        var paid = await _svc.GrantEveryWeekAsync(now: now);
        Assert.Equal(1, paid);
        Assert.Single(_money.Logs);
        Assert.Equal(20m, _money.Logs[0].Money);
        Assert.Equal(MoneyTypePrivilegeBase(VipPeriodicGiftService.PrivilegeEveryWeek), _money.Logs[0].Type);

        var mark = now.ToString("yyyyMM") + VipPeriodicGiftService.GetWeekOfMYear(now);
        var gifts = await _db.Queryable<UserVipGiftLog>()
            .Where(l => l.UserID == _uid && l.Type == VipPeriodicGiftService.PrivilegeEveryWeek)
            .ToListAsync();
        Assert.Single(gifts);
        Assert.Equal(mark, gifts[0].Mark);

        Assert.Equal(0, await _svc.GrantEveryWeekAsync(now: now));
        Assert.Single(_money.Logs);
    }

    /// <summary>
    /// 非发放窗口：不发。
    /// </summary>
    [Fact]
    public async Task GrantEveryWeek_outside_window_skips()
    {
        var now = new DateTime(2026, 3, 16, 12, 0, 0); // 超过 10 点一小时
        var weekStart = VipPeriodicGiftService.FirstDayOfWeek(now.Date);
        SeedPeriodStats(_uid, weekStart.AddDays(-7), recharge: 200, bet: 80, orderCount: 1);
        Assert.Equal(0, await _svc.GrantEveryWeekAsync(now: now));
        Assert.Empty(_money.Logs);
    }

    /// <summary>
    /// 月礼发放日窗口：上月门槛达标发放。
    /// </summary>
    [Fact]
    public async Task GrantEveryMonth_in_window_pays()
    {
        // 2026-04-05 10:10；上月 2026-03-01 ~ 2026-04-01
        var now = new DateTime(2026, 4, 5, 10, 10, 0);
        SeedPeriodStats(_uid, new DateTime(2026, 3, 10), recharge: 200, bet: 80, orderCount: 3);

        var paid = await _svc.GrantEveryMonthAsync(now: now);
        Assert.Equal(1, paid);
        Assert.Equal(30m, _money.Logs[0].Money);
        Assert.Equal(MoneyTypePrivilegeBase(VipPeriodicGiftService.PrivilegeEveryMonth), _money.Logs[0].Type);
    }

    /// <summary>
    /// 生日当天发礼金；活动锁跳过。
    /// </summary>
    [Fact]
    public async Task GrantBirthday_pays_and_skips_activity_lock()
    {
        var now = new DateTime(2026, 3, 15, 8, 0, 0);
        var paid = await _svc.GrantBirthdayAsync(now: now);
        Assert.Equal(1, paid);
        Assert.Equal(15m, _money.Logs[0].Money);

        var lockedId = _db.Insertable(BuildUser(
            "u_bd_lock",
            level: 1,
            birthday: new DateTime(2000, 3, 15),
            lockFlags: VipPeriodicGiftService.LockActivity)).ExecuteReturnIdentity();
        // 主用户 Mark 幂等；活动锁用户跳过
        Assert.Equal(0, await _svc.GrantBirthdayAsync(now: now));
        Assert.Empty(await _db.Queryable<UserVipGiftLog>()
            .Where(l => l.UserID == lockedId)
            .ToListAsync());
        Assert.Single(_money.Logs);
    }

    /// <summary>
    /// 清理内存库。
    /// </summary>
    public void Dispose() => _db?.Dispose();

    private static int MoneyTypePrivilegeBase(int privilegeId) =>
        VipPeriodicGiftService.MoneyTypePrivilegeBase + privilegeId;

    private void EnsureSite()
    {
        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "gift",
            CreateAt = DateTime.UtcNow,
            NotifyAt = DateTime.UtcNow,
            Status = 0,
            Currency = 1,
            Wallet = VipPeriodicGiftService.WalletSingle,
            IsCurrencySelect = false,
            APIKey = Guid.NewGuid(),
            SettingString = "IsEnabledVIP=true",
            APPSettingString = "",
            PCSettingString = "",
            GameCredit = 0,
            NotifyCredit = 0,
            AdminDomain = "",
            HomeTemplateName = ""
        }).ExecuteCommand();
    }

    private void EnsureVipConfig()
    {
        // 周：周一(1) 10 点；月：每月 5 号 10 点，至少 1 笔
        const string rights = """
            <root>
              <item ID="8" Enabled="true" Name="EveryWeek" Ratio="1" L1="20|100|50"><![CDATA[Day=1&Hour=10&RechargeOrderCount=0]]></item>
              <item ID="3" Enabled="true" Name="EveryMonth" Ratio="1" L1="30|100|50"><![CDATA[Day=5&Hour=10&RechargeOrderCount=1]]></item>
              <item ID="4" Enabled="true" Name="Birthday" Ratio="1" L1="15"><![CDATA[]]></item>
            </root>
            """;
        _db.Insertable(new SiteVipConfig
        {
            ID = 1001,
            CreateAt = DateTime.UtcNow,
            VIPConditionSetting = "<root></root>",
            VIPRightSetting = rights,
            PointItemSettingString = "",
            ExperItemSettingString = "",
            RuleClause = "",
            PointRule = "",
            AutoReturnRule = "",
            Currency = 1
        }).ExecuteCommand();
    }

    private void SeedPeriodStats(int userId, DateTime day, decimal recharge, decimal bet, int orderCount)
    {
        _db.Insertable(new UserDateMoney
        {
            SiteID = 1001,
            UserID = userId,
            Date = day.Date,
            Type = VipPeriodicGiftService.MoneyTypeRecharge,
            Money = recharge,
            Count = orderCount,
            Currency = 1
        }).ExecuteCommand();
        _db.Insertable(new UserDateGame
        {
            SiteID = 1001,
            UserID = userId,
            Date = day.Date,
            Type = 1,
            Category = 0,
            BetMoney = bet,
            Money = 0,
            Currency = 1
        }).ExecuteCommand();
    }

    private static User BuildUser(string name, byte level, DateTime birthday, int lockFlags = 0) => new()
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
        Level = level,
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

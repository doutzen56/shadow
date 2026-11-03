using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// VIP ExeUpgrade：条件升级、升级锁跳过、晋级礼金幂等（内存 SQLite + 假账本）。
/// </summary>
public sealed class VipUpgradeSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly VipUpgradeService _svc;
    private readonly int _uid;

    /// <summary>
    /// 建站、VIP 配置、会员与累计存款。
    /// </summary>
    public VipUpgradeSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(
            ["Users", "Site", "site_VipConfig", "site_VipUpGradeLog", "usr_VipGiftLog", "rpt_UserDateMoney", "rpt_UserDateGame"],
            []);

        EnsureSite(enabledVip: true);
        EnsureVipConfig();
        _uid = _db.Insertable(BuildUser("u_vip", level: 0, initLevel: 0, lockFlags: 0)).ExecuteReturnIdentity();
        SeedRecharge(_uid, 1000m);
        _svc = new VipUpgradeService(new TestBizDb(_db), _money);
    }

    /// <summary>
    /// 存款达标：Level 升到 1，写升级日志并发晋级礼金。
    /// </summary>
    [Fact]
    public async Task TryUpgrade_recharge_meets_level1_and_sends_gift()
    {
        var ok = await _svc.TryUpgradeAsync(_uid);
        Assert.True(ok);

        var user = await _db.Queryable<User>().InSingleAsync(_uid);
        Assert.Equal((byte)1, user.Level);

        var logs = await _db.Queryable<SiteVipUpGradeLog>().Where(l => l.UserID == _uid).ToListAsync();
        Assert.Single(logs);
        Assert.Equal(0, logs[0].OldLevel);
        Assert.Equal(1, logs[0].NewLevel);

        Assert.Single(_money.Logs);
        Assert.Equal(50m, _money.Logs[0].Money);
        Assert.Equal(VipUpgradeService.MoneyTypeUpgradeGift, _money.Logs[0].Type);

        var gifts = await _db.Queryable<UserVipGiftLog>().Where(l => l.UserID == _uid).ToListAsync();
        Assert.Single(gifts);
        Assert.Equal("1", gifts[0].Mark);
    }

    /// <summary>
    /// 升级锁：不改等级、不加礼金。
    /// </summary>
    [Fact]
    public async Task TryUpgrade_upgrade_lock_skips()
    {
        var lockedId = _db.Insertable(BuildUser("u_lock", level: 0, initLevel: 0, lockFlags: VipUpgradeService.LockUpgrade))
            .ExecuteReturnIdentity();
        SeedRecharge(lockedId, 1000m);

        var before = _money.Logs.Count;
        var ok = await _svc.TryUpgradeAsync(lockedId);
        Assert.False(ok);
        var user = await _db.Queryable<User>().InSingleAsync(lockedId);
        Assert.Equal((byte)0, user.Level);
        Assert.Equal(before, _money.Logs.Count);
        Assert.Empty(await _db.Queryable<SiteVipUpGradeLog>().Where(l => l.UserID == lockedId).ToListAsync());
    }

    /// <summary>
    /// 未开 VIP：跳过。
    /// </summary>
    [Fact]
    public async Task TryUpgrade_vip_disabled_skips()
    {
        _db.Updateable<Site>()
            .SetColumns(s => s.SettingString == "IsEnabledVIP=false")
            .Where(s => s.ID == 1001)
            .ExecuteCommand();

        var ok = await _svc.TryUpgradeAsync(_uid);
        Assert.False(ok);
        var user = await _db.Queryable<User>().InSingleAsync(_uid);
        Assert.Equal((byte)0, user.Level);
    }

    /// <summary>
    /// 礼金幂等：同等级 Mark 已存在则不再加款。
    /// </summary>
    [Fact]
    public async Task TryUpgrade_gift_idempotent_by_mark()
    {
        // 先升到 1
        Assert.True(await _svc.TryUpgradeAsync(_uid));
        Assert.Single(_money.Logs);

        // 再塞一笔存款够升 2，但先把等级手工拉回 0 并清升级日志，只测礼金 Mark 挡 1 档重复
        _db.Updateable<User>().SetColumns(u => u.Level == (byte)0).Where(u => u.ID == _uid).ExecuteCommand();
        _db.Deleteable<SiteVipUpGradeLog>().Where(l => l.UserID == _uid).ExecuteCommand();
        // Mark=1 仍在；再升级到 1 时应升级成功但礼金因 Mark 跳过
        Assert.True(await _svc.TryUpgradeAsync(_uid));
        Assert.Single(_money.Logs);
    }

    /// <summary>
    /// 清理内存库连接。
    /// </summary>
    public void Dispose() => _db?.Dispose();

    private void EnsureSite(bool enabledVip)
    {
        if (_db.Queryable<Site>().Any(s => s.ID == 1001))
            return;
        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "vip",
            CreateAt = DateTime.UtcNow,
            NotifyAt = DateTime.UtcNow,
            Status = 0,
            Currency = 1,
            Wallet = VipUpgradeService.WalletSingle,
            IsCurrencySelect = false,
            APIKey = Guid.NewGuid(),
            SettingString = enabledVip ? "IsEnabledVIP=true" : "IsEnabledVIP=false",
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
        const string condition = """
            <root>
              <item Level="0" Name="V0" UpgradeFlag="false" UpgradeClose="false" Upgrade3="0" Upgrade4="0" Hold2="0" />
              <item Level="1" Name="V1" UpgradeFlag="false" UpgradeClose="false" Upgrade3="500" Upgrade4="0" Hold2="0" />
              <item Level="2" Name="V2" UpgradeFlag="false" UpgradeClose="false" Upgrade3="5000" Upgrade4="0" Hold2="0" />
            </root>
            """;
        const string rights = """
            <root>
              <item ID="2" Enabled="true" Name="Upgrade" Ratio="1" L0="0" L1="50" L2="100" />
            </root>
            """;
        if (_db.Queryable<SiteVipConfig>().Any(c => c.ID == 1001))
            return;
        _db.Insertable(new SiteVipConfig
        {
            ID = 1001,
            CreateAt = DateTime.UtcNow,
            VIPConditionSetting = condition,
            VIPRightSetting = rights,
            PointItemSettingString = "",
            ExperItemSettingString = "",
            RuleClause = "",
            PointRule = "",
            AutoReturnRule = "",
            Currency = 1
        }).ExecuteCommand();
    }

    private void SeedRecharge(int userId, decimal money)
    {
        _db.Insertable(new UserDateMoney
        {
            SiteID = 1001,
            UserID = userId,
            Date = DateTime.Today,
            Type = VipUpgradeService.MoneyTypeRecharge,
            Money = money,
            Count = 1,
            Currency = 1
        }).ExecuteCommand();
    }

    private static User BuildUser(string name, byte level, byte initLevel, int lockFlags) => new()
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
        Birthday = DateTime.UtcNow,
        Telegram = "",
        Line = "",
        Google = "",
        Info = "",
        DeviceId = "",
        RegisteredFrom = "",
        PixelID = "",
        InitLevel = initLevel,
        Is_Temporary_Password = false,
        BlockGame = "",
        OldGroupID = 0
    };

}

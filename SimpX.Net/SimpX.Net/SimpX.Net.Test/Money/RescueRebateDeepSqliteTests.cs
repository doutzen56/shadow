using System.Text.Json;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 救援金预览扣活动和返水、发放跳过无效行；返水按日投注重算且不覆盖已发放。
/// </summary>
public sealed class RescueRebateDeepSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "deep.test");
    private readonly MoneyOperateService _rescue;
    private readonly RebatePayoutService _rebate;
    private readonly int _userId;
    private readonly DateTime _day;

    /// <summary>建站、会员、救援活动和一笔亏损。</summary>
    public RescueRebateDeepSqliteTests()
    {
        _day = DateTime.Today.AddDays(-1);
        _db = SqliteBizSchema.CreateMemoryDb(
            ["Users", "Site", "site_Event", "usr_EventLog", "rpt_UserDateGame", "rpt_UserDateMoney", "site_RebateSetting", "usr_RebateLog", "usr_RebatePlan"],
            []);
        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "deep",
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
        _userId = _db.Insertable(BuildUser("u_deep", 5)).ExecuteReturnIdentity();
        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "救援",
            SubTitle = "",
            Tag = "",
            Cover = "",
            CreateAt = _day,
            StartAt = _day,
            ExpireAt = DateTime.Today.AddDays(10),
            Description = "",
            Content = "",
            SettingString = "Rate=0.1&Flow=2&Statistics=RealWinLose",
            Type = MoneyOperateService.EventTypeRescue,
            IsVisible = true,
            IsOpen = true,
            IsDetail = true,
            Status = 0,
            Views = "",
            Skin = "",
            SupportCurrency = "",
            FilterGame = "",
            FilterPlatform = "",
            Demo = "",
            Background = ""
        }).ExecuteCommand();
        _db.Insertable(new UserDateGame
        {
            SiteID = 1001,
            UserID = _userId,
            Date = _day.Date,
            Type = 10,
            Category = 2,
            BetMoney = 1000,
            Money = -100,
            Currency = 1
        }).ExecuteCommand();
        _db.Insertable(new UserDateMoney
        {
            SiteID = 1001,
            UserID = _userId,
            Date = _day.Date,
            Type = 200,
            Money = 20,
            Count = 1,
            Currency = 1
        }).ExecuteCommand();
        _db.Insertable(new UserDateMoney
        {
            SiteID = 1001,
            UserID = _userId,
            Date = _day.Date,
            Type = 60,
            Money = 10,
            Count = 1,
            Currency = 1
        }).ExecuteCommand();
        _db.Insertable(new RebateSetting
        {
            SiteID = 1001,
            Groups = "5",
            Levels = "",
            BetMoney = 100,
            SettingString = "10=1",
            CreateAt = DateTime.UtcNow,
            MaxMoney = 0,
            IsAuto = false,
            Name = "综合",
            Type = "AA",
            CreatorID = 1,
            MinMoney = 0,
            CreatorName = "t",
            Enabled = true
        }).ExecuteCommand();

        var biz = new TestBizDb(_db);
        _rescue = new MoneyOperateService(biz, _site, new RecordingMoneyLedger(), new Microsoft.AspNetCore.Http.HttpContextAccessor());
        _rebate = new RebatePayoutService(biz, _site, new RecordingMoneyLedger());
    }

    /// <summary>净亏损 70，10% 应发 7，流水倍数 2。</summary>
    [Fact]
    public async Task Query_subtracts_event_and_rebate_when_real_win_lose()
    {
        var raw = await _rescue.QueryAsync(new Dictionary<string, object?>
        {
            ["Date"] = _day.ToString("yyyy-MM-dd")
        });
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(raw));
        var row = doc.RootElement[0];
        Assert.Equal(20m, row.GetProperty("EventMoney").GetDecimal());
        Assert.Equal(10m, row.GetProperty("RebateMoney").GetDecimal());
        Assert.Equal(7m, row.GetProperty("Money").GetDecimal());
        Assert.Equal(14m, row.GetProperty("Flow").GetDecimal());
    }

    /// <summary>当天不能预览发放。</summary>
    [Fact]
    public async Task Query_rejects_today()
    {
        // 无 Furion 宿主时 Oops.Oh 可能包一层类型初始化异常；只断言必须失败。
        await Assert.ThrowsAnyAsync<Exception>(() => _rescue.QueryAsync(new Dictionary<string, object?>
        {
            ["Date"] = DateTime.Today.ToString("yyyy-MM-dd")
        }));
    }

    /// <summary>未知会员记失败；本站会员写入一次，再发同一天记失败。</summary>
    [Fact]
    public async Task Post_skips_unknown_user_and_duplicate()
    {
        var day = _day.ToString("yyyy-MM-dd");
        var first = await _rescue.PostAsync(new Dictionary<string, object?>
        {
            ["1"] = JsonSerializer.Serialize(new { Date = day, EventID = 1, UserID = 999999, Money = 5m, Flow = 1m, Currency = 1 }),
            ["2"] = JsonSerializer.Serialize(new { Date = day, EventID = 1, UserID = _userId, Money = 7m, Flow = 14m, Currency = 1, GameMoney = -100m, EventMoney = 20m, RebateMoney = 10m })
        });
        using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(first)))
        {
            Assert.Equal(1, doc.RootElement.GetProperty("Success").GetInt32());
            Assert.Equal(1, doc.RootElement.GetProperty("Faild").GetInt32());
        }

        var second = await _rescue.PostAsync(new Dictionary<string, object?>
        {
            ["1"] = JsonSerializer.Serialize(new { Date = day, EventID = 1, UserID = _userId, Money = 7m, Flow = 14m, Currency = 1 })
        });
        using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(second)))
        {
            Assert.Equal(0, doc.RootElement.GetProperty("Success").GetInt32());
            Assert.Equal(1, doc.RootElement.GetProperty("Faild").GetInt32());
        }

        var logs = await _db.Queryable<EventLog>().Where(l => l.SiteID == 1001 && l.UserID == _userId).ToListAsync();
        Assert.Single(logs);
    }

    /// <summary>有效投注 1000、比例 1% 写成 10；再算不增行；已发放不改金额。</summary>
    [Fact]
    public async Task Recalculate_writes_once_and_skips_paid()
    {
        var input = new RebateGeneratePlanInput
        {
            StartAt = _day,
            EndAt = _day,
            Name = "重算"
        };
        var created = await _rebate.RecalculateAsync(input);
        Assert.Equal(1, created);
        var again = await _rebate.RecalculateAsync(input);
        Assert.Equal(1, again);
        var rows = await _db.Queryable<UserRebateLog>().Where(l => l.SiteID == 1001).ToListAsync();
        Assert.Single(rows);
        Assert.Equal(10m, rows[0].Money);
        Assert.Equal("AA", rows[0].Type);

        rows[0].Status = RebatePayoutService.LogStatusSuccess;
        rows[0].PlanID = 8;
        await _db.Updateable(rows[0]).UpdateColumns(l => new { l.Status, l.PlanID }).ExecuteCommandAsync();
        await _db.Updateable<UserDateGame>()
            .SetColumns(g => new UserDateGame { BetMoney = 5000 })
            .Where(g => g.SiteID == 1001 && g.UserID == _userId)
            .ExecuteCommandAsync();
        var skipped = await _rebate.RecalculateAsync(input);
        Assert.Equal(0, skipped);
        var kept = await _db.Queryable<UserRebateLog>().Where(l => l.SiteID == 1001).ToListAsync();
        Assert.Equal(10m, kept[0].Money);
    }

    /// <summary>启用开关按编号取反。</summary>
    [Fact]
    public async Task UpdateEnabled_toggles_setting()
    {
        var id = await _db.Queryable<RebateSetting>().Where(s => s.SiteID == 1001).Select(s => s.ID).FirstAsync();
        var off = await _rebate.UpdateenabledAsync(new Dictionary<string, object?> { ["ID"] = id });
        using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(off)))
            Assert.False(doc.RootElement.GetProperty("Enabled").GetBoolean());
        var on = await _rebate.UpdateenabledAsync(new Dictionary<string, object?> { ["ID"] = id });
        using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(on)))
            Assert.True(doc.RootElement.GetProperty("Enabled").GetBoolean());
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private static User BuildUser(string name, int groupId) => new()
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
        GroupID = groupId,
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
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("未定站");
    }
}

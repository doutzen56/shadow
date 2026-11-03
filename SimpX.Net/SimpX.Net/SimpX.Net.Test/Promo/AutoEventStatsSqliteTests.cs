using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 全民代理 / 邀请有礼 / 洗码 / 邀请任务 / 转盘次数（内存 SQLite）。
/// </summary>
public sealed class AutoEventStatsSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly AutoEventStatsService _svc;
    private readonly int _agentId;
    private readonly int _inviteeId;
    private readonly int _playerId;

    /// <summary>
    /// 建站、活动、代理与下级、洗码玩家。
    /// </summary>
    public AutoEventStatsSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(
            ["Users", "Site", "site_Event", "usr_EventLog", "rpt_UserDateGame", "rpt_UserDateMoney", "usr_DelotteryLog",
             "usr_RechargeOrder", "usr_Depth"],
            []);
        EnsureSite();
        _agentId = _db.Insertable(BuildUser("agent", inviteId: 0, agentId: 9, groupId: 1)).ExecuteReturnIdentity();
        _inviteeId = _db.Insertable(BuildUser(
                "invitee",
                inviteId: _agentId,
                agentId: _agentId,
                groupId: 1,
                createAt: new DateTime(2026, 6, 1, 10, 0, 0)))
            .ExecuteReturnIdentity();
        _playerId = _db.Insertable(BuildUser("player", inviteId: 0, agentId: 9, groupId: 1)).ExecuteReturnIdentity();
        EnsureEvents();
        _svc = new AutoEventStatsService(new TestBizDb(_db));
    }

    /// <summary>
    /// 全民代理：直属昨日投注 × 比例写待审日志，SourceID 幂等。
    /// </summary>
    [Fact]
    public async Task UserInvite_writes_pending_log_once()
    {
        var now = new DateTime(2026, 6, 2, 10, 0, 0);
        SeedBet(_inviteeId, now.Date.AddDays(-1), 1000);
        Assert.Equal(1, await _svc.RunUserInviteAsync(now: now));
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.UserID == _agentId && l.Type == AutoEventStatsService.EventTypeUserInvite)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(10m, logs[0].Reward); // 1000 * 0.01
        Assert.Equal(0, await _svc.RunUserInviteAsync(now: now));
    }

    /// <summary>
    /// 邀请有礼日返：给推荐人写待审。
    /// </summary>
    [Fact]
    public async Task InviteReward_daily_pays_inviter_log()
    {
        var now = new DateTime(2026, 6, 2, 12, 5, 0);
        SeedBet(_inviteeId, now.Date.AddDays(-1), 500);
        Assert.Equal(1, await _svc.RunInviteRewardDailyAsync(now: now));
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.UserID == _agentId && l.Type == AutoEventStatsService.EventTypeInviteReward)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(25m, logs[0].Reward); // 500 * 0.05
    }

    /// <summary>
    /// 洗码：昨日投注达标写待审，含积分档，幂等。
    /// </summary>
    [Fact]
    public async Task Washing_writes_pending_log_once()
    {
        var now = new DateTime(2026, 6, 2, 17, 35, 0);
        SeedBet(_playerId, now.Date.AddDays(-1), 200);
        SeedRecharge(_playerId, now.Date.AddDays(-1), 50);
        Assert.Equal(1, await _svc.RunWashingAsync(now: now));
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.UserID == _playerId && l.Type == AutoEventStatsService.EventTypeWashing)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(50m, logs[0].Reward);
        Assert.Equal((short)5, logs[0].Coin);
        Assert.Equal(0, await _svc.RunWashingAsync(now: now));
    }

    /// <summary>
    /// 邀请任务日返：昨日新注册下级达标投注给邀请人写待审。
    /// </summary>
    [Fact]
    public async Task InviteTask_daily_pays_inviter_log()
    {
        var now = new DateTime(2026, 6, 2, 17, 35, 0);
        SeedBet(_inviteeId, now.Date.AddDays(-1), 300);
        Assert.Equal(1, await _svc.RunInviteTaskDailyAsync(now: now));
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.UserID == _agentId && l.Type == AutoEventStatsService.EventTypeInviteTask)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(20m, logs[0].Reward);
        Assert.Equal(0, await _svc.RunInviteTaskDailyAsync(now: now));
    }

    /// <summary>
    /// 转盘打码发次数 + 日清幂等。
    /// </summary>
    [Fact]
    public async Task Delottery_bet_grant_and_clean()
    {
        var grantAt = new DateTime(2026, 6, 2, 1, 5, 0);
        SeedBet(_playerId, grantAt.Date.AddDays(-1), 150);
        Assert.Equal(1, await _svc.RunDelotteryBetGrantAsync(now: grantAt));
        var logs = await _db.Queryable<UserDelotteryLog>()
            .Where(l => l.UserID == _playerId && l.Type == AutoEventStatsService.DelotteryTypeBet)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(3m, logs[0].Point);
        Assert.Equal(0, await _svc.RunDelotteryBetGrantAsync(now: grantAt));

        var cleanAt = new DateTime(2026, 6, 2, 23, 56, 0);
        Assert.Equal(1, await _svc.RunDelotteryCleanAsync(now: cleanAt));
        var clears = await _db.Queryable<UserDelotteryLog>()
            .Where(l => l.UserID == _playerId && l.Type == AutoEventStatsService.DelotteryTypeClearZero)
            .ToListAsync();
        Assert.Single(clears);
        Assert.Equal(-3m, clears[0].Point);
        Assert.Equal(0m, clears[0].Balance);
        Assert.Equal(0, await _svc.RunDelotteryCleanAsync(now: cleanAt));
    }

    /// <summary>
    /// 擂台赛日赛：当日投注排行写待审，SourceID 幂等。
    /// </summary>
    [Fact]
    public async Task Competition_daily_ranks_and_pays()
    {
        var now = new DateTime(2026, 6, 2, 23, 56, 0);
        SeedBet(_playerId, now.Date, 500);
        SeedBet(_inviteeId, now.Date, 200);
        Assert.Equal(2, await _svc.RunCompetitionAsync(now: now));
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.Type == AutoEventStatsService.EventTypeCompetition)
            .OrderBy(l => l.Reward)
            .ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Equal(100m, logs[1].Reward); // 第一名
        Assert.Equal(50m, logs[0].Reward);
        Assert.Equal(0, await _svc.RunCompetitionAsync(now: now));
    }

    /// <summary>
    /// 邀请有礼充值日返。
    /// </summary>
    [Fact]
    public async Task InviteReward_recharge_daily()
    {
        var now = new DateTime(2026, 6, 2, 12, 5, 0);
        SeedRecharge(_inviteeId, now.Date.AddDays(-1), 200);
        Assert.Equal(1, await _svc.RunInviteRewardDailyAsync(now: now));
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.UserID == _agentId && l.SourceID.StartsWith("recharge-"))
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(20m, logs[0].Reward); // 200 * 0.1
    }

    /// <summary>
    /// 邀请有礼亏损月返：注入 7 月 1 日强制月窗，被邀人 6 月 Money 为负。
    /// </summary>
    [Fact]
    public async Task InviteReward_loss_monthly()
    {
        var now = new DateTime(2026, 7, 1, 12, 5, 0);
        SeedGameMoney(_inviteeId, new DateTime(2026, 6, 15), -200m);
        Assert.Equal(1, await _svc.RunInviteRewardDailyAsync(now: now));
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.UserID == _agentId && l.SourceID.StartsWith("loss-"))
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(10m, logs[0].Reward); // max(0, -(-200)) * 0.05
        Assert.Equal(0, await _svc.RunInviteRewardDailyAsync(now: now));
    }

    /// <summary>
    /// 邀请有礼盈利月返：注入 7 月 1 日强制月窗，被邀人 6 月 Money 为正。
    /// </summary>
    [Fact]
    public async Task InviteReward_win_monthly()
    {
        var now = new DateTime(2026, 7, 1, 12, 5, 0);
        SeedGameMoney(_inviteeId, new DateTime(2026, 6, 15), 300m);
        Assert.Equal(1, await _svc.RunInviteRewardDailyAsync(now: now));
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.UserID == _agentId && l.SourceID.StartsWith("win-"))
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(15m, logs[0].Reward); // max(0, 300) * 0.05
        Assert.Equal(0, await _svc.RunInviteRewardDailyAsync(now: now));
    }

    /// <summary>
    /// 邀请任务首充日返：读 usr_RechargeOrder Count=1。
    /// </summary>
    [Fact]
    public async Task InviteTask_first_recharge_daily()
    {
        var now = new DateTime(2026, 6, 2, 17, 35, 0);
        SeedFirstRechargeOrder(_inviteeId, now.Date.AddDays(-1).AddHours(12), 150);
        Assert.Equal(1, await _svc.RunInviteTaskDailyAsync(now: now));
        var logs = await _db.Queryable<EventLog>()
            .Where(l => l.UserID == _agentId && l.Type == AutoEventStatsService.EventTypeInviteTask
                        && l.SourceID.StartsWith("3"))
            .ToListAsync();
        Assert.Single(logs);
        Assert.Equal(15m, logs[0].Reward);
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
            Name = "ae",
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

    private void EnsureEvents()
    {
        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "全民代理",
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
                """{"BetAmount1":0.01,"BetAmount2":0,"BetAmount3":0,"WashFlow":1,"Put":1,"IsAuto":1,"UserGroup":[1]}""",
            Type = AutoEventStatsService.EventTypeUserInvite,
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
        }).ExecuteCommand();

        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "邀请有礼",
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
                """{"ReturnType":"VaildBet","ReturnPeriod":"Day","ReturnWay":"Rate","ReturnValue1":0.05,"WashFlow":1,"Put":1,"IsAuto":1,"UserGroup":[1]}""",
            Type = AutoEventStatsService.EventTypeInviteReward,
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
        }).ExecuteCommand();

        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "洗码",
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
                """{"PutFirst":1,"WashFlow":1,"Recharge":"0","SettingGame":"","UserGroup":[1],"DataSetting":"{\"Rules\":[{\"ID\":1,\"Money\":\"50\",\"Point\":\"100\",\"AwardPoint\":5}]}"}""",
            Type = AutoEventStatsService.EventTypeWashing,
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
        }).ExecuteCommand();

        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "邀请任务",
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
                """{"ReturnType":"VaildBet","ReturnPeriod":"Day","ReturnWay":"Fixed","WashFlow":1,"Put":1,"IsAuto":1,"UserGroup":[1],"DataSetting":"{\"Items\":[{\"ID\":1,\"needMoney\":\"100\",\"Money\":\"20\",\"Member\":1,\"RewardUpLimit\":\"0\"}]}"}""",
            Type = AutoEventStatsService.EventTypeInviteTask,
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
        }).ExecuteCommand();

        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "邀请有礼充值",
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
                """{"ReturnType":"Recharge","ReturnPeriod":"Day","ReturnWay":"Rate","ReturnValue1":0.1,"WashFlow":1,"Put":1,"IsAuto":1,"UserGroup":[1]}""",
            Type = AutoEventStatsService.EventTypeInviteReward,
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
        }).ExecuteCommand();

        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "邀请有礼亏损月返",
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
                """{"ReturnType":"Loss","ReturnPeriod":"Month","ReturnWay":"Rate","ReturnValue1":0.05,"WashFlow":1,"Put":1,"IsAuto":1,"UserGroup":[1]}""",
            Type = AutoEventStatsService.EventTypeInviteReward,
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
        }).ExecuteCommand();

        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "邀请有礼盈利月返",
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
                """{"ReturnType":"Win","ReturnPeriod":"Month","ReturnWay":"Rate","ReturnValue1":0.05,"WashFlow":1,"Put":1,"IsAuto":1,"UserGroup":[1]}""",
            Type = AutoEventStatsService.EventTypeInviteReward,
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
        }).ExecuteCommand();

        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "邀请任务充值",
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
                """{"ReturnType":"FirstRecharge","ReturnPeriod":"Day","ReturnWay":"Fixed","WashFlow":1,"Put":1,"IsAuto":1,"UserGroup":[1],"DataSetting":"{\"Items\":[{\"ID\":1,\"needMoney\":\"100\",\"Money\":\"15\",\"Member\":1,\"RewardUpLimit\":\"0\"}]}"}""",
            Type = AutoEventStatsService.EventTypeInviteTask,
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
        }).ExecuteCommand();

        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            Title = "擂台赛",
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
                """{"ReturnPeriod":"Day","WashFlow":1,"StartWashFlow":"100","Put":1,"IsAuto":1,"PutType":"Reward","UserGroup":[1],"DataSetting":"{\"Items\":[{\"ID\":1,\"Name\":\"冠军\",\"Money\":\"100\",\"AwardPoint\":0},{\"ID\":2,\"Name\":\"亚军\",\"Money\":\"50\",\"AwardPoint\":0}]}"}""",
            Type = AutoEventStatsService.EventTypeCompetition,
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
        }).ExecuteCommand();

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
                """{"UserGroup":[1],"DataSetting":"{\"Rules\":[{\"ID\":1,\"MaxCount\":3,\"Recharge\":\"100\",\"ReturnType\":\"Bet\",\"ReturnPeriod\":\"Day\"}]}"}""",
            Type = AutoEventStatsService.EventTypeDelottery,
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
        }).ExecuteCommand();
    }

    private void SeedBet(int userId, DateTime day, decimal bet)
    {
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

    /// <summary>
    /// 写入游戏日汇总盈亏（UserDateGame.Money），供亏损/盈利口径。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="day">业务日</param>
    /// <param name="money">盈亏金额</param>
    private void SeedGameMoney(int userId, DateTime day, decimal money)
    {
        _db.Insertable(new UserDateGame
        {
            SiteID = 1001,
            UserID = userId,
            Date = day.Date,
            Type = 1,
            Category = 0,
            BetMoney = 0,
            Money = money,
            Currency = 1
        }).ExecuteCommand();
    }

    private void SeedRecharge(int userId, DateTime day, decimal money)
    {
        _db.Insertable(new UserDateMoney
        {
            SiteID = 1001,
            UserID = userId,
            Date = day.Date,
            Type = AutoEventStatsService.MoneyTypeRecharge,
            Money = money,
            Count = 1,
            Currency = 1
        }).ExecuteCommand();
    }

    private void SeedFirstRechargeOrder(int userId, DateTime payAt, decimal paid)
    {
        _db.Insertable(new RechargeOrder
        {
            ID = Guid.NewGuid().ToString("N"),
            SiteID = 1001,
            UserID = userId,
            Money = paid,
            Fee = 0,
            Cost = 0,
            CreateAt = payAt,
            PayID = 1,
            IsPay = true,
            PayAt = payAt,
            Paid = paid,
            SystemID = "T",
            AdminID = 0,
            Count = 1,
            Remark = "",
            IP = "0.0.0.0",
            Currency = 1,
            FromCurrency = 1,
            IsCheckFail = false,
            ActiveID = "0",
            IsDeleted = false,
            IsProxyRecharge = false,
            PayAccount = ""
        }).ExecuteCommand();
    }

    private static User BuildUser(
        string name,
        int inviteId,
        int agentId,
        int groupId,
        DateTime? createAt = null) => new()
    {
        SiteID = 1001,
        UserName = name,
        NickName = name,
        AccountName = "",
        Password = "x",
        PayPassword = "",
        CreateAt = createAt ?? DateTime.UtcNow,
        Face = "",
        RegIP = "",
        Lock = 0,
        Function = 0,
        LoginAt = DateTime.UtcNow,
        LoginIP = "",
        AgentID = agentId,
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
        InviteID = inviteId,
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

using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 人工加扣款（内存 SQLite + 假账本）。禁写 BetWin30。
/// </summary>
public sealed class MoneyOperateSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "money.test");
    private readonly RecordingMoneyLedger _money = new();
    private readonly MoneyOperateService _svc;

    /// <summary>
    /// 建 Users / MoneyLog 壳，植入测试会员。
    /// </summary>
    public MoneyOperateSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [MoneyLog] (
              [LogID] INTEGER NOT NULL,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Money] REAL NOT NULL,
              [Type] INTEGER NOT NULL,
              [SourceID] TEXT,
              [SubType] INTEGER NOT NULL DEFAULT 0,
              [TableID] INTEGER NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [LockMoney] REAL NOT NULL DEFAULT 0,
              [Balance] REAL NOT NULL DEFAULT 0,
              [CreateAt] TEXT,
              [IP] TEXT,
              [LogDesc] TEXT
            );
            CREATE TABLE IF NOT EXISTS [usr_MoneyReview] (
              [ReviewID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Money] REAL NOT NULL,
              [Type] INTEGER NOT NULL,
              [Flow] REAL NOT NULL,
              [MinFlow] REAL NOT NULL,
              [CreateAt] TEXT NOT NULL,
              [CreatorID] INTEGER NOT NULL,
              [SourceID] TEXT,
              [Action] INTEGER NOT NULL,
              [ReviewerID] INTEGER NOT NULL,
              [ReviewAt] TEXT,
              [Status] INTEGER NOT NULL,
              [Currency] INTEGER NOT NULL,
              [LogDesc] TEXT,
              [EventID] INTEGER NOT NULL,
              [EventIDNoTrans] INTEGER NOT NULL,
              [PayID] INTEGER NOT NULL
            );
            """);
        _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "u_grant",
            NickName = "u",
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
            GroupID = 0,
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
        }).ExecuteCommand();

        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "t",
            CreateAt = DateTime.UtcNow,
            NotifyAt = DateTime.UtcNow,
            Status = 0,
            Currency = 1,
            Wallet = 0,
            IsCurrencySelect = false,
            APIKey = Guid.NewGuid(),
            SettingString = "{}",
            APPSettingString = "",
            PCSettingString = "",
            GameCredit = 0,
            NotifyCredit = 0,
            AdminDomain = "",
            HomeTemplateName = ""
        }).ExecuteCommand();

        _svc = new MoneyOperateService(new TestBizDb(_db), _site, _money, new HttpContextAccessor());
    }

    /// <summary>
    /// 免审加款走账本，金额为正。
    /// </summary>
    [Fact]
    public async Task Grant_without_check_calls_ledger_positive()
    {
        var r = await _svc.GrantAsync(new MoneyOperateSubmitInput
        {
            User = "u_grant",
            Type = 2,
            Currency = 1,
            Money = 10,
            SourceID = "src-grant-1",
            Description = "前",
            DescriptionA = "后",
            Flow = 1,
            MinFlow = 0
        });
        Assert.True(r.Items[0].Success);
        Assert.Single(_money.Logs);
        Assert.Equal(10m, _money.Logs[0].Money);
        Assert.Equal("src-grant-1", _money.Logs[0].SourceId);
    }

    /// <summary>
    /// 免审扣款走账本，金额为负。
    /// </summary>
    [Fact]
    public async Task Deduct_without_check_calls_ledger_negative()
    {
        var r = await _svc.DeductAsync(new MoneyOperateSubmitInput
        {
            User = "u_grant",
            Type = 3,
            Currency = 1,
            Money = 5,
            SourceID = "src-deduct-1",
            Description = "前",
            DescriptionA = "后"
        });
        Assert.True(r.Items[0].Success);
        Assert.Equal(-5m, _money.Logs[0].Money);
    }

    /// <summary>
    /// 开启审核时只写审核单，不调账本（JSON Setting）。
    /// </summary>
    [Fact]
    public async Task Grant_with_need_check_inserts_review_only()
    {
        await _db.Updateable<Site>()
            .SetColumns(s => s.SettingString == "{\"MoneyChangedNeedCheck\":true}")
            .Where(s => s.ID == 1001)
            .ExecuteCommandAsync();

        var r = await _svc.GrantAsync(new MoneyOperateSubmitInput
        {
            User = "u_grant",
            Type = 2,
            Currency = 1,
            Money = 8,
            SourceID = "src-review-1",
            Description = "前",
            DescriptionA = "后",
            Flow = 2,
            MinFlow = 1
        });
        Assert.True(r.Items[0].Success);
        Assert.Empty(_money.Logs);
        var n = await _db.Queryable<MoneyReview>().CountAsync();
        Assert.Equal(1, n);
    }

    /// <summary>
    /// 真库常见 query 串 Setting：MoneyChangedNeedCheck=True 也要进审核。
    /// </summary>
    [Fact]
    public async Task Grant_with_query_setting_need_check_inserts_review_only()
    {
        await _db.Updateable<Site>()
            .SetColumns(s => s.SettingString == "Logo=a&MoneyChangedNeedCheck=True&MinMoney=1")
            .Where(s => s.ID == 1001)
            .ExecuteCommandAsync();

        var r = await _svc.GrantAsync(new MoneyOperateSubmitInput
        {
            User = "u_grant",
            Type = 2,
            Currency = 1,
            Money = 3,
            SourceID = "src-review-q1",
            Description = "前",
            DescriptionA = "后"
        });
        Assert.True(r.Items[0].Success);
        Assert.Empty(_money.Logs);
        Assert.Equal(1, await _db.Queryable<MoneyReview>().CountAsync());
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>假账本。</summary>

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
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

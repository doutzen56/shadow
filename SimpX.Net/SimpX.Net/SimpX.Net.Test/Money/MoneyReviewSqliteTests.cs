using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 加减款审核（内存 SQLite + 假账本）。禁写 BetWin30。
/// </summary>
public sealed class MoneyReviewSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "review.test");
    private readonly RecordingMoneyLedger _money = new();
    private readonly MoneyReviewService _svc;
    private int _userId;

    /// <summary>
    /// 建表并植入待审单。
    /// </summary>
    public MoneyReviewSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
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
            CREATE TABLE IF NOT EXISTS [usr_MoneyLock] (
              [LockID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Type] INTEGER NOT NULL,
              [SourceID] INTEGER NOT NULL,
              [Money] REAL NOT NULL,
              [IsLock] INTEGER NOT NULL,
              [Currency] INTEGER NOT NULL,
              [UnLockAt] TEXT,
              [UnLockDesc] TEXT
            );
            """);
        _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "u_rev",
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
        _userId = _db.Queryable<User>().Select(u => u.ID).First();
        _db.Insertable(new MoneyReview
        {
            SiteID = 1001,
            UserID = _userId,
            Money = 12,
            Type = 2,
            Flow = 1,
            MinFlow = 0,
            CreateAt = DateTime.Now,
            CreatorID = 1,
            SourceID = "src-r1",
            Action = MoneyReviewService.ActionIn,
            ReviewerID = 0,
            ReviewAt = DateTime.MinValue,
            Status = MoneyReviewService.StatusNone,
            Currency = 1,
            Description = "前 | 后",
            EventID = 0,
            EventIDNoTrans = 0,
            PayID = 0
        }).ExecuteCommand();
        _svc = new MoneyReviewService(new TestBizDb(_db), _site, _money);
    }

    /// <summary>
    /// 列表能读到待审。
    /// </summary>
    [Fact]
    public async Task List_pending_returns_row()
    {
        var list = await _svc.ListAsync(new MoneyReviewListInput { Status = "0" });
        Assert.Single(list);
        Assert.Equal("u_rev", list[0].UserName);
        Assert.Equal(12m, list[0].Money);
    }

    /// <summary>
    /// 通过走账本。
    /// </summary>
    [Fact]
    public async Task Check_success_calls_ledger()
    {
        var id = await _db.Queryable<MoneyReview>().Select(r => r.ID).FirstAsync();
        await _svc.CheckAsync(new MoneyReviewCheckInput { ID = id, Status = MoneyReviewService.StatusSuccess }, 9);
        Assert.Single(_money.Logs);
        Assert.Equal(12m, _money.Logs[0].Money);
        var st = await _db.Queryable<MoneyReview>().Where(r => r.ID == id).Select(r => r.Status).FirstAsync();
        Assert.Equal(MoneyReviewService.StatusSuccess, st);
    }

    /// <summary>
    /// 拒绝不走账本。
    /// </summary>
    [Fact]
    public async Task Check_fail_skips_ledger()
    {
        var id = await _db.Queryable<MoneyReview>().Select(r => r.ID).FirstAsync();
        await _svc.CheckAsync(new MoneyReviewCheckInput { ID = id, Status = MoneyReviewService.StatusFailed }, 9);
        Assert.Empty(_money.Logs);
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

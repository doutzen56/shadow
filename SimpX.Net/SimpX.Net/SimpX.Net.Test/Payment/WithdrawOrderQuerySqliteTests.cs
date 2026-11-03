using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 提现订单只读分页（内存 SQLite）。禁写 BetWin30。
/// </summary>
public sealed class WithdrawOrderQuerySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "wd.test");
    private readonly WithdrawOrderService _svc;
    private readonly int _userId;

    /// <summary>
    /// 建 Users / usr_WithdrawOrder，植入一单。
    /// </summary>
    public WithdrawOrderQuerySqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_WithdrawOrder] (
              [OrderID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [CreateAt] TEXT NOT NULL,
              [AccountBank] INTEGER NOT NULL DEFAULT 0,
              [AccountName] TEXT,
              [AccountNumber] TEXT,
              [Money] REAL NOT NULL,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [Fee] REAL NOT NULL DEFAULT 0,
              [Paid] REAL NOT NULL DEFAULT 0,
              [PayAt] TEXT,
              [ChannelID] INTEGER NOT NULL DEFAULT 0,
              [Description] TEXT,
              [Mark] TEXT,
              [IP] TEXT,
              [Cost] REAL NOT NULL DEFAULT 0,
              [SystemID] TEXT,
              [AdminID] INTEGER NOT NULL DEFAULT 0,
              [CashierID] INTEGER NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [FromCurrency] INTEGER NOT NULL DEFAULT 0,
              [AuditAt] TEXT
            );
            """);

        _userId = _db.Insertable(BuildUser("u_wd")).ExecuteReturnIdentity();
        _db.Insertable(new WithdrawOrder
        {
            SiteID = 1001,
            UserID = _userId,
            CreateAt = DateTime.UtcNow.AddHours(-1),
            Bank = 1,
            AccountName = "张三",
            AccountNumber = "6222021234567890",
            Money = 100,
            Status = 0,
            Fee = 1,
            Paid = 99,
            PayAt = DateTime.UtcNow,
            ChannelID = 1,
            Description = "",
            Mark = "",
            IP = "127.0.0.1",
            Cost = 0,
            SystemID = "",
            AdminID = 0,
            CashierID = 0,
            Currency = 1,
            FromCurrency = 1,
            AuditAt = DateTime.UtcNow
        }).ExecuteCommand();

        _db.Insertable(new WithdrawOrder
        {
            SiteID = 9999,
            UserID = 1,
            CreateAt = DateTime.UtcNow,
            Bank = 1,
            AccountName = "他站",
            AccountNumber = "1111222233334444",
            Money = 50,
            Status = 0,
            Fee = 0,
            Paid = 50,
            PayAt = DateTime.UtcNow,
            ChannelID = 1,
            Description = "",
            Mark = "",
            IP = "",
            Cost = 0,
            SystemID = "",
            AdminID = 0,
            CashierID = 0,
            Currency = 1,
            FromCurrency = 1,
            AuditAt = DateTime.UtcNow
        }).ExecuteCommand();

        _svc = new WithdrawOrderService(new TestBizDb(_db), _site);
    }

    /// <summary>
    /// 只返回本站订单，卡号脱敏。
    /// </summary>
    [Fact]
    public async Task Page_filters_site_and_masks_account()
    {
        var page = await _svc.PageAsync(new WithdrawOrderPageInput { Page = 1, PageSize = 20 });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal("u_wd", page.Items[0].UserName);
        Assert.Equal("6222****7890", page.Items[0].AccountNumber);
    }

    /// <summary>
    /// 按会员名过滤。
    /// </summary>
    [Fact]
    public async Task Page_filter_by_user_name()
    {
        var page = await _svc.PageAsync(new WithdrawOrderPageInput { User = "u_wd", Page = 1, PageSize = 20 });
        Assert.Equal(1, page.Total);
        var miss = await _svc.PageAsync(new WithdrawOrderPageInput { User = "nobody", Page = 1, PageSize = 20 });
        Assert.Equal(0, miss.Total);
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
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

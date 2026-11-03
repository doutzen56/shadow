using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 入款审核（内存 SQLite + 假账本）。禁写 BetWin30。
/// </summary>
public sealed class BankRechargeCheckSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "brc.test");
    private readonly RecordingMoneyLedger _money = new();
    private readonly BankRechargeCheckService _svc;
    private readonly int _orderPk;

    /// <summary>
    /// 建表植入待审入款单。
    /// </summary>
    public BankRechargeCheckSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_BankRechargeOrder] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [PayID] INTEGER NOT NULL DEFAULT 0,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [CreateAt] TEXT NOT NULL,
              [CheckAt] TEXT,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [Name] TEXT,
              [Account] TEXT,
              [Money] REAL NOT NULL,
              [Paid] REAL NOT NULL DEFAULT 0,
              [OrderID] TEXT,
              [Description] TEXT,
              [AdminID] INTEGER NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [PayOrderID] TEXT,
              [IsDeleted] INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS [usr_RechargeOrder] (
              [OrderID] TEXT NOT NULL PRIMARY KEY,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Money] REAL NOT NULL,
              [Fee] REAL NOT NULL DEFAULT 0,
              [Cost] REAL NOT NULL DEFAULT 0,
              [CreateAt] TEXT NOT NULL,
              [PayID] INTEGER NOT NULL DEFAULT 0,
              [IsPay] INTEGER NOT NULL DEFAULT 0,
              [PayAt] TEXT,
              [Paid] REAL NOT NULL DEFAULT 0,
              [SystemID] TEXT,
              [AdminID] INTEGER NOT NULL DEFAULT 0,
              [Count] INTEGER NOT NULL DEFAULT 0,
              [Remark] TEXT,
              [IP] TEXT,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [FromCurrency] INTEGER NOT NULL DEFAULT 0,
              [IsCheckFail] INTEGER NOT NULL DEFAULT 0,
              [ActiveID] TEXT,
              [IsDeleted] INTEGER NOT NULL DEFAULT 0,
              [IsProxyRecharge] INTEGER NOT NULL DEFAULT 0,
              [PayAccount] TEXT
            );
            """);

        var uid = _db.Insertable(BuildUser("u_brc")).ExecuteReturnIdentity();
        _db.Insertable(new RechargeOrder
        {
            ID = "RO-BRC-1",
            SiteID = 1001,
            UserID = uid,
            Money = 88,
            Fee = 0,
            Cost = 0,
            CreateAt = DateTime.UtcNow,
            PayID = 1,
            IsPay = false,
            PayAt = DateTime.UtcNow,
            Paid = 0,
            SystemID = "",
            AdminID = 0,
            Count = 0,
            Remark = "",
            IP = "",
            Currency = 1,
            FromCurrency = 1,
            IsCheckFail = false,
            ActiveID = "",
            IsDeleted = false,
            IsProxyRecharge = false,
            PayAccount = ""
        }).ExecuteCommand();

        _orderPk = _db.Insertable(new BankRechargeOrder
        {
            PayID = 1,
            SiteID = 1001,
            UserID = uid,
            CreateAt = DateTime.UtcNow.AddMinutes(-10),
            CheckAt = DateTime.UtcNow,
            Status = BankRechargeCheckService.StatusNone,
            Name = "李四",
            Account = "6222",
            Money = 88,
            Paid = 0,
            OrderID = "RO-BRC-1",
            Description = "",
            AdminID = 0,
            Currency = 1,
            PayOrderID = "",
            IsDeleted = false
        }).ExecuteReturnIdentity();

        _svc = new BankRechargeCheckService(new TestBizDb(_db), _site, _money);
    }

    /// <summary>
    /// 列表能看到待审单。
    /// </summary>
    [Fact]
    public async Task List_returns_pending()
    {
        var list = await _svc.ListAsync(new BankRechargeCheckListInput { Status = "0" });
        Assert.Contains(list, x => x.ID == _orderPk && x.UserName == "u_brc");
    }

    /// <summary>
    /// 通过审核写充值账变并改状态。
    /// </summary>
    [Fact]
    public async Task Check_success_calls_ledger()
    {
        await _svc.CheckAsync(new BankRechargeCheckInput
        {
            ID = _orderPk,
            Status = BankRechargeCheckService.StatusSuccess,
            Paid = 88,
            Description = "通过",
            DescriptionA = "备注"
        }, adminId: 9);

        Assert.Single(_money.Logs);
        Assert.Equal(88m, _money.Logs[0].Money);
        Assert.Equal(BankRechargeCheckService.MoneyTypeRecharge, _money.Logs[0].Type);

        var order = await _db.Queryable<BankRechargeOrder>().InSingleAsync(_orderPk);
        Assert.Equal(BankRechargeCheckService.StatusSuccess, order.Status);
        Assert.Equal(88m, order.Paid);
        Assert.Equal(9, order.AdminID);

        var ro = await _db.Queryable<RechargeOrder>().InSingleAsync("RO-BRC-1");
        Assert.True(ro.IsPay);
        Assert.Equal(88m, ro.Paid);
    }

    /// <summary>
    /// 拒绝不写账本，Paid 置 0。
    /// </summary>
    [Fact]
    public async Task Check_reject_skips_ledger()
    {
        await _svc.CheckAsync(new BankRechargeCheckInput
        {
            ID = _orderPk,
            Status = BankRechargeCheckService.StatusFailed,
            Description = "拒"
        }, adminId: 1);

        Assert.Empty(_money.Logs);
        var order = await _db.Queryable<BankRechargeOrder>().InSingleAsync(_orderPk);
        Assert.Equal(BankRechargeCheckService.StatusFailed, order.Status);
        Assert.Equal(0m, order.Paid);
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

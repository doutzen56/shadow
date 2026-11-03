using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 拒绝删除未到账充值单（内存 SQLite）。
/// </summary>
public sealed class RechargeOrderDeleteSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "del.test");
    private readonly RechargeOrderDeleteService _svc;
    private readonly int _uid;

    /// <summary>
    /// 建表并植入未到账单 + 关联线下入款单。
    /// </summary>
    public RechargeOrderDeleteSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
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
            CREATE TABLE IF NOT EXISTS [site_AdminLog] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [AdminID] INTEGER NOT NULL DEFAULT 0,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [Content] TEXT,
              [IP] TEXT,
              [IPAddress] TEXT,
              [CreateAt] TEXT,
              [RawData] TEXT
            );
            """);

        _uid = _db.Insertable(BuildUser("u_del")).ExecuteReturnIdentity();
        InsertOrder("RO-DEL-1", isPay: false);
        InsertOrder("RO-DEL-PAID", isPay: true);
        _db.Insertable(new BankRechargeOrder
        {
            PayID = 1,
            SiteID = 1001,
            UserID = _uid,
            CreateAt = DateTime.UtcNow,
            CheckAt = DateTime.UtcNow,
            Status = 0,
            Name = "",
            Account = "",
            Money = 10,
            Paid = 0,
            OrderID = "RO-DEL-1",
            Description = "",
            AdminID = 0,
            Currency = 1,
            PayOrderID = "",
            IsDeleted = false
        }).ExecuteCommand();

        _svc = new RechargeOrderDeleteService(new TestBizDb(_db), _site);
    }

    /// <summary>
    /// 未到账可软删，关联入款单一并删。
    /// </summary>
    [Fact]
    public async Task Delete_unpaid_soft_deletes_order_and_bank()
    {
        await _svc.DeleteAsync("RO-DEL-1", adminId: 9);
        var order = await _db.Queryable<RechargeOrder>().InSingleAsync("RO-DEL-1");
        Assert.True(order.IsDeleted);
        var bank = await _db.Queryable<BankRechargeOrder>().Where(b => b.OrderID == "RO-DEL-1").FirstAsync();
        Assert.True(bank.IsDeleted);
        var log = await _db.Queryable<SiteAdminLog>().Where(l => l.RawData == "RO-DEL-1").FirstAsync();
        Assert.Equal(9, log.AdminID);
        Assert.Contains("DeleteRechargeOrder", log.Content);
    }

    /// <summary>
    /// 已到账不可删。
    /// </summary>
    [Fact]
    public async Task Delete_paid_rejects()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.DeleteAsync("RO-DEL-PAID"));
        var order = await _db.Queryable<RechargeOrder>().InSingleAsync("RO-DEL-PAID");
        Assert.False(order.IsDeleted);
    }

    /// <summary>
    /// 列表不再出现已删单。
    /// </summary>
    [Fact]
    public async Task Page_hides_deleted()
    {
        await _svc.DeleteAsync("RO-DEL-1");
        var pageSvc = new RechargeOrderService(new TestBizDb(_db), _site);
        var page = await pageSvc.PageAsync(new RechargeOrderPageInput { Page = 1, PageSize = 50 });
        Assert.DoesNotContain(page.Items, x => x.OrderID == "RO-DEL-1");
        Assert.Contains(page.Items, x => x.OrderID == "RO-DEL-PAID");
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private void InsertOrder(string id, bool isPay)
    {
        _db.Insertable(new RechargeOrder
        {
            ID = id,
            SiteID = 1001,
            UserID = _uid,
            Money = 10,
            Fee = 0,
            Cost = 0,
            CreateAt = DateTime.UtcNow,
            PayID = 1,
            IsPay = isPay,
            PayAt = DateTime.UtcNow,
            Paid = isPay ? 10 : 0,
            SystemID = isPay ? "GW" : "",
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
    }

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

    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host) { SiteId = siteId; Host = host; }
        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

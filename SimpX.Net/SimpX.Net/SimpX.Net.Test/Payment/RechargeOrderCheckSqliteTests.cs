using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 充值订单确认到账（内存 SQLite + 假账本）。禁写 BetWin30。
/// </summary>
public sealed class RechargeOrderCheckSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "roc.test");
    private readonly RecordingMoneyLedger _money = new();
    private readonly RechargeOrderCheckService _svc;
    private readonly int _uid;

    /// <summary>
    /// 建表并植入未到账订单。
    /// </summary>
    public RechargeOrderCheckSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site"], []);
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
            """);

        _uid = _db.Insertable(BuildUser("u_roc")).ExecuteReturnIdentity();
        InsertOrder("RO-CHECK-1", money: 100m, isPay: false, systemId: "");
        _svc = new RechargeOrderCheckService(new TestBizDb(_db), _site, _money, new MemoryLedgerSideEffectQueues());
    }

    /// <summary>
    /// 确认到账写充值账变；有手续费时再写负手续费。
    /// </summary>
    [Fact]
    public async Task Check_success_credits_and_marks_paid()
    {
        await _svc.CheckAsync(new RechargeOrderCheckInput
        {
            ID = "RO-CHECK-1",
            SystemID = "GW-1",
            Paid = 100m,
            Fee = 2m,
            Cost = 0.5m
        }, adminId: 7);

        Assert.Equal(2, _money.Logs.Count);
        Assert.Equal(100m, _money.Logs[0].Money); // Paid+Fee = 回调金额
        Assert.Equal(RechargeOrderCheckService.MoneyTypeRecharge, _money.Logs[0].Type);
        Assert.Equal(-2m, _money.Logs[1].Money);
        Assert.Equal(RechargeOrderCheckService.MoneyTypeRechargeFee, _money.Logs[1].Type);

        var order = await _db.Queryable<RechargeOrder>().InSingleAsync("RO-CHECK-1");
        Assert.True(order.IsPay);
        Assert.Equal(98m, order.Paid);
        Assert.Equal(2m, order.Fee);
        Assert.Equal(0.5m, order.Cost);
        Assert.Equal("GW-1", order.SystemID);
        Assert.Equal(7, order.AdminID);
        Assert.Equal(1, order.Count);
    }

    /// <summary>
    /// 已入账再确认应失败，余额不再变。
    /// </summary>
    [Fact]
    public async Task Check_twice_rejects()
    {
        await _svc.CheckAsync(new RechargeOrderCheckInput
        {
            ID = "RO-CHECK-1",
            SystemID = "GW-A",
            Paid = 100m
        }, adminId: 1);

        await Assert.ThrowsAnyAsync<Exception>(() => _svc.CheckAsync(new RechargeOrderCheckInput
        {
            ID = "RO-CHECK-1",
            SystemID = "GW-B",
            Paid = 100m
        }, adminId: 1));

        Assert.Single(_money.Logs);
    }

    /// <summary>
    /// 回调金额大于订单金额拒绝。
    /// </summary>
    [Fact]
    public async Task Check_overpay_rejects()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.CheckAsync(new RechargeOrderCheckInput
        {
            ID = "RO-CHECK-1",
            SystemID = "GW-O",
            Paid = 100.01m
        }, adminId: 1));
        Assert.Empty(_money.Logs);
    }

    /// <summary>
    /// 网关单号冲突拒绝。
    /// </summary>
    [Fact]
    public async Task Check_duplicate_system_id_rejects()
    {
        InsertOrder("RO-CHECK-2", money: 10m, isPay: true, systemId: "GW-DUP");
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.CheckAsync(new RechargeOrderCheckInput
        {
            ID = "RO-CHECK-1",
            SystemID = "GW-DUP",
            Paid = 10m
        }, adminId: 1));
        Assert.Empty(_money.Logs);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private void InsertOrder(string id, decimal money, bool isPay, string systemId)
    {
        _db.Insertable(new RechargeOrder
        {
            ID = id,
            SiteID = 1001,
            UserID = _uid,
            Money = money,
            Fee = 0,
            Cost = 0,
            CreateAt = DateTime.UtcNow,
            PayID = 1,
            IsPay = isPay,
            PayAt = DateTime.UtcNow,
            Paid = isPay ? money : 0,
            SystemID = systemId,
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
        public FixedSiteAccessor(int? siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

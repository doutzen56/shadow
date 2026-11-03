using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 充值网关 Mock 回调（内存 SQLite + 假账本）。
/// </summary>
public sealed class RechargeNotifySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly RechargeNotifyService _svc;
    private readonly int _uid;
    private const int PayId = 88;

    /// <summary>
    /// 建表：通道 + 未到账订单。
    /// </summary>
    public RechargeNotifySqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [site_Payment] (
              [PayID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [CateID] INTEGER NOT NULL DEFAULT 0,
              [SiteID] INTEGER NOT NULL,
              [PayName] TEXT,
              [PayDesc] TEXT,
              [Remark] TEXT,
              [Sort] INTEGER NOT NULL DEFAULT 0,
              [Status] INTEGER NOT NULL DEFAULT 1,
              [Setting] TEXT,
              [SysID] INTEGER NOT NULL DEFAULT 0,
              [MinMoney] REAL NOT NULL DEFAULT 0,
              [MaxMoney] REAL NOT NULL DEFAULT 0,
              [Amount] TEXT,
              [Agent] TEXT,
              [Fee] REAL NOT NULL DEFAULT 0,
              [MinFee] REAL NOT NULL DEFAULT 0,
              [MaxFee] REAL NOT NULL DEFAULT 0,
              [Cost] REAL NOT NULL DEFAULT 0,
              [MinCost] REAL NOT NULL DEFAULT 0,
              [MaxCost] REAL NOT NULL DEFAULT 0,
              [MaxPay] REAL NOT NULL DEFAULT 0,
              [Platform] INTEGER NOT NULL DEFAULT 0,
              [IsDeleted] INTEGER NOT NULL DEFAULT 0,
              [IsNotify] INTEGER NOT NULL DEFAULT 0,
              [IsERC] TEXT,
              [IsTRC] TEXT,
              [RegisterDays] INTEGER NOT NULL DEFAULT 0,
              [IsOpenAccount] INTEGER NOT NULL DEFAULT 0,
              [OpenType] INTEGER NOT NULL DEFAULT 0,
              [IsRandom] INTEGER NOT NULL DEFAULT 0,
              [IsNotHaveZero] INTEGER NOT NULL DEFAULT 0,
              [IsQBNotHaveZero] INTEGER NOT NULL DEFAULT 0,
              [IsKYC] INTEGER NOT NULL DEFAULT 0,
              [IsBank] INTEGER NOT NULL DEFAULT 0,
              [IsMobile] INTEGER NOT NULL DEFAULT 0,
              [IsEmail] INTEGER NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [DownloadLink] TEXT
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

        // 固定 PayID=88：先插到 87 再插目标行较麻烦，直接指定 identity 关闭时用原生 SQL
        _db.Ado.ExecuteCommand(
            """
            INSERT INTO [site_Payment]([PayID],[SiteID],[PayName],[Fee],[Cost],[IsDeleted],[Status])
            VALUES(88,1001,'mock',0.01,0.02,0,1);
            """);

        _uid = _db.Insertable(BuildUser("u_rn")).ExecuteReturnIdentity();
        InsertOrder("RO-NOTIFY-1", money: 100m, payId: PayId, isPay: false);
        var check = new RechargeOrderCheckService(new TestBizDb(_db), new FixedSiteAccessor(1001, "rn.test"), _money, new MemoryLedgerSideEffectQueues());
        _svc = new RechargeNotifyService(new TestBizDb(_db), check, new RechargeNotifyPluginRouter(new IRechargeNotifyPlugin[] { new MockFormRechargeNotifyPlugin(), new HttpMd5RechargeNotifyPlugin() }, new MockFormRechargeNotifyPlugin()));
    }

    /// <summary>
    /// 成功回调入账并回 SUCCESS。
    /// </summary>
    [Fact]
    public async Task Notify_success_credits()
    {
        var body = "orderId=RO-NOTIFY-1&systemId=GW-N1&money=100&success=true";
        var ack = await _svc.NotifyAsync(PayId, body);
        Assert.Equal("SUCCESS", ack);

        var order = await _db.Queryable<RechargeOrder>().InSingleAsync("RO-NOTIFY-1");
        Assert.True(order.IsPay);
        Assert.Equal("GW-N1", order.SystemID);
        // Fee=1% of 100 = 1 → Paid=99；账本 Paid+Fee=100，再 -1 手续费
        Assert.Equal(99m, order.Paid);
        Assert.Equal(2, _money.Logs.Count);
        Assert.Equal(100m, _money.Logs[0].Money);
        Assert.Equal(-1m, _money.Logs[1].Money);
    }

    /// <summary>
    /// success=false 不入账。
    /// </summary>
    [Fact]
    public async Task Notify_failed_flag_skips_credit()
    {
        var ack = await _svc.NotifyAsync(PayId, "orderId=RO-NOTIFY-1&systemId=GW-X&money=100&success=false&message=bank-reject");
        Assert.Equal("bank-reject", ack);
        Assert.Empty(_money.Logs);
        var order = await _db.Queryable<RechargeOrder>().InSingleAsync("RO-NOTIFY-1");
        Assert.False(order.IsPay);
    }

    /// <summary>
    /// 重复回调第二次不得 SUCCESS 入账。
    /// </summary>
    [Fact]
    public async Task Notify_twice_second_not_success()
    {
        var body = "orderId=RO-NOTIFY-1&systemId=GW-A&money=100&success=true";
        Assert.Equal("SUCCESS", await _svc.NotifyAsync(PayId, body));
        var second = await _svc.NotifyAsync(PayId, "orderId=RO-NOTIFY-1&systemId=GW-B&money=100&success=true");
        Assert.NotEqual("SUCCESS", second);
        Assert.Equal(2, _money.Logs.Count); // 仅第一笔手续费拆分两笔
    }

    /// <summary>
    /// 通道与订单 PayID 不一致拒绝。
    /// </summary>
    [Fact]
    public async Task Notify_payid_mismatch_rejects()
    {
        InsertOrder("RO-NOTIFY-2", money: 50m, payId: 99, isPay: false);
        var ack = await _svc.NotifyAsync(PayId, "orderId=RO-NOTIFY-2&systemId=GW-M&money=50&success=true");
        Assert.Equal("#支付渠道不匹配#", ack);
        Assert.Empty(_money.Logs);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private void InsertOrder(string id, decimal money, int payId, bool isPay)
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
            PayID = payId,
            IsPay = isPay,
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

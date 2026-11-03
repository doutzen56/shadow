using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Systems;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 充值回调 IP 白名单与 Mock MD5 验签（内存 SQLite）。
/// </summary>
public sealed class RechargeNotifySecureSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly RechargeNotifyService _svc;
    private readonly int _uid;
    private int _payId;

    /// <summary>
    /// 建分组白名单、系统支付、站点通道、未到账单。
    /// </summary>
    public RechargeNotifySecureSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [sys_PaymentGroup] (
              [GroupID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [GroupName] TEXT,
              [CallbackIP] TEXT,
              [IsEnable] TEXT
            );
            CREATE TABLE IF NOT EXISTS [sys_Payment] (
              [SettingID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [GroupID] INTEGER NOT NULL DEFAULT 0,
              [Name] TEXT,
              [IsOpen] INTEGER NOT NULL DEFAULT 1,
              [Fields] TEXT,
              [Bank] TEXT,
              [Sign] TEXT,
              [Submit] TEXT,
              [Callback] TEXT,
              [SpecialType] INTEGER NOT NULL DEFAULT 0
            );
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

        var groupId = _db.Insertable(new SystemPaymentGroup
        {
            Name = "g1",
            CallbackIP = "10.0.0.8|203.0.113.10",
            IsEnable = "1"
        }).ExecuteReturnIdentity();
        var sysId = _db.Insertable(new SystemPayment
        {
            GroupID = groupId,
            Name = "sys",
            IsOpen = true,
            FieldString = "",
            BankString = "",
            SignString = "",
            SubmitString = "",
            CallbackString = "",
            SpecialType = 0
        }).ExecuteReturnIdentity();

        _payId = _db.Insertable(new SitePayment
        {
            SiteID = 1001,
            CateID = 1,
            Name = "secure-mock",
            Description = "",
            Remark = "",
            Sort = 0,
            Status = 1,
            SettingString = "NotifyKey=test-secret",
            SysID = sysId,
            MinMoney = 0,
            MaxMoney = 0,
            Amount = "",
            Agent = "",
            Fee = 0,
            MinFee = 0,
            MaxFee = 0,
            Cost = 0,
            MinCost = 0,
            MaxCost = 0,
            MaxPay = 0,
            Platform = 0,
            IsDeleted = false,
            IsNotify = false,
            IsERC = "",
            IsTRC = "",
            RegisterDays = 0,
            IsOpenAccount = false,
            OpenType = 0,
            IsRandom = false,
            IsNotHaveZero = false,
            IsQBNotHaveZero = false,
            IsKYC = false,
            IsBank = false,
            IsMobile = false,
            IsEmail = false,
            Currency = 1,
            DownloadLink = ""
        }).ExecuteReturnIdentity();

        _uid = _db.Insertable(BuildUser("u_sec")).ExecuteReturnIdentity();
        InsertOrder("RO-SEC-1");
        var check = new RechargeOrderCheckService(new TestBizDb(_db), new FixedSiteAccessor(1001, "sec.test"), _money, new MemoryLedgerSideEffectQueues());
        _svc = new RechargeNotifyService(new TestBizDb(_db), check, new RechargeNotifyPluginRouter(new IRechargeNotifyPlugin[] { new MockFormRechargeNotifyPlugin(), new HttpMd5RechargeNotifyPlugin() }, new MockFormRechargeNotifyPlugin()));
    }

    /// <summary>
    /// IP 不在白名单拒绝。
    /// </summary>
    [Fact]
    public async Task Notify_rejects_bad_ip()
    {
        var body = SignedBody("RO-SEC-1", "GW1", "100");
        var ack = await _svc.NotifyAsync(_payId, body, clientIp: "1.2.3.4");
        Assert.StartsWith("非法回调", ack);
        Assert.Empty(_money.Logs);
    }

    /// <summary>
    /// 签名错误拒绝。
    /// </summary>
    [Fact]
    public async Task Notify_rejects_bad_sign()
    {
        var body = "orderId=RO-SEC-1&systemId=GW1&money=100&success=true&sign=deadbeef";
        var ack = await _svc.NotifyAsync(_payId, body, clientIp: "10.0.0.8");
        Assert.Equal("#回调验签失败#", ack);
        Assert.Empty(_money.Logs);
    }

    /// <summary>
    /// IP+签都对则入账。
    /// </summary>
    [Fact]
    public async Task Notify_accepts_good_ip_and_sign()
    {
        var body = SignedBody("RO-SEC-1", "GW-OK", "100");
        var ack = await _svc.NotifyAsync(_payId, body, clientIp: "10.0.0.8");
        Assert.Equal("SUCCESS", ack);
        Assert.Single(_money.Logs);
        var order = await _db.Queryable<RechargeOrder>().InSingleAsync("RO-SEC-1");
        Assert.True(order.IsPay);
    }

    /// <summary>
    /// XFF 解析取首段。
    /// </summary>
    [Fact]
    public void ResolveClientIp_takes_xff_first()
    {
        var ip = RechargeNotifySecurity.ResolveClientIp("10.0.0.8, 1.1.1.1", "9.9.9.9", "8.8.8.8");
        Assert.Equal("10.0.0.8", ip);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private static string SignedBody(string orderId, string systemId, string money)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["orderId"] = orderId,
            ["systemId"] = systemId,
            ["money"] = money,
            ["success"] = "true"
        };
        var sign = RechargeNotifySecurity.ComputeMockMd5(fields, "test-secret");
        return $"orderId={orderId}&systemId={systemId}&money={money}&success=true&sign={sign}";
    }

    private void InsertOrder(string id)
    {
        _db.Insertable(new RechargeOrder
        {
            ID = id,
            SiteID = 1001,
            UserID = _uid,
            Money = 100m,
            Fee = 0,
            Cost = 0,
            CreateAt = DateTime.UtcNow,
            PayID = _payId,
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

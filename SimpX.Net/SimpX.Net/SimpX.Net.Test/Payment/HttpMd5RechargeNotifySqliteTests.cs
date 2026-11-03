using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// http-md5 充值回调插件（自定义字段名 + 验签）。
/// </summary>
public sealed class HttpMd5RechargeNotifySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly RechargeNotifyService _svc;
    private readonly int _payId;
    private readonly int _uid;

    /// <summary>
    /// 通道 Setting：Plugin=http-md5，自定义 order_id/amount/trade_no。
    /// </summary>
    public HttpMd5RechargeNotifySqliteTests()
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

        var setting =
            "Plugin=http-md5&Key=md5-secret&OrderField=order_id&MoneyField=amount&TradeNoField=trade_no&SuccessField=status&SuccessValue=1";
        _payId = _db.Insertable(new SitePayment
        {
            SiteID = 1001,
            CateID = 1,
            Name = "http-md5",
            Description = "",
            Remark = "",
            Sort = 0,
            Status = 1,
            SettingString = setting,
            SysID = 0,
            MinMoney = 0,
            MaxMoney = 0,
            Amount = "",
            Agent = "",
            Fee = 0,
            Cost = 0,
            IsDeleted = false,
            IsNotify = false,
            IsERC = "",
            IsTRC = "",
            DownloadLink = "",
            Currency = 1
        }).ExecuteReturnIdentity();

        _uid = _db.Insertable(BuildUser("u_md5")).ExecuteReturnIdentity();
        _db.Insertable(new RechargeOrder
        {
            ID = "RO-MD5-1",
            SiteID = 1001,
            UserID = _uid,
            Money = 50m,
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

        var check = new RechargeOrderCheckService(new TestBizDb(_db), new FixedSiteAccessor(1001, "md5.test"), _money, new MemoryLedgerSideEffectQueues());
        var mock = new MockFormRechargeNotifyPlugin();
        var router = new RechargeNotifyPluginRouter(
            new IRechargeNotifyPlugin[] { mock, new HttpMd5RechargeNotifyPlugin() },
            mock);
        _svc = new RechargeNotifyService(new TestBizDb(_db), check, router);
    }

    /// <summary>
    /// 自定义字段 + 正确签入账。
    /// </summary>
    [Fact]
    public async Task HttpMd5_custom_fields_credits()
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["order_id"] = "RO-MD5-1",
            ["amount"] = "50",
            ["trade_no"] = "T-100",
            ["status"] = "1"
        };
        var sign = RechargeNotifySecurity.ComputeMockMd5(fields, "md5-secret");
        var body = $"order_id=RO-MD5-1&amount=50&trade_no=T-100&status=1&sign={sign}";
        var ack = await _svc.NotifyAsync(_payId, body, clientIp: "127.0.0.1");
        Assert.Equal("SUCCESS", ack);
        Assert.True((await _db.Queryable<RechargeOrder>().InSingleAsync("RO-MD5-1")).IsPay);
    }

    /// <summary>
    /// Plugin=http-md5 但无 Key 时拒绝。
    /// </summary>
    [Fact]
    public async Task HttpMd5_without_key_rejects()
    {
        await _db.Updateable<SitePayment>()
            .SetColumns(p => new SitePayment { SettingString = "Plugin=http-md5" })
            .Where(p => p.ID == _payId)
            .ExecuteCommandAsync();
        var ack = await _svc.NotifyAsync(_payId, "order_id=RO-MD5-1&amount=50&trade_no=T&status=1&sign=x", "127.0.0.1");
        Assert.Equal("通道未配置 http-md5 Key", ack);
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


    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host) { SiteId = siteId; Host = host; }
        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

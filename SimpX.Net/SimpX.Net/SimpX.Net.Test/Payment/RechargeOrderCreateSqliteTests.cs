using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 充值建单 + Stub 出站（内存 SQLite）。禁写 BetWin30。
/// </summary>
public sealed class RechargeOrderCreateSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "roc.create");
    private readonly RechargeOrderCreateService _svc;
    private readonly int _uid;
    private readonly int _lockedUid;
    private int _payId;

    /// <summary>
    /// 建表并植入会员与通道。
    /// </summary>
    public RechargeOrderCreateSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site", "site_Payment"], []);
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

        _uid = InsertUser("u_ok", lockFlags: 0);
        _lockedUid = InsertUser("u_lock", lockFlags: RechargeOrderCreateService.LockRechargeFlag);
        _payId = _db.Insertable(new SitePayment
        {
            SiteID = 1001,
            CateID = 1,
            Name = "mock-pay",
            Description = "",
            Remark = "",
            Status = RechargeOrderCreateService.PaymentStatusNormal,
            SettingString = "Plugin=mock",
            SysID = 0,
            MinMoney = 10,
            MaxMoney = 1000,
            Fee = 0.01m,
            MinFee = 0,
            MaxFee = 0,
            Cost = 0,
            MinCost = 0,
            MaxCost = 0,
            Currency = 2,
            IsDeleted = false
        }).ExecuteReturnIdentity();

        var gateway = new RechargeGatewayRouter(new StubRechargeGatewayPlugin(), Array.Empty<IRechargeGatewayPlugin>());
        _svc = new RechargeOrderCreateService(new TestBizDb(_db), _site, gateway, new RechargeDuplicateAmountChecker(new LegacyLedgerRedisHolder()));
    }

    /// <summary>
    /// 正常建单：落库未到账，Stub 返回支付地址。
    /// </summary>
    [Fact]
    public async Task Create_success_inserts_order_and_returns_pay_url()
    {
        var output = await _svc.CreateAsync(new RechargeOrderCreateInput
        {
            UserID = _uid,
            PayID = _payId,
            Money = 100m,
            AccountName = "Tom",
            ClientIP = "1.2.3.4"
        });

        Assert.False(string.IsNullOrWhiteSpace(output.OrderID));
        Assert.Contains("pay.local/mock", output.PayUrl);
        Assert.StartsWith("MOCK-", output.SystemId);

        var order = await _db.Queryable<RechargeOrder>().InSingleAsync(output.OrderID);
        Assert.Equal(100m, order.Money);
        Assert.Equal(1m, order.Fee);
        Assert.False(order.IsPay);
        Assert.Equal(_payId, order.PayID);
        Assert.Equal(output.SystemId, order.SystemID);
        Assert.Equal("1.2.3.4", order.IP);
    }

    /// <summary>
    /// 禁止充值锁拒绝建单。
    /// </summary>
    [Fact]
    public async Task Create_rejects_when_recharge_locked()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.CreateAsync(new RechargeOrderCreateInput
            {
                UserID = _lockedUid,
                PayID = _payId,
                Money = 50m
            }));
        Assert.Equal("#禁止充值#", ex.Message);
        Assert.Equal(0, await _db.Queryable<RechargeOrder>().CountAsync());
    }

    /// <summary>
    /// 低于最低额度拒绝。
    /// </summary>
    [Fact]
    public async Task Create_rejects_below_min_money()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.CreateAsync(new RechargeOrderCreateInput
            {
                UserID = _uid,
                PayID = _payId,
                Money = 1m
            }));
        Assert.Contains("最低", ex.Message);
    }

    /// <summary>
    /// 配置了未知 Plugin 且无匹配插件时失败（订单仍可能已落库前失败——本例在网关阶段前已选通道，网关失败在插单后）。
    /// </summary>
    [Fact]
    public async Task Create_fails_when_plugin_missing()
    {
        var payId = _db.Insertable(new SitePayment
        {
            SiteID = 1001,
            CateID = 1,
            Name = "bad-plugin",
            Description = "",
            Remark = "",
            Status = RechargeOrderCreateService.PaymentStatusNormal,
            SettingString = "Plugin=vendor-xyz&Key=k",
            SysID = 0,
            MinMoney = 1,
            MaxMoney = 1000,
            Currency = 2,
            IsDeleted = false
        }).ExecuteReturnIdentity();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.CreateAsync(new RechargeOrderCreateInput
            {
                UserID = _uid,
                PayID = payId,
                Money = 20m
            }));
        Assert.Contains("vendor-xyz", ex.Message);
        // 对齐旧站：订单先落库再调网关
        Assert.Equal(1, await _db.Queryable<RechargeOrder>().Where(o => o.PayID == payId).CountAsync());
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    private int InsertUser(string userName, int lockFlags) =>
        _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = userName,
            NickName = userName,
            AccountName = "",
            Password = "x",
            PayPassword = "",
            CreateAt = DateTime.UtcNow,
            Face = "",
            RegIP = "",
            Lock = lockFlags,
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
            Unbalanced = 0
        }).ExecuteReturnIdentity();

    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        private readonly int _siteId;
        private readonly string _host;

        public FixedSiteAccessor(int siteId, string host)
        {
            _siteId = siteId;
            _host = host;
        }

        public int? SiteId => _siteId;
        public string Host => _host;
        public int RequireSiteId() => _siteId;
    }
}

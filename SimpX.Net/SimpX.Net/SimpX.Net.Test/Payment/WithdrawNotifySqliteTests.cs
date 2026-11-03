using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 匿名代付回调（内存 SQLite + Stub 验签 + 假账本）。禁写 BetWin30。
/// </summary>
public sealed class WithdrawNotifySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly WithdrawNotifyService _svc;
    private readonly int _orderId;
    private readonly int _channelId;

    /// <summary>
    /// 建付款中订单与 Stub 代付渠道；站点访问器故意不定站，证明按渠道 SiteID 完结。
    /// </summary>
    public WithdrawNotifySqliteTests()
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
            CREATE TABLE IF NOT EXISTS [usr_WithdrawOrderLog] (
              [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [OrderID] INTEGER NOT NULL,
              [Status] INTEGER NOT NULL,
              [CreateAt] TEXT NOT NULL,
              [AdminID] INTEGER NOT NULL DEFAULT 0,
              [IP] TEXT
            );
            CREATE TABLE IF NOT EXISTS [site_WithdrawChannel] (
              [ChannelID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [SysID] INTEGER NOT NULL DEFAULT 0,
              [Name] TEXT,
              [Setting] TEXT,
              [QuerySetting] TEXT,
              [IsOpen] INTEGER NOT NULL DEFAULT 1,
              [Sort] INTEGER NOT NULL DEFAULT 0,
              [CreateAt] TEXT,
              [IsDeleted] INTEGER NOT NULL DEFAULT 0,
              [Cost] REAL NOT NULL DEFAULT 0,
              [MinCost] REAL NOT NULL DEFAULT 0,
              [MaxCost] REAL NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0
            );
            """);

        var uid = _db.Insertable(BuildUser("u_wd_notify")).ExecuteReturnIdentity();
        _channelId = _db.Insertable(new WithdrawChannel
        {
            SiteID = 1001,
            SysID = 99,
            Name = "Stub代付回调",
            Setting = "",
            QuerySetting = "",
            IsOpen = true,
            Sort = 1,
            CreateAt = DateTime.UtcNow,
            IsDeleted = false,
            Cost = 0,
            MinCost = 0,
            MaxCost = 0,
            Currency = 1
        }).ExecuteReturnIdentity();

        _orderId = _db.Insertable(new WithdrawOrder
        {
            SiteID = 1001,
            UserID = uid,
            CreateAt = DateTime.UtcNow,
            Bank = 1,
            AccountName = "测",
            AccountNumber = "6222021234567890",
            Money = 50,
            Status = WithdrawOrderOperateService.StatusInPayment,
            Fee = 2,
            Paid = 48,
            PayAt = DateTime.UtcNow,
            ChannelID = _channelId,
            Description = "paymenting",
            Mark = "",
            IP = "",
            Cost = 0,
            SystemID = $"STUB0",
            AdminID = 1,
            CashierID = 0,
            Currency = 1,
            FromCurrency = 1,
            AuditAt = DateTime.UtcNow
        }).ExecuteReturnIdentity();

        // 回写真实 SystemID
        var order = _db.Queryable<WithdrawOrder>().InSingle(_orderId);
        order.SystemID = $"STUB{_orderId}";
        _db.Updateable(order).UpdateColumns(o => new { o.SystemID }).ExecuteCommand();

        var site = new FixedSiteAccessor(null, "wd.notify");
        var operate = new WithdrawOrderOperateService(
            new TestBizDb(_db),
            site,
            _money,
            new WithdrawGatewayRouter(new StubWithdrawGateway(), Array.Empty<IWithdrawGatewayPlugin>()));
        _svc = new WithdrawNotifyService(new TestBizDb(_db), operate);
    }

    /// <summary>
    /// Stub 验签成功：付款中 → 已到账，写提现账变。
    /// </summary>
    [Fact]
    public async Task Notify_stub_success_finishes_order()
    {
        var body = $"orderId={_orderId}&money=48&success=true&sign=STUB";
        var ack = await _svc.NotifyAsync(_channelId, body, "127.0.0.1");
        Assert.Equal("SUCCESS", ack);

        var order = await _db.Queryable<WithdrawOrder>().InSingleAsync(_orderId);
        Assert.Equal(WithdrawOrderOperateService.StatusFinish, order.Status);
        Assert.Equal(48m, order.Paid);
        Assert.Contains(_money.Logs, l => l.Type == WithdrawOrderOperateService.MoneyTypeWithdraw && l.Money == -48m);
        Assert.Contains(_money.Logs, l => l.Type == WithdrawOrderOperateService.MoneyTypeWithdrawFee && l.Money == -2m);
        Assert.Single(_money.WithdrawUnlocks);
    }

    /// <summary>
    /// 错误签名拒绝，状态不变。
    /// </summary>
    [Fact]
    public async Task Notify_bad_sign_rejects()
    {
        var ack = await _svc.NotifyAsync(
            _channelId,
            $"orderId={_orderId}&money=48&success=true&sign=BAD",
            "127.0.0.1");
        Assert.Equal("#回调验签失败#", ack);
        var order = await _db.Queryable<WithdrawOrder>().InSingleAsync(_orderId);
        Assert.Equal(WithdrawOrderOperateService.StatusInPayment, order.Status);
        Assert.Empty(_money.Logs);
    }

    /// <summary>
    /// success=false：回待处理，不解锁不入账。
    /// </summary>
    [Fact]
    public async Task Notify_fail_flag_sets_wait()
    {
        var ack = await _svc.NotifyAsync(
            _channelId,
            $"orderId={_orderId}&money=48&success=false&sign=STUB",
            "127.0.0.1");
        Assert.Equal("SUCCESS", ack);
        var order = await _db.Queryable<WithdrawOrder>().InSingleAsync(_orderId);
        Assert.Equal(WithdrawOrderOperateService.StatusWait, order.Status);
        Assert.Empty(_money.Logs);
        Assert.Empty(_money.WithdrawUnlocks);
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

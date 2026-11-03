using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 提现审核/人工出款（内存 SQLite + 假账本）。禁写 BetWin30。
/// </summary>
public sealed class WithdrawOrderOperateSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "wd.op");
    private readonly RecordingMoneyLedger _money = new();
    private readonly WithdrawOrderOperateService _svc;
    private readonly int _orderId;
    private readonly int _channelId;

    /// <summary>
    /// 建订单、渠道、待审单。
    /// </summary>
    public WithdrawOrderOperateSqliteTests()
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

        var uid = _db.Insertable(BuildUser("u_wd_op")).ExecuteReturnIdentity();
        _channelId = _db.Insertable(new WithdrawChannel
        {
            SiteID = 1001,
            SysID = 0,
            Name = "人工出款",
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
            Status = WithdrawOrderOperateService.StatusNone,
            Fee = 0,
            Paid = 0,
            PayAt = DateTime.UtcNow,
            ChannelID = 0,
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
        }).ExecuteReturnIdentity();

        _svc = new WithdrawOrderOperateService(
            new TestBizDb(_db),
            _site,
            _money,
            new WithdrawGatewayRouter(new StubWithdrawGateway(), Array.Empty<IWithdrawGatewayPlugin>()));
    }

    /// <summary>
    /// 审核通过进入待处理。
    /// </summary>
    [Fact]
    public async Task Check_agree_sets_wait()
    {
        await _svc.CheckAsync(new WithdrawOrderCheckInput { OrderID = _orderId, Action = "Agree" }, 7);
        var order = await _db.Queryable<WithdrawOrder>().InSingleAsync(_orderId);
        Assert.Equal(WithdrawOrderOperateService.StatusWait, order.Status);
        Assert.Equal(7, order.AdminID);
    }

    /// <summary>
    /// 审核拒绝解锁提现锁。
    /// </summary>
    [Fact]
    public async Task Check_reject_unlocks_withdraw()
    {
        await _svc.CheckAsync(new WithdrawOrderCheckInput { OrderID = _orderId, Action = "Reject" }, 1);
        Assert.Single(_money.WithdrawUnlocks);
        var order = await _db.Queryable<WithdrawOrder>().InSingleAsync(_orderId);
        Assert.Equal(WithdrawOrderOperateService.StatusFailed, order.Status);
    }

    /// <summary>
    /// 人工出款写提现与手续费账变。
    /// </summary>
    [Fact]
    public async Task Exec_manual_payout_writes_ledger()
    {
        await _svc.CheckAsync(new WithdrawOrderCheckInput { OrderID = _orderId, Action = "Agree" }, 1);
        await _svc.ExecAsync(new WithdrawOrderExecInput
        {
            OrderID = _orderId,
            ChannelID = _channelId,
            Action = "Agree",
            Fee = 2
        }, 9);

        Assert.Contains(_money.Logs, l => l.Type == WithdrawOrderOperateService.MoneyTypeWithdraw && l.Money == -48m);
        Assert.Contains(_money.Logs, l => l.Type == WithdrawOrderOperateService.MoneyTypeWithdrawFee && l.Money == -2m);
        var order = await _db.Queryable<WithdrawOrder>().InSingleAsync(_orderId);
        Assert.Equal(WithdrawOrderOperateService.StatusFinish, order.Status);
        Assert.Equal(48m, order.Paid);
        Assert.Equal(9, order.CashierID);
    }

    /// <summary>
    /// 代付渠道提交后进入付款中，确认后才账变。
    /// </summary>
    [Fact]
    public async Task Exec_gateway_then_confirm_writes_ledger()
    {
        var gwChannel = _db.Insertable(new WithdrawChannel
        {
            SiteID = 1001,
            SysID = 99,
            Name = "Stub代付",
            Setting = "",
            QuerySetting = "",
            IsOpen = true,
            Sort = 2,
            CreateAt = DateTime.UtcNow,
            IsDeleted = false,
            Cost = 0,
            MinCost = 0,
            MaxCost = 0,
            Currency = 1
        }).ExecuteReturnIdentity();

        await _svc.CheckAsync(new WithdrawOrderCheckInput { OrderID = _orderId, Action = "Agree" }, 1);
        await _svc.ExecAsync(new WithdrawOrderExecInput
        {
            OrderID = _orderId,
            ChannelID = gwChannel,
            Action = "Agree",
            Fee = 0
        }, 2);

        var mid = await _db.Queryable<WithdrawOrder>().InSingleAsync(_orderId);
        Assert.Equal(WithdrawOrderOperateService.StatusInPayment, mid.Status);
        Assert.Equal($"STUB{_orderId}", mid.SystemID);
        Assert.Empty(_money.Logs);

        await _svc.ConfirmGatewayAsync(_orderId, 3);
        Assert.Contains(_money.Logs, l => l.Type == WithdrawOrderOperateService.MoneyTypeWithdraw);
        var done = await _db.Queryable<WithdrawOrder>().InSingleAsync(_orderId);
        Assert.Equal(WithdrawOrderOperateService.StatusFinish, done.Status);
    }

    /// <summary>
    /// 代付失败回待处理且不解锁（对齐旧 1012）。
    /// </summary>
    [Fact]
    public async Task FailGateway_sets_wait_without_unlock()
    {
        var gwChannel = _db.Insertable(new WithdrawChannel
        {
            SiteID = 1001,
            SysID = 99,
            Name = "Stub代付2",
            Setting = "",
            QuerySetting = "",
            IsOpen = true,
            Sort = 3,
            CreateAt = DateTime.UtcNow,
            IsDeleted = false,
            Cost = 0,
            MinCost = 0,
            MaxCost = 0,
            Currency = 1
        }).ExecuteReturnIdentity();

        await _svc.CheckAsync(new WithdrawOrderCheckInput { OrderID = _orderId, Action = "Agree" }, 1);
        await _svc.ExecAsync(new WithdrawOrderExecInput
        {
            OrderID = _orderId,
            ChannelID = gwChannel,
            Action = "Agree",
            Fee = 0
        }, 2);

        await _svc.FailGatewayAsync(_orderId, 5, "网关失败");
        var order = await _db.Queryable<WithdrawOrder>().InSingleAsync(_orderId);
        Assert.Equal(WithdrawOrderOperateService.StatusWait, order.Status);
        Assert.Empty(_money.WithdrawUnlocks);
        Assert.Empty(_money.Logs);
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

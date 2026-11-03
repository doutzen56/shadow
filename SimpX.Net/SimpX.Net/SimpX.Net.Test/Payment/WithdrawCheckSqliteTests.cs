using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>提现定时核对：120 秒门槛与空网关跳过（内存 SQLite）。</summary>
public sealed class WithdrawCheckSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly WithdrawCheckService _svc;

    public WithdrawCheckSqliteTests()
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

        var gateway = new WithdrawGatewayRouter(new StubWithdrawGateway(), Array.Empty<IWithdrawGatewayPlugin>());
        var operate = new WithdrawOrderOperateService(
            new TestBizDb(_db),
            new FixedSiteAccessor(1002, "wd.check"),
            new RecordingMoneyLedger(),
            gateway);
        _svc = new WithdrawCheckService(
            new TestBizDb(_db),
            gateway,
            operate,
            NullLogger<WithdrawCheckService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    [Fact]
    public async Task Skip_When_InPaymentLog_YoungerThan_120s()
    {
        var id = SeedOrder(logAgeSeconds: 30, withGateway: true);
        var n = await _svc.CheckInPaymentAsync();
        Assert.Equal(1, n);
        var order = await _db.Queryable<WithdrawOrder>().InSingleAsync(id);
        Assert.Equal(WithdrawOrderOperateService.StatusInPayment, order.Status);
    }

    [Fact]
    public async Task Skip_When_No_Gateway_Config()
    {
        var id = SeedOrder(logAgeSeconds: 200, withGateway: false);
        var n = await _svc.CheckInPaymentAsync();
        Assert.Equal(1, n);
        var order = await _db.Queryable<WithdrawOrder>().InSingleAsync(id);
        Assert.Equal(WithdrawOrderOperateService.StatusInPayment, order.Status);
    }

    private int SeedOrder(int logAgeSeconds, bool withGateway)
    {
        var channelId = _db.Insertable(new WithdrawChannel
        {
            SiteID = 1002,
            SysID = withGateway ? 1 : 0,
            Name = "t",
            Setting = withGateway
                ? "{\"Plugin\":\"http-md5\",\"Gateway\":\"http://x\",\"Key\":\"k\"}"
                : "",
            QuerySetting = withGateway
                ? "{\"Plugin\":\"http-md5\",\"Gateway\":\"http://x\",\"QueryGateway\":\"http://q\",\"Key\":\"k\"}"
                : "",
            IsOpen = true,
            Sort = 1,
            CreateAt = DateTime.UtcNow,
            IsDeleted = false
        }).ExecuteReturnIdentity();

        var orderId = _db.Insertable(new WithdrawOrder
        {
            SiteID = 1002,
            UserID = 1,
            CreateAt = DateTime.UtcNow.AddMinutes(-10),
            Bank = 1,
            AccountName = "a",
            AccountNumber = "1",
            Money = 100,
            Status = WithdrawOrderOperateService.StatusInPayment,
            Fee = 0,
            Paid = 0,
            PayAt = DateTime.UtcNow,
            ChannelID = channelId,
            Description = "",
            Mark = "",
            IP = "",
            Cost = 0,
            SystemID = "",
            AdminID = 0,
            CashierID = 0,
            Currency = 2,
            FromCurrency = 2,
            AuditAt = DateTime.UtcNow
        }).ExecuteReturnIdentity();

        _db.Insertable(new WithdrawOrderLog
        {
            SiteID = 1002,
            OrderID = orderId,
            Status = WithdrawOrderOperateService.StatusInPayment,
            CreateAt = DateTime.UtcNow.AddSeconds(-logAgeSeconds),
            AdminID = 0,
            IP = "127.0.0.1"
        }).ExecuteCommand();

        return orderId;
    }

    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005");
    }
}

using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SqlSugar;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 支付 C3/C4：Demo 下单/查单/回调入账（内存库 + 假账本）。
/// </summary>
public sealed class PaymentOrderNotifyCreditSqliteTests : IDisposable
{
    private readonly SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly PaymentOrderService _svc;

    /// <summary>
    /// 建支付单表。
    /// </summary>
    public PaymentOrderNotifyCreditSqliteTests()
    {
        _db = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = "DataSource=:memory:;Cache=Shared",
            IsAutoCloseConnection = false,
            MoreSettings = new ConnMoreSettings { IsAutoRemoveDataCache = true }
        });
        _db.Ado.Connection.Open();
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [pay_payment_order] (
              [Id] INTEGER PRIMARY KEY NOT NULL,
              [TenantId] INTEGER,
              [PaymentOrderNo] TEXT,
              [BusinessOrderNo] TEXT,
              [ChannelCode] TEXT,
              [Amount] REAL,
              [Subject] TEXT,
              [Status] INTEGER,
              [ChannelTradeNo] TEXT,
              [Attach] TEXT,
              [ExtraJson] TEXT,
              [CreateTime] TEXT,
              [UpdateTime] TEXT,
              [CreateUserId] INTEGER,
              [CreateUserName] TEXT,
              [UpdateUserId] INTEGER,
              [UpdateUserName] TEXT
            );
            """);

        _svc = new PaymentOrderService(
            _db,
            [new TestDemoChannel()],
            new FixedTenantAccessor(1300000000001),
            _money);
    }

    /// <summary>下单 → 查单 → 回调成功入账一次；重复回调不双加。</summary>
    [Fact]
    public async Task Create_get_notify_credits_once()
    {
        var created = await _svc.Create(new PaymentCreateRequest
        {
            BusinessOrderNo = "BIZ-001",
            Amount = 12.34m,
            Subject = "测试充值",
            ChannelCode = "demo",
            SiteId = 1001,
            UserId = 88,
            Currency = 1,
            MoneyType = 1
        });
        Assert.False(string.IsNullOrWhiteSpace(created.PaymentOrderNo));
        Assert.Equal(PaymentStatus.Created, created.Status);
        Assert.True(created.PayParams.ContainsKey("cashierUrl"));

        var got = await _svc.Get(created.PaymentOrderNo);
        Assert.Equal(12.34m, got.Amount);

        var ack = await _svc.Notify("demo", $"paymentOrderNo={Uri.EscapeDataString(created.PaymentOrderNo)}&success=true");
        Assert.Equal("SUCCESS", ack);

        var paid = await _svc.Get(created.PaymentOrderNo);
        Assert.Equal(PaymentStatus.Paid, paid.Status);
        Assert.Single(_money.Logs);
        Assert.Equal(1001, _money.Logs[0].SiteId);
        Assert.Equal(88, _money.Logs[0].UserId);
        Assert.Equal(12.34m, _money.Logs[0].Money);
        Assert.Equal(1, _money.Logs[0].Type);

        await _svc.Notify("demo", $"paymentOrderNo={Uri.EscapeDataString(created.PaymentOrderNo)}&success=true");
        Assert.Single(_money.Logs);
    }

    /// <summary>无会员 Attach 时只改状态，不入账。</summary>
    [Fact]
    public async Task Notify_without_user_skips_credit()
    {
        var created = await _svc.Create(new PaymentCreateRequest
        {
            BusinessOrderNo = "BIZ-002",
            Amount = 1m,
            ChannelCode = "demo"
        });
        await _svc.Notify("demo", $"paymentOrderNo={Uri.EscapeDataString(created.PaymentOrderNo)}&success=true");
        Assert.Empty(_money.Logs);
        Assert.Equal(PaymentStatus.Paid, (await _svc.Get(created.PaymentOrderNo)).Status);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db?.Ado.Connection?.Close();
        _db?.Dispose();
    }

    /// <summary>假账本。</summary>

    /// <summary>定租。</summary>
    private sealed class FixedTenantAccessor : ICurrentTenantAccessor
    {
        public FixedTenantAccessor(long tenantId) => TenantId = tenantId;

        public long? TenantId { get; }
        public string Host => "pay.test";
        public long RequireTenantId() => TenantId ?? throw new InvalidOperationException("Z1005");
    }

    /// <summary>测试用 Demo 渠道（与 Channels DemoPaymentChannel 语义一致）。</summary>
    private sealed class TestDemoChannel : IPaymentChannel
    {
        public string ChannelCode => "demo";

        public Task<Dictionary<string, string>> CreateAsync(PaymentOrder order, CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, string>
            {
                ["cashierUrl"] = $"/demo-pay/checkout?orderNo={order.PaymentOrderNo}",
                ["amount"] = order.Amount.ToString("0.00")
            });

        public Task<PaymentNotifyContext> ParseNotifyAsync(string rawBody, CancellationToken cancellationToken = default)
        {
            var map = rawBody.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]), StringComparer.OrdinalIgnoreCase);
            if (!map.TryGetValue("paymentOrderNo", out var orderNo) || string.IsNullOrWhiteSpace(orderNo))
                throw new InvalidOperationException("缺少 paymentOrderNo");
            var success = !map.TryGetValue("success", out var s) ||
                          s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";
            return Task.FromResult(new PaymentNotifyContext
            {
                ChannelCode = ChannelCode,
                PaymentOrderNo = orderNo,
                ChannelTradeNo = $"DEMO-{orderNo}",
                Status = success ? PaymentStatus.Paid : PaymentStatus.Failed,
                RawBody = rawBody
            });
        }
    }
}

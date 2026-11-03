using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 真库冒烟：插未到账单 + Mock 回调改 IsPay（假账本）；资金用直调 SP。
/// </summary>
public sealed class BetWin30RechargeNotifySmokeTests
{
    /// <summary>
    /// Notify SUCCESS 后订单到账。
    /// </summary>
    [Fact]
    public async Task Notify_marks_paid_for_test1031()
    {
        var cs = Environment.GetEnvironmentVariable("SIMPX_BETWIN30_CS");
        if (string.IsNullOrWhiteSpace(cs))
        {
            cs =
                "Server=43.198.37.153,21433;Database=BetWin30;User Id=sa;Password=Admin@123;Encrypt=True;TrustServerCertificate=True;";
        }

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = cs,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        try { await db.Ado.GetScalarAsync("SELECT 1"); }
        catch (Exception ex) { Assert.Fail($"BetWin30 不可达：{ex.Message}"); return; }

        var users = await db.Queryable<User>()
            .Where(u => u.SiteID == 1001 && u.UserName == "test1031").Take(1).ToListAsync();
        Assert.True(users.Count == 1);
        var user = users[0];

        var pays = await db.Queryable<SitePayment>()
            .Where(p => p.SiteID == 1001 && !p.IsDeleted && p.Status == 1)
            .OrderBy(p => p.ID)
            .Take(1)
            .ToListAsync();
        Assert.True(pays.Count == 1, "站点 1001 缺少可用支付通道");
        var payId = pays[0].ID;

        var before = (await db.Queryable<UserMoney>()
            .Where(w => w.SiteID == 1001 && w.UserID == user.ID && w.Currency == 1).Take(1).ToListAsync())[0].TotalMoney;

        var orderId = "R" + DateTime.UtcNow.ToString("yyMMddHHmmssfff");
        var systemId = "G" + Guid.NewGuid().ToString("N")[..10];
        db.Insertable(new RechargeOrder
        {
            ID = orderId,
            SiteID = 1001,
            UserID = user.ID,
            Money = 0.01m,
            Fee = 0,
            Cost = 0,
            CreateAt = DateTime.Now,
            PayID = payId,
            IsPay = false,
            PayAt = DateTime.Now,
            Paid = 0,
            SystemID = "",
            AdminID = 0,
            Count = 0,
            Remark = "notify-smoke",
            IP = "127.0.0.1",
            Currency = 1,
            FromCurrency = 1,
            IsCheckFail = false,
            ActiveID = "",
            IsDeleted = false,
            IsProxyRecharge = false,
            PayAccount = ""
        }).ExecuteCommand();

        var fake = new RecordingMoneyLedger();
        var check = new RechargeOrderCheckService(new TestBizDb(db), new FixedSiteAccessor(null, "x"), fake, new MemoryLedgerSideEffectQueues());
        var svc = new RechargeNotifyService(new TestBizDb(db), check, new RechargeNotifyPluginRouter(new IRechargeNotifyPlugin[] { new MockFormRechargeNotifyPlugin(), new HttpMd5RechargeNotifyPlugin() }, new MockFormRechargeNotifyPlugin()));
        var body = $"orderId={Uri.EscapeDataString(orderId)}&systemId={Uri.EscapeDataString(systemId)}&money=0.01&success=true";
        var ack = await svc.NotifyAsync(payId, body);
        Assert.Equal("SUCCESS", ack);

        var order = await db.Queryable<RechargeOrder>().InSingleAsync(orderId);
        Assert.True(order.IsPay);
        Assert.Equal(systemId, order.SystemID);
        Assert.Single(fake.Logs);

        var logId = Math.Abs((int)(DateTime.UtcNow.Ticks % 1_000_000_000));
        var rows = await db.Ado.ExecuteCommandAsync(
            "EXEC usr_AddMoneyLog @LogID,@SiteID,@UserID,@Money,@Type,@SubType,@SourceID,@IP,@Description,@Currency",
            new List<SugarParameter>
            {
                new("@LogID", logId),
                new("@SiteID", 1001),
                new("@UserID", user.ID),
                new("@Money", 0.01m),
                new("@Type", 1),
                new("@SubType", (byte)0),
                new("@SourceID", orderId.Length > 20 ? orderId[..20] : orderId),
                new("@IP", ""),
                new("@Description", "notify-smoke"),
                new("@Currency", 1)
            });
        Assert.True(rows > 0);
        var after = (await db.Queryable<UserMoney>()
            .Where(w => w.SiteID == 1001 && w.UserID == user.ID && w.Currency == 1).Take(1).ToListAsync())[0].TotalMoney;
        Assert.True(after >= before + 0.01m, $"before={before} after={after}");
    }


    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host) { SiteId = siteId; Host = host; }
        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

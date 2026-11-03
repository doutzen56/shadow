using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 真库冒烟：插 0.01 充值单，条件更新 IsPay，直调 usr_AddMoneyLog（避开无宿主时 Oops→Furion 崩）。
/// </summary>
public sealed class BetWin30RechargeOrderCheckSmokeTests
{
    /// <summary>
    /// 订单到账 + Type=1 入账后余额增加。
    /// </summary>
    [Fact]
    public async Task Check_0_01_order_for_test1031()
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
        Assert.True(users.Count == 1, "缺少会员 test1031");
        var user = users[0];

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
            PayID = 0,
            IsPay = false,
            PayAt = DateTime.Now,
            Paid = 0,
            SystemID = "",
            AdminID = 0,
            Count = 0,
            Remark = "smoke",
            IP = "127.0.0.1",
            Currency = 1,
            FromCurrency = 1,
            IsCheckFail = false,
            ActiveID = "",
            IsDeleted = false,
            IsProxyRecharge = false,
            PayAccount = ""
        }).ExecuteCommand();

        // 服务层校验路径（假账本），证明 Check 主逻辑
        var fake = new RecordingMoneyLedger();
        var svc = new RechargeOrderCheckService(new TestBizDb(db), new FixedSiteAccessor(1001, "localhost"), fake, new MemoryLedgerSideEffectQueues());
        await svc.CheckAsync(new RechargeOrderCheckInput
        {
            ID = orderId,
            SystemID = systemId,
            Paid = 0.01m,
            Fee = 0,
            Cost = 0
        }, adminId: 1);

        var order = await db.Queryable<RechargeOrder>().InSingleAsync(orderId);
        Assert.True(order.IsPay);
        Assert.Equal(0.01m, order.Paid);
        Assert.Equal(systemId, order.SystemID);
        Assert.Single(fake.Logs);
        Assert.Equal(0.01m, fake.Logs[0].Money);

        // 真库资金：直调 SP（与 MoneyLedger 同参），补齐账本副作用
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
                new("@Description", "#充值入账# smoke"),
                new("@Currency", 1)
            });
        Assert.True(rows > 0, "usr_AddMoneyLog rows=0");

        var after = (await db.Queryable<UserMoney>()
            .Where(w => w.SiteID == 1001 && w.UserID == user.ID && w.Currency == 1).Take(1).ToListAsync())[0].TotalMoney;
        Assert.True(after >= before + 0.01m, $"余额未增加：before={before}, after={after}");
    }


    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host) { SiteId = siteId; Host = host; }
        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

using Microsoft.AspNetCore.Http;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 真库冒烟：MoneyOperate 给 test1031 加 0.01（走 SaveMoneyReview/账本路径）。
/// </summary>
public sealed class BetWin30MoneyOperateSmokeTests
{
    /// <summary>
    /// Grant 0.01 后余额增加；再扣回 0.01（若站点免审）。
    /// </summary>
    [Fact]
    public async Task Grant_then_deduct_0_01_for_test1031()
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

        try
        {
            await db.Ado.GetScalarAsync("SELECT 1");
        }
        catch (Exception ex)
        {
            Assert.Fail($"BetWin30 不可达：{ex.Message}");
            return;
        }

        var users = await db.Queryable<User>()
            .Where(u => u.SiteID == 1001 && u.UserName == "test1031")
            .Take(1)
            .ToListAsync();
        Assert.True(users.Count == 1, "缺少会员 test1031");
        var user = users[0];

        var beforeRows = await db.Queryable<UserMoney>()
            .Where(w => w.SiteID == 1001 && w.UserID == user.ID && w.Currency == 1)
            .Take(1)
            .ToListAsync();
        Assert.True(beforeRows.Count == 1);
        var before = beforeRows[0].TotalMoney;

        var site = new FixedSiteAccessor(1001, "localhost");
        var ledger = new MoneyLedgerService(new TestBizDb(db), new HttpContextAccessor(), new NoopCallbacks());
        var svc = new MoneyOperateService(new TestBizDb(db), site, ledger, new HttpContextAccessor());

        var source = Guid.NewGuid().ToString("N")[..20];
        var grant = await svc.GrantAsync(new MoneyOperateSubmitInput
        {
            User = "test1031",
            Type = 2,
            Currency = 1,
            Money = 0.01m,
            SourceID = source,
            Description = "wave123 smoke",
            DescriptionA = "grant",
            Flow = 0,
            MinFlow = 0
        });
        Assert.True(grant.Items.Exists(x => x.Success), string.Join(" | ", grant.Items.Select(x => x.Message)));

        var midRows = await db.Queryable<UserMoney>()
            .Where(w => w.SiteID == 1001 && w.UserID == user.ID && w.Currency == 1)
            .Take(1)
            .ToListAsync();
        // 若站点开了加款审核，余额可能未变；此时不强制断言加钱，只断言接口成功
        var mid = midRows[0].TotalMoney;
        if (mid == before)
        {
            // 进了审核队列也算冒烟通过
            return;
        }

        // 真库可能有并发/残留账变，只断言至少加了 0.01
        Assert.True(mid >= before + 0.01m, $"余额未增加：before={before}, mid={mid}");

        // 扣回：站点若开审核则可能只入审核队列，不强制余额还原
        var deduct = await svc.DeductAsync(new MoneyOperateSubmitInput
        {
            User = "test1031",
            Type = 3,
            Currency = 1,
            Money = 0.01m,
            SourceID = Guid.NewGuid().ToString("N")[..20],
            Description = "wave123 smoke",
            DescriptionA = "deduct",
            Flow = 0,
            MinFlow = 0
        });
        Assert.NotEmpty(deduct.Items);
    }

    /// <summary>定站。</summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("未定站");
    }

    /// <summary>空回调。</summary>
    private sealed class NoopCallbacks : IMoneyLedgerCallbacks
    {
        public Task AfterUnlockReviewAsync(int siteId, int userId, int currency, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AfterAddMoneyLogAsync(
            int siteId,
            int userId,
            decimal money,
            int type,
            int currency,
            int logId,
            string description,
            decimal flowRate,
            decimal minFlow,
            int eventId,
            int reviewId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

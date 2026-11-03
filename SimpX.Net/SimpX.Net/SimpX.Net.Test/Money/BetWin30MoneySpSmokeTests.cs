using Microsoft.AspNetCore.Http;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 连 BetWin30 真库冒烟 usr_AddMoneyLog（会员 test1031）。
/// </summary>
public sealed class BetWin30MoneySpSmokeTests
{
    /// <summary>人工加款类型（与人工加扣款测例 Type=2 一致）。</summary>
    private const int MoneyTypeManualGrant = 2;

    /// <summary>
    /// 给 test1031 加 0.01，读回 TotalMoney 增加。
    /// </summary>
    [Fact]
    public async Task AddMoneyLog_sp_credits_test1031()
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
        Assert.True(users.Count == 1, "缺少会员 test1031（SiteID=1001）");
        var user = users[0];

        var beforeRows = await db.Queryable<UserMoney>()
            .Where(w => w.SiteID == 1001 && w.UserID == user.ID && w.Currency == 1)
            .Take(1)
            .ToListAsync();
        Assert.True(beforeRows.Count == 1, "缺少 usr_Money Currency=1");
        var before = beforeRows[0].TotalMoney;

        var source = $"S{DateTime.UtcNow:HHmmssfff}";
        var ledger = new MoneyLedgerService(new TestBizDb(db), new HttpContextAccessor(), new NoopCallbacks());
        await ledger.AddMoneyLogAsync(
            1001,
            user.ID,
            0.01m,
            MoneyTypeManualGrant,
            source,
            "abcde2 SP smoke test1031",
            1);

        var afterRows = await db.Queryable<UserMoney>()
            .Where(w => w.SiteID == 1001 && w.UserID == user.ID && w.Currency == 1)
            .Take(1)
            .ToListAsync();
        var after = afterRows[0].TotalMoney;
        // 真库可能被其它冒烟并发改余额，只断言至少加了 0.01
        Assert.True(after >= before + 0.01m, $"余额未增加：before={before}, after={after}");
    }

    /// <summary>空回调。</summary>
    private sealed class NoopCallbacks : IMoneyLedgerCallbacks
    {
        /// <inheritdoc />
        public Task AfterUnlockReviewAsync(int siteId, int userId, int currency, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        /// <inheritdoc />
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

using Microsoft.AspNetCore.Http;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 真库冒烟：Excel 中文类型「手工加款」解析后给 test1031 加 0.01。
/// </summary>
public sealed class BetWin30MoneyOperateExcelTypeSmokeTests
{
    /// <summary>
    /// 中文类型解析成功并走加款主路径。
    /// </summary>
    [Fact]
    public async Task Grant_0_01_with_chinese_type_手工加款()
    {
        Assert.True(MoneyTypeNameResolver.TryResolve("手工加款", out var type));
        Assert.Equal(250, type);

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

        var site = new FixedSiteAccessor(1001, "localhost");
        var ledger = new MoneyLedgerService(new TestBizDb(db), new HttpContextAccessor(), new NoopCallbacks());
        var svc = new MoneyOperateService(new TestBizDb(db), site, ledger, new HttpContextAccessor());

        var grant = await svc.GrantAsync(new MoneyOperateSubmitInput
        {
            User = "test1031",
            Type = type,
            Currency = 1,
            Money = 0.01m,
            SourceID = Guid.NewGuid().ToString("N")[..20],
            Description = "cont1 excel-type",
            DescriptionA = "手工加款",
            Flow = 0,
            MinFlow = 0
        });
        Assert.True(grant.Items.Exists(x => x.Success), string.Join(" | ", grant.Items.Select(x => x.Message)));
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
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("未定站");
    }

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

using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 充值接口设定增改（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class SitePaymentCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表并植入分类。
    /// </summary>
    public SitePaymentCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(
            ["site_Payment", "site_PaymentCategory", "usr_Group"],
            []);
        _db.Insertable(new SitePaymentCategory
        {
            SiteID = 1001,
            Name = "银行卡",
            Sort = 1,
            Count = 0
        }).ExecuteCommand();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 新增→详情→改名。
    /// </summary>
    [Fact]
    public async Task Save_Info_RoundTrip()
    {
        var cateId = (await _db.Queryable<SitePaymentCategory>().Take(1).ToListAsync())[0].ID;
        var svc = new SitePaymentService(new TestBizDb(_db), _site);
        await svc.SaveAsync(new SitePaymentSaveInput
        {
            CateID = cateId,
            Name = "通道A",
            SysID = 11,
            MinMoney = 10,
            MaxMoney = 1000,
            Status = 1
        });

        var list = await svc.ListAsync(new SitePaymentListInput());
        Assert.Single(list);
        var id = list[0].ID;
        var info = await svc.InfoAsync(id);
        Assert.Equal("通道A", info.Name);

        await svc.SaveAsync(new SitePaymentSaveInput
        {
            ID = id,
            CateID = cateId,
            Name = "通道B",
            SysID = 11,
            MinMoney = 20,
            MaxMoney = 2000,
            Status = 1
        });
        Assert.Equal("通道B", (await svc.InfoAsync(id)).Name);
    }

    /// <summary>
    /// 固定定站。
    /// </summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>
        /// 指定站。
        /// </summary>
        public FixedSiteAccessor(int? siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        /// <inheritdoc />
        public int? SiteId { get; }

        /// <inheritdoc />
        public string Host { get; }

        /// <inheritdoc />
        public int RequireSiteId() =>
            SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

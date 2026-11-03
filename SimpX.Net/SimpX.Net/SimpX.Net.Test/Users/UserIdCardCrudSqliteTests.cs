using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 证件信息增删改查（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class UserIdCardCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表。
    /// </summary>
    public UserIdCardCrudSqliteTests()
    {
        // Page 会联查 Users 取用户名
        _db = SqliteBizSchema.CreateMemoryDb(["usr_IdCard", "Users", "usr_BlackKyc"], []);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 增→查→改→删。
    /// </summary>
    [Fact]
    public async Task Crud_RoundTrip()
    {
        var svc = new UserIdCardService(new TestBizDb(_db), _site);

        await svc.SaveAsync(new UserIdCardSaveInput
        {
            UserID = 90001,
            UserName = "user90001",
            CardName = "张三",
            CardNumber = "SQLite-TEST-CARD-001",
            CardType = 1,
            Status = 0,
            Country = "CN",
            CardFront = "/f/a.jpg",
            CardBack = "/f/b.jpg"
        });

        var page1 = await svc.PageAsync(new UserIdCardAdminPageInput { Page = 1, PageSize = 20 });
        Assert.Equal(1, page1.Total);
        var id = page1.Data[0].ID;
        Assert.Equal("张三", page1.Data[0].CardName);

        await svc.SaveAsync(new UserIdCardSaveInput
        {
            ID = id,
            UserID = 90001,
            CardName = "李四",
            CardNumber = "SQLite-TEST-CARD-001",
            CardType = 1,
            Status = 1,
            Remark = "已审"
        });

        var page2 = await svc.PageAsync(new UserIdCardAdminPageInput { Page = 1, PageSize = 20 });
        Assert.Equal("李四", page2.Data[0].CardName);
        Assert.Equal(1, page2.Data[0].Status);

        await svc.DeleteAsync(id);
        var page3 = await svc.PageAsync(new UserIdCardAdminPageInput { Page = 1, PageSize = 20 });
        Assert.Equal(0, page3.Total);
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

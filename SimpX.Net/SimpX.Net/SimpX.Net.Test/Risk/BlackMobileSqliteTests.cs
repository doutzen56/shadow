using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 手机黑名单增删改（内存 SQLite）。禁写 BetWin30。
/// </summary>
public sealed class BlackMobileSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表。
    /// </summary>
    public BlackMobileSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["usr_BlackMobile"], []);
    }

    /// <summary>
    /// 增→改→删。
    /// </summary>
    [Fact]
    public async Task Crud_RoundTrip()
    {
        var svc = new BlackMobileService(new TestBizDb(_db), _site);
        await svc.SaveAsync(new BlackMobileSaveInput { BlackMobile = "13800001111", Demo = "a" });
        var list1 = await svc.ListAsync();
        Assert.Single(list1);
        var id = list1[0].ID;

        await svc.SaveAsync(new BlackMobileSaveInput { ID = id, BlackMobile = "13800001111", Demo = "b" });
        var list2 = await svc.ListAsync();
        Assert.Equal("b", list2[0].Demo);

        await svc.DeleteAsync(id);
        Assert.Empty(await svc.ListAsync());
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    /// <summary>固定定站。</summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        private readonly int _id;
        private readonly string _host;
        public FixedSiteAccessor(int id, string host) { _id = id; _host = host; }
        public int? SiteId => _id;
        public string Host => _host;
        public int RequireSiteId() => _id;
    }
}

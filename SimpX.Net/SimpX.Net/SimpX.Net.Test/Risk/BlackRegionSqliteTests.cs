using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 国别限制读写（内存 SQLite）。禁写 BetWin30。
/// </summary>
public sealed class BlackRegionSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建 site_Config。
    /// </summary>
    public BlackRegionSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_Config"], []);
        _db.Insertable(new SiteConfig { SiteID = 1001, BlackRegion = "China" }).ExecuteCommand();
    }

    /// <summary>
    /// 列表 → 加入 → 移除。
    /// </summary>
    [Fact]
    public async Task List_Add_Remove()
    {
        var svc = new BlackRegionService(new TestBizDb(_db), _site);
        var list1 = await svc.ListAsync();
        Assert.Contains("China", list1.Regions);

        await svc.SaveAsync(new BlackRegionSaveInput { Value = "Japan", Save = true });
        var list2 = await svc.ListAsync();
        Assert.Contains("Japan", list2.Regions);

        await svc.SaveAsync(new BlackRegionSaveInput { Value = "Japan", Save = false });
        var list3 = await svc.ListAsync();
        Assert.DoesNotContain("Japan", list3.Regions);
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

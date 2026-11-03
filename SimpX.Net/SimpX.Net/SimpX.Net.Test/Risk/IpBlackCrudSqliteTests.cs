using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// IP 黑名单增删改查（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class IpBlackCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表。
    /// </summary>
    public IpBlackCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_Config_BlackIP"], []);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 增→查→改→删。
    /// </summary>
    [Fact]
    public async Task Crud_RoundTrip()
    {
        var svc = new IpBlackService(new TestBizDb(_db), _site);

        await svc.SaveAsync(new IpBlackSaveInput
        {
            ID = 0,
            Name = "测段",
            StartIP = "10.0.0.1",
            EndIP = "10.0.0.10",
            Remark = "r1"
        });

        var list1 = await svc.ListAsync();
        Assert.Single(list1);
        Assert.Equal("10.0.0.1", list1[0].StartIP);
        Assert.Equal("10.0.0.10", list1[0].EndIP);

        var id = list1[0].ID;
        var info = await svc.DetailAsync(id);
        Assert.Equal("测段", info.Name);
        Assert.Equal("r1", info.Remark);

        await svc.SaveAsync(new IpBlackSaveInput
        {
            ID = id,
            Name = "测段2",
            StartIP = "10.0.0.1",
            EndIP = "10.0.0.1",
            Remark = "r2"
        });

        var list2 = await svc.ListAsync();
        Assert.Equal("测段2", list2[0].Name);
        Assert.Equal("10.0.0.1", list2[0].EndIP);

        await svc.DeleteAsync(id);
        Assert.Empty(await svc.ListAsync());
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

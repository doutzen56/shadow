using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 站点域名增删改查（内存 SQLite，一比一表结构）。禁止写 BetWin30。
/// </summary>
public class SiteDomainCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建 SQLite 并植入空表。
    /// </summary>
    public SiteDomainCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_Domain"], []);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 增→查→改→删。
    /// </summary>
    [Fact]
    public async Task Crud_RoundTrip()
    {
        var svc = new SiteDomainService(new TestBizDb(_db), _site);

        await svc.SaveAsync(new SiteDomainSaveInput
        {
            ID = 0,
            Domain = "crud-domain.example",
            Weights = 10,
            IsVisible = true,
            IsSSL = true,
            AgentName = ""
        });

        var list1 = await svc.ListAsync();
        Assert.Single(list1);
        Assert.Equal("https://crud-domain.example", list1[0].Link);

        var id = list1[0].ID;
        var info = await svc.InfoAsync(id);
        Assert.Equal("crud-domain.example", info.Domain);

        await svc.SaveAsync(new SiteDomainSaveInput
        {
            ID = id,
            Domain = "crud-domain.example",
            Weights = 20,
            IsVisible = false,
            IsSSL = false,
            AgentName = ""
        });

        var list2 = await svc.ListAsync();
        Assert.Equal(20, list2[0].Weights);
        Assert.False(list2[0].IsVisible);
        Assert.StartsWith("http://", list2[0].Link);

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

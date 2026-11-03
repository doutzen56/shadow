using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 站点域名列表只读（BetWin30）。禁止写库。
/// </summary>
public class SiteDomainReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读连接。
    /// </summary>
    public SiteDomainReadOnlyTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// S2：定站域名列表可读；未定站失败。
    /// </summary>
    [Fact]
    public async Task S2_List_ReadOnly()
    {
        var host = await FirstHostAsync();
        var svc = new SiteDomainService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var result = await svc.ListAsync();
        Assert.NotNull(result);
        foreach (var row in result)
        {
            Assert.False(string.IsNullOrEmpty(row.Link));
            Assert.StartsWith("http", row.Link, StringComparison.OrdinalIgnoreCase);
        }

        var noSite = new SiteDomainService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => noSite.ListAsync());
        Assert.Contains("Z1005", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 取可用主机。
    /// </summary>
    private async Task<SiteHost> FirstHostAsync()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(hosts);
        return hosts[0];
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

using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// IP 黑名单只读。禁止写库。
/// </summary>
public class IpBlackReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>建立只读连接。</summary>
    public IpBlackReadOnlyTests() => _db = OrmConnection.CreateClient();

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>RISK-01：定站列表可读；未定站失败；IP 格式化。</summary>
    [Fact]
    public async Task Risk01_List_ReadOnly()
    {
        Assert.Equal("1.2.3.4", IpBlackService.FormatIp(16909060L));

        var host = await FirstHostAsync();
        var svc = new IpBlackService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var list = await svc.ListAsync();
        Assert.NotNull(list);

        var noSite = new IpBlackService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => noSite.ListAsync());
        Assert.Contains("Z1005", ex.Message, StringComparison.Ordinal);
    }

    private async Task<SiteHost> FirstHostAsync()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID).Take(1).ToListAsync();
        Assert.NotEmpty(hosts);
        return hosts[0];
    }

    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host) { SiteId = siteId; Host = host; }
        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}

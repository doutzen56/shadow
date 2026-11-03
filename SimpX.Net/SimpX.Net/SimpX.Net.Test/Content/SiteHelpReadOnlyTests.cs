using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>帮助中心只读。禁止写库。</summary>
public class SiteHelpReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>建立只读连接。</summary>
    public SiteHelpReadOnlyTests() => _db = OrmConnection.CreateClient();

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>CMS-N2：定站列表可读；未定站失败。</summary>
    [Fact]
    public async Task Help01_List_ReadOnly()
    {
        var host = await FirstHostAsync();
        var svc = new SiteHelpService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var list = await svc.ListAsync();
        Assert.NotNull(list);
        var cols = await svc.ColumnListAsync();
        Assert.NotNull(cols);

        var noSite = new SiteHelpService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
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

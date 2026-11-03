using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 只读连 BetWin30，验 SiteNewsService 列表/详情/栏目。禁止 save/delete 写测试库。
/// </summary>
public class SiteNewsReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读业务库连接。
    /// </summary>
    public SiteNewsReadOnlyTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// CMS-01：定站后新闻分页仅当前站。
    /// </summary>
    [Fact]
    public async Task Cms01_Page_ReturnsCurrentSiteRows_ReadOnly()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(hosts);
        var host = hosts[0];

        var hasNews = await _db.Queryable<News>().AnyAsync(n => n.SiteID == host.SiteID);
        Assert.True(hasNews);

        var svc = new SiteNewsService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var page = await svc.PageAsync(new SiteNewsPageInput { Page = 1, PageSize = 10 });
        Assert.True(page.RecordCount >= 1);
        Assert.NotEmpty(page.Data);
        Assert.All(page.Data, row => Assert.True(row.ID > 0));
    }

    /// <summary>
    /// CMS-01b：详情字段齐全；未定站失败。
    /// </summary>
    [Fact]
    public async Task Cms01b_Detail_And_WithoutSite_ReadOnly()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(hosts);
        var host = hosts[0];

        var newsRows = await _db.Queryable<News>()
            .Where(n => n.SiteID == host.SiteID)
            .OrderBy(n => n.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(newsRows);

        var svc = new SiteNewsService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var detail = await svc.DetailAsync(newsRows[0].ID);
        Assert.Equal(newsRows[0].ID, detail.ID);
        Assert.False(string.IsNullOrEmpty(detail.Title));
        Assert.False(string.IsNullOrEmpty(detail.CreateAt));

        var cols = await svc.ColumnListAsync();
        Assert.NotNull(cols);

        var noSite = new SiteNewsService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            noSite.PageAsync(new SiteNewsPageInput()));
        Assert.Contains("Z1005", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 固定当前站点，仅供只读测试。
    /// </summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>
        /// 指定站点号与主机名。
        /// </summary>
        /// <param name="siteId">站点号；null 表示未定站</param>
        /// <param name="host">主机名</param>
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
            SiteId ?? throw new InvalidOperationException("Z1005：未定站（单测替身，宿主为 Oops）");
    }
}

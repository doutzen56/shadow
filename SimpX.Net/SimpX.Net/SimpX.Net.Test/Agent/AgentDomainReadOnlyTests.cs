using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 只读连 BetWin30，验代理域名列表/详情。禁止写库。
/// </summary>
public class AgentDomainReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读连接。
    /// </summary>
    public AgentDomainReadOnlyTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// PROXY-01：定站后列表（放宽日期到近 10 年以免今日无数据）。
    /// </summary>
    [Fact]
    public async Task Proxy01_Page_ReturnsCurrentSite_ReadOnly()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(hosts);
        var host = hosts[0];

        var has = await _db.Queryable<UserDomain>().AnyAsync(d => d.SiteID == host.SiteID);
        Assert.True(has);

        var svc = new AgentDomainService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var page = await svc.PageAsync(new AgentDomainPageInput
        {
            Page = 1,
            PageSize = 10,
            StartAt = DateTime.Now.AddYears(-10),
            EndAt = DateTime.Now.AddDays(1)
        });
        Assert.True(page.RecordCount >= 1);
        Assert.NotEmpty(page.Data);
        Assert.All(page.Data, r => Assert.True(r.ID > 0));

        var detail = await svc.DetailAsync(page.Data[0].ID);
        Assert.Equal(page.Data[0].ID, detail.ID);

        var noSite = new AgentDomainService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            noSite.PageAsync(new AgentDomainPageInput { StartAt = DateTime.Now.AddYears(-1), EndAt = DateTime.Now }));
        Assert.Contains("Z1005", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 固定站点。
    /// </summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>
        /// 指定站。
        /// </summary>
        /// <param name="siteId">站号</param>
        /// <param name="host">主机</param>
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

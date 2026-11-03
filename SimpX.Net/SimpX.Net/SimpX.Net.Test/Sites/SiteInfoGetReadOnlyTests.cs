using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 只读连 BetWin30，验 SiteInfoService.Get（对齐旧 site/info/get）。禁止 save，不写测试库。
/// </summary>
public class SiteInfoGetReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读业务库连接。
    /// </summary>
    public SiteInfoGetReadOnlyTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// SITE-01：按 site_Host 定站后 get，返回同站 ID、Setting、Key(MD5)。
    /// </summary>
    [Fact]
    public async Task Site01_Get_ReturnsCurrentSiteFields_ReadOnly()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(hosts);
        var host = hosts[0];

        var siteExists = await _db.Queryable<Site>().AnyAsync(s => s.ID == host.SiteID);
        Assert.True(siteExists);

        var accessor = new FixedSiteAccessor(host.SiteID, host.Host);
        var svc = new SiteInfoService(new TestBizDb(_db), accessor);
        var output = await svc.GetAsync();

        Assert.Equal(host.SiteID, output.ID);
        Assert.False(string.IsNullOrEmpty(output.Name));
        Assert.NotNull(output.Setting);
        Assert.Matches("^[0-9a-f]{32}$", output.Key);
        Assert.False(string.IsNullOrEmpty(output.Logo));
    }

    /// <summary>
    /// SITE-04：未定站时 get 在查库前就失败（单测替身抛 Z1005；宿主里是 Oops Z1005）。
    /// </summary>
    [Fact]
    public async Task Site04_Get_WithoutSite_FailsBeforeQuery()
    {
        var accessor = new FixedSiteAccessor(null, null);
        var svc = new SiteInfoService(new TestBizDb(_db), accessor);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GetAsync());
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

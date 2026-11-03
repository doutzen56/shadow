using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 会员帐变只读（BetWin30）。禁止写库。
/// </summary>
public class MoneyLogReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读连接。
    /// </summary>
    public MoneyLogReadOnlyTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// MONEY-01：定站分页可读；未定站失败。
    /// </summary>
    [Fact]
    public async Task Money01_Page_ReadOnly()
    {
        var host = await FirstHostAsync();
        var svc = new MoneyLogService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var page = await svc.PageAsync(new MoneyLogAdminPageInput
        {
            Page = 1,
            PageSize = 10,
            StartAt = DateTime.Now.Date.AddDays(-7),
            EndAt = DateTime.Now
        });
        Assert.NotNull(page);
        Assert.NotNull(page.Data);
        Assert.NotNull(page.Summary);
        Assert.True(page.Total >= 0);

        var noSite = new MoneyLogService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            noSite.PageAsync(new MoneyLogAdminPageInput()));
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

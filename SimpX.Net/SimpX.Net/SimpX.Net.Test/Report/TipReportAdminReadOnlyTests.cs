using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 打赏报表只读（BetWin30）。禁止写库。
/// </summary>
public class TipReportAdminReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读连接。
    /// </summary>
    public TipReportAdminReadOnlyTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// T3：定站打赏查询成功（可空）；未定站失败。
    /// </summary>
    [Fact]
    public async Task T3_List_ReadOnly()
    {
        var host = await FirstHostAsync();
        var today = DateTime.Now.Date.ToString("yyyy-MM-dd");
        var svc = new TipReportService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var result = await svc.ListAsync(new TipReportListInput
        {
            StartAt = today,
            EndAt = today,
            IsOpen = "1"
        });
        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        var noSite = new TipReportService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            noSite.ListAsync(new TipReportListInput { StartAt = today, EndAt = today }));
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

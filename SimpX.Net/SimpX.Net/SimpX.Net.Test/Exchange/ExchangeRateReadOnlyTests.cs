using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 汇率只读（BetWin30）。禁止写库。
/// </summary>
public class ExchangeRateReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读连接。
    /// </summary>
    public ExchangeRateReadOnlyTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// FX-01：基准币与列表可读；未定站失败。
    /// </summary>
    [Fact]
    public async Task Fx01_ListAndBase_ReadOnly()
    {
        var host = await FirstHostAsync();
        var svc = new ExchangeRateService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var bas = await svc.GetBaseCurrencyAsync();
        Assert.True(bas.BaseValue >= 0);

        var list = await svc.ListAsync(new ExchangeRateListInput { Currency = bas.BaseValue });
        Assert.NotNull(list);

        if (list.Count > 0)
        {
            var detail = await svc.DetailAsync(list[0].ID);
            Assert.Equal(list[0].ID, detail.ID);
            Assert.Equal(host.SiteID, detail.SiteID);
        }

        var noSite = new ExchangeRateService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            noSite.GetBaseCurrencyAsync());
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

using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 汇率保存（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class ExchangeRateCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表并植入一条汇率。
    /// </summary>
    public ExchangeRateCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_ExchangeRate", "site_Currency"], []);
        _db.Insertable(new SiteCurrency
        {
            SiteID = 1001,
            Currency = 2,
            IsOpen = true,
            Sort = 1,
            Accuracy = 2,
            SysOpen = true,
            IsRegister = true
        }).ExecuteCommand();
        _db.Insertable(new SiteExchangeRate
        {
            SiteID = 1001,
            BaseCurrency = 1,
            ExchangeCurrency = 2,
            MarketPrice = 7.2m,
            Point = 7.2m,
            ExchangeMode = 0,
            Parameter = 0,
            IsBase = true,
            ExchangeFee = 0,
            ExchangeCharge = 0,
            ExchangeLimit = 0,
            IsCheck = false,
            Status = 1
        }).ExecuteCommand();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// FX-02：自定模式保存后 Point=参数。
    /// </summary>
    [Fact]
    public async Task Save_Custom_UpdatesPoint()
    {
        var id = (await _db.Queryable<SiteExchangeRate>().Take(1).ToListAsync())[0].ID;
        var svc = new ExchangeRateService(new TestBizDb(_db), _site);
        await svc.SaveAsync(new ExchangeRateSaveInput
        {
            ID = id,
            MarketPrice = 7.2m,
            ExchangeMode = 3,
            Parameter = 6.8m
        });

        var detail = await svc.DetailAsync(id);
        Assert.Equal(3, detail.ExchangeMode);
        Assert.Equal(6.8m, detail.Point);
        Assert.Equal(6.8m, detail.Parameter);
    }

    /// <summary>
    /// 差价模式：在旧点数上加参数。
    /// </summary>
    [Fact]
    public async Task Save_Price_AddsParameterToOldPoint()
    {
        var id = (await _db.Queryable<SiteExchangeRate>().Take(1).ToListAsync())[0].ID;
        var svc = new ExchangeRateService(new TestBizDb(_db), _site);
        await svc.SaveAsync(new ExchangeRateSaveInput
        {
            ID = id,
            MarketPrice = 7.2m,
            ExchangeMode = 1,
            Parameter = 0.1m
        });

        var detail = await svc.DetailAsync(id);
        Assert.Equal(7.3m, detail.Point);
        Assert.Equal(0.1m, detail.Parameter);
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

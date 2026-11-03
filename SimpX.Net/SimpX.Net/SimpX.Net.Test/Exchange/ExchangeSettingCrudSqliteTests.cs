using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 兑币设置增改（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class ExchangeSettingCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表并植入一条汇率。
    /// </summary>
    public ExchangeSettingCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_ExchangeRate"], []);
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
            ExchangeCharge = 0.01m,
            ExchangeLimit = 0,
            IsCheck = false,
            Status = 1
        }).ExecuteCommand();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 列表→详情→保存手续费百分比入库。
    /// </summary>
    [Fact]
    public async Task ExchangeList_Info_Save_RoundTrip()
    {
        var svc = new ExchangeRateService(new TestBizDb(_db), _site);
        var list = await svc.ExchangeListAsync(new ExchangeSettingListInput { CurrencyType = 1 });
        Assert.Single(list);
        Assert.Equal("#开放#", list[0].Status);
        Assert.Equal("#倍率#", list[0].ExchangeFee);

        var id = list[0].ID;
        var info = await svc.ExchangeInfoAsync(id);
        Assert.Equal("1.00", info.ExchangeCharge);

        await svc.SaveExchangeAsync(new ExchangeSettingSaveInput
        {
            ID = id,
            ExchangeFee = 1,
            ExchangeCharge = 2.5m,
            ExchangeLimit = 100,
            IsCheck = true,
            Status = 0
        });

        var after = await svc.ExchangeInfoAsync(id);
        Assert.Equal(1, after.ExchangeFee);
        Assert.Equal("2.50", after.ExchangeCharge);
        Assert.Equal(100, after.ExchangeLimit);
        Assert.True(after.IsCheck);
        Assert.Equal(0, after.Status);

        var row = (await _db.Queryable<SiteExchangeRate>().Where(r => r.ID == id).ToListAsync())[0];
        Assert.True(Math.Abs(row.ExchangeCharge - 0.025m) < 0.0001m);

        var closed = await svc.ExchangeListAsync(new ExchangeSettingListInput
        {
            CurrencyType = 1,
            Status = "0"
        });
        Assert.Single(closed);
    }

    /// <summary>
    /// 他站编号不可改。
    /// </summary>
    [Fact]
    public async Task SaveExchange_WrongSite_Fails()
    {
        var other = new ExchangeRateService(new TestBizDb(_db), new FixedSiteAccessor(9999, "other"));
        var id = (await _db.Queryable<SiteExchangeRate>().Take(1).ToListAsync())[0].ID;
        await Assert.ThrowsAnyAsync<Exception>(() => other.SaveExchangeAsync(new ExchangeSettingSaveInput
        {
            ID = id,
            ExchangeFee = 0,
            ExchangeCharge = 1m,
            ExchangeLimit = 0,
            IsCheck = false,
            Status = 1
        }));
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

using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Systems;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 平台货币汇率（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class SystemExchangeSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建表。
    /// </summary>
    public SystemExchangeSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["sys_Exchange"], []);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 空表 List 会补齐币对；改价持久化。
    /// </summary>
    [Fact]
    public async Task List_CreatesPairs_ThenUpdatePrice()
    {
        var svc = new SystemExchangeService(new TestBizDb(_db));
        var list = await svc.ListAsync(new SystemExchangeListInput { Currency = 0 });
        Assert.True(list.Count >= 30);
        Assert.DoesNotContain(list, x => x.CurrencyGroup.StartsWith("CNY/CNY", StringComparison.Ordinal));

        var first = list[0];
        var result = await svc.UpdateMarketPriceAsync(new SystemExchangeUpdatePriceInput
        {
            ID = first.ID,
            MarketPrice = "7.25"
        });
        Assert.True(result.UpdateAt > DateTime.MinValue);

        var row = (await _db.Queryable<SystemExchange>().Where(x => x.ID == first.ID).ToListAsync())[0];
        Assert.Equal(7.25m, row.MarketPrice);
    }

    /// <summary>
    /// 非法价格拒绝。
    /// </summary>
    [Fact]
    public async Task UpdatePrice_BadNumber_Fails()
    {
        var svc = new SystemExchangeService(new TestBizDb(_db));
        await svc.ListAsync(new SystemExchangeListInput { Currency = 1 });
        var id = (await _db.Queryable<SystemExchange>().Take(1).ToListAsync())[0].ID;
        await Assert.ThrowsAnyAsync<Exception>(() =>
            svc.UpdateMarketPriceAsync(new SystemExchangeUpdatePriceInput
            {
                ID = id,
                MarketPrice = "abc"
            }));
    }
}

using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台系统市场汇率。对齐旧版 /system/setting/config/exchangelist|updateExchange|updatecurrency。
/// </summary>
public sealed class SystemExchangeService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。系统汇率在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemExchangeService(IBizDbAccessor biz)
    {
        _db = biz.Db;
    }

    /// <summary>
    /// 按基准币列汇率；若无数据则按旧 CreateExchange 补齐。
    /// </summary>
    /// <param name="input">基准币</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SystemExchangeListItemDto>> ListAsync(
        SystemExchangeListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var baseCurrency = input.Currency;
        var list = await _db.Queryable<SystemExchange>()
            .Where(p => p.BaseCurrency == baseCurrency)
            .OrderBy(p => p.ExchangeCurrency)
            .ToListAsync(cancellationToken);

        if (list.Count == 0)
        {
            await CreateMissingPairsAsync(baseCurrency, cancellationToken);
            list = await _db.Queryable<SystemExchange>()
                .Where(p => p.BaseCurrency == baseCurrency)
                .OrderBy(p => p.ExchangeCurrency)
                .ToListAsync(cancellationToken);
        }

        return list.Select(p => new SystemExchangeListItemDto
        {
            ID = p.ID,
            CurrencyGroup = $"{OldApiEnumDisplay.Currency(p.BaseCurrency)}/{OldApiEnumDisplay.Currency(p.ExchangeCurrency)}",
            MarketPrice = p.MarketPrice,
            UpdateAt = p.UpdateAt
        }).ToList();
    }

    /// <summary>
    /// 更新市场价。对齐旧 updateExchange。
    /// </summary>
    /// <param name="input">编号与价格</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新时间</returns>
    public async Task<SystemExchangeUpdatePriceResult> UpdateMarketPriceAsync(
        SystemExchangeUpdatePriceInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#编号错误#");
        if (string.IsNullOrWhiteSpace(input.MarketPrice))
            throw Oops.Oh("#请输汇率错误#");
        if (!decimal.TryParse(input.MarketPrice.Trim(), out var price))
            throw Oops.Oh("#请输汇率必须为数字#");

        var rows = await _db.Queryable<SystemExchange>()
            .Where(p => p.ID == input.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var exchange = rows.FirstOrDefault()
            ?? throw Oops.Oh("#编号错误#");

        var now = DateTime.Now;
        exchange.MarketPrice = price;
        exchange.UpdateAt = now;
        var ok = await _db.Updateable(exchange)
            .UpdateColumns(p => new { p.MarketPrice, p.UpdateAt })
            .ExecuteCommandAsync(cancellationToken);
        if (ok != 1)
            throw Oops.Oh("#保存失败#");

        return new SystemExchangeUpdatePriceResult { UpdateAt = now };
    }

    /// <summary>
    /// 补齐缺失币对。对齐旧 updatecurrency。
    /// </summary>
    /// <param name="currency">基准币</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task EnsureCurrencyPairsAsync(int currency, CancellationToken cancellationToken = default)
    {
        await CreateMissingPairsAsync(currency, cancellationToken);
    }

    /// <summary>
    /// 为基准币补齐尚未存在的兑换币对（默认市价 1.02）。
    /// </summary>
    private async Task CreateMissingPairsAsync(int baseCurrency, CancellationToken cancellationToken)
    {
        var existing = await _db.Queryable<SystemExchange>()
            .Where(p => p.BaseCurrency == baseCurrency)
            .Select(p => p.ExchangeCurrency)
            .ToListAsync(cancellationToken);
        var set = existing.ToHashSet();
        var now = DateTime.Now;
        var toAdd = new List<SystemExchange>();
        foreach (Currency c in Enum.GetValues<Currency>())
        {
            var item = (int)c;
            if (item == baseCurrency || set.Contains(item))
                continue;
            toAdd.Add(new SystemExchange
            {
                BaseCurrency = baseCurrency,
                ExchangeCurrency = item,
                MarketPrice = 1.02m,
                Status = true,
                UpdateAt = now
            });
        }

        if (toAdd.Count > 0)
            await _db.Insertable(toAdd).ExecuteCommandAsync(cancellationToken);
    }
}

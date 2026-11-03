using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台汇率与兑币设置。对齐旧版 /admin/exchange/rate/*。
/// </summary>
public sealed partial class ExchangeRateService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。汇率在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public ExchangeRateService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 按基准币列出已开启币种的汇率。
    /// </summary>
    /// <param name="input">基准币</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>列表</returns>
    public async Task<List<ExchangeRateListItemDto>> ListAsync(
        ExchangeRateListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var baseCurrency = input.Currency;

        var openCurrencies = await _db.Queryable<SiteCurrency>()
            .Where(c => c.SiteID == siteId && c.IsOpen)
            .Select(c => c.Currency)
            .ToListAsync(cancellationToken);

        var list = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.SiteID == siteId && r.BaseCurrency == baseCurrency && openCurrencies.Contains(r.ExchangeCurrency))
            .OrderBy(r => r.ExchangeCurrency)
            .ToListAsync(cancellationToken);

        return list.Select(p => new ExchangeRateListItemDto
        {
            ID = p.ID,
            CurrencyGroup = $"{p.BaseCurrency}/{p.ExchangeCurrency}",
            MarketPrice = p.MarketPrice,
            Point = p.Point,
            Mode = DescribeMode(p.ExchangeMode),
            Parameter = p.Parameter == 0 ? "-" : p.Parameter.ToString()
        }).ToList();
    }

    /// <summary>
    /// 当前站基准币。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>基准币</returns>
    public async Task<ExchangeBaseCurrencyDto> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var baseRow = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.SiteID == siteId && r.IsBase)
            .OrderBy(r => r.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var value = baseRow.Count > 0 ? baseRow[0].BaseCurrency : 0;
        if (value == 0)
        {
            var any = await _db.Queryable<SiteExchangeRate>()
                .Where(r => r.SiteID == siteId)
                .OrderBy(r => r.ID)
                .Take(1)
                .ToListAsync(cancellationToken);
            if (any.Count > 0)
                value = any[0].BaseCurrency;
        }

        return new ExchangeBaseCurrencyDto { Base = value, BaseValue = value };
    }

    /// <summary>
    /// 详情；须属于当前站。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<ExchangeRateDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.ID == id && r.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var model = rows.FirstOrDefault()
            ?? throw Oops.Oh("编号错误");

        return new ExchangeRateDetailDto
        {
            ID = model.ID,
            MarketPrice = model.MarketPrice,
            SiteID = model.SiteID,
            ExchangeMode = (byte)model.ExchangeMode,
            Parameter = model.Parameter,
            Point = model.Point,
            IsBase = model.IsBase
        };
    }

    /// <summary>
    /// 保存汇率点数。对齐旧 saveinfo：按模式用服务端重算 Point。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(ExchangeRateSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("编号错误");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.ID == input.ID && r.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var exchange = rows.FirstOrDefault()
            ?? throw Oops.Oh("编号错误");

        var oldPoint = exchange.Point;
        exchange.MarketPrice = input.MarketPrice;
        exchange.ExchangeMode = input.ExchangeMode;

        // 差价/倍率在旧点数上算；行情用市场价；自定用参数。对齐旧 saveinfo。
        switch (input.ExchangeMode)
        {
            case 0:
                exchange.Point = input.MarketPrice;
                break;
            case 1:
                exchange.Point = oldPoint + input.Parameter;
                exchange.Parameter = input.Parameter;
                break;
            case 2:
                exchange.Point = oldPoint * input.Parameter;
                exchange.Parameter = input.Parameter;
                break;
            case 3:
                exchange.Point = input.Parameter;
                exchange.Parameter = input.Parameter;
                break;
            default:
                throw Oops.Oh("交易模式错误");
        }

        await _db.Updateable(exchange)
            .UpdateColumns(r => new
            {
                r.Point,
                r.MarketPrice,
                r.Parameter,
                r.ExchangeMode
            })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 兑币设置列表。对齐旧 exchangelist。
    /// </summary>
    /// <param name="input">基准币与状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<ExchangeSettingListItemDto>> ExchangeListAsync(
        ExchangeSettingListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<SiteExchangeRate>()
            .Where(r => r.SiteID == siteId && r.BaseCurrency == input.CurrencyType);

        var statusFilter = ParseOpenStatusFilter(input.Status);
        if (statusFilter.HasValue)
            query = query.Where(r => r.Status == statusFilter.Value);

        var list = await query
            .OrderBy(r => r.ExchangeCurrency)
            .ToListAsync(cancellationToken);

        return list.Select(p => new ExchangeSettingListItemDto
        {
            ID = p.ID,
            CurrencyGroup = $"{p.BaseCurrency}/{p.ExchangeCurrency}",
            ExchangeCharge = p.ExchangeCharge,
            Status = DescribeOpenStatus(p.Status),
            ExchangeFee = DescribeFee(p.ExchangeFee),
            Point = p.Point,
            Limit = p.ExchangeLimit
        }).ToList();
    }

    /// <summary>
    /// 兑币设置详情。对齐旧 exchangeinfo。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<ExchangeSettingDetailDto> ExchangeInfoAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.ID == id && r.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var model = rows.FirstOrDefault()
            ?? throw Oops.Oh("编号错误");

        return new ExchangeSettingDetailDto
        {
            ID = model.ID,
            CurrencyGroup = $"{model.BaseCurrency}/{model.ExchangeCurrency}",
            SiteID = model.SiteID,
            ExchangeFee = (byte)model.ExchangeFee,
            ExchangeLimit = model.ExchangeLimit,
            Status = (byte)model.Status,
            IsCheck = model.IsCheck,
            ExchangeCharge = (model.ExchangeCharge * 100).ToString("0.00")
        };
    }

    /// <summary>
    /// 保存兑币设置。对齐旧 saveexchange：手续费表单值除以 100 入库。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveExchangeAsync(
        ExchangeSettingSaveInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("编号错误");
        if (input.ExchangeFee > 1)
            throw Oops.Oh("手续费模式错误");
        if (input.Status > 1)
            throw Oops.Oh("状态错误");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.ID == input.ID && r.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var exchange = rows.FirstOrDefault()
            ?? throw Oops.Oh("编号错误");

        exchange.ExchangeFee = input.ExchangeFee;
        exchange.ExchangeCharge = input.ExchangeCharge / 100m;
        exchange.ExchangeLimit = input.ExchangeLimit;
        exchange.IsCheck = input.IsCheck;
        exchange.Status = input.Status;

        await _db.Updateable(exchange)
            .UpdateColumns(r => new
            {
                r.ExchangeFee,
                r.ExchangeCharge,
                r.ExchangeLimit,
                r.IsCheck,
                r.Status
            })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 模式文案（对齐旧 Description）。
    /// </summary>
    private static string DescribeMode(int mode) => mode switch
    {
        0 => "#市场行情#",
        1 => "#差价#",
        2 => "#倍率#",
        3 => "#自定#",
        _ => mode.ToString()
    };

    /// <summary>
    /// 手续费模式文案。
    /// </summary>
    private static string DescribeFee(int fee) => fee switch
    {
        0 => "#倍率#",
        1 => "#自定#",
        _ => fee.ToString()
    };

    /// <summary>
    /// 开放状态文案。
    /// </summary>
    private static string DescribeOpenStatus(int status) => status switch
    {
        0 => "#关闭#",
        1 => "#开放#",
        _ => status.ToString()
    };

    /// <summary>
    /// 解析列表状态筛选项；空表示不筛。
    /// </summary>
    private static byte? ParseOpenStatusFilter(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;
        var s = status.Trim();
        if (byte.TryParse(s, out var n) && n <= 1)
            return n;
        if (string.Equals(s, "Open", StringComparison.OrdinalIgnoreCase)
            || s.Contains("开放", StringComparison.Ordinal))
            return 1;
        if (string.Equals(s, "Close", StringComparison.OrdinalIgnoreCase)
            || s.Contains("关闭", StringComparison.Ordinal))
            return 0;
        return null;
    }

    // --- merged from ExchangeRateService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 getcurrency。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetcurrencyAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetcurrencyCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 getcurrencyinfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetcurrencyinfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetcurrencyinfoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 getfee。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetfeeAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetfeeCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 getmode。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetmodeAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetmodeCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 GetSysExchange。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetSysExchangeAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetSysExchangeCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatesiterate。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatesiterateAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatesiterateCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：getcurrency —— 当前站已开启币种。</summary>
    private async Task<object?> GetcurrencyCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteCurrency>()
            .Where(c => c.SiteID == siteId && c.IsOpen)
            .OrderBy(c => c.Sort)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new
        {
            Type = OldApiEnumDisplay.Currency(t.Currency),
            Name = t.Currency
        }).ToList();
    }


    /// <summary>核心：getcurrencyinfo —— 全币种字典（源：Data.Currency 枚举）。</summary>
    private Task<object?> GetcurrencyinfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        var values = Enum.GetValues<Currency>();
        var list = new List<object>(values.Length);
        foreach (var c in values)
            list.Add(new { Type = (int)c, Name = c.ToString() });
        return Task.FromResult<object?>(list);
    }


    /// <summary>核心：getfee</summary>
    private Task<object?> GetfeeCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(new object[]
        {
            new { Type = (byte)0, Name = "#倍率#" },
            new { Type = (byte)1, Name = "#自定#" }
        });
    }


    /// <summary>核心：getmode</summary>
    private Task<object?> GetmodeCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(new object[]
        {
            new { Type = (byte)0, Name = "#市场行情#" },
            new { Type = (byte)1, Name = "#差价#" },
            new { Type = (byte)2, Name = "#倍率#" },
            new { Type = (byte)3, Name = "#自定#" }
        });
    }


    /// <summary>核心：GetSysExchange —— 从系统汇率补齐/刷新站点市场汇率。</summary>
    private async Task<object?> GetSysExchangeCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var baseCurrency = ReadInt(input, "Currency", "currency", "Type", "Base");
        await SyncSysExchangeAsync(baseCurrency, cancellationToken);
        return null;
    }


    /// <summary>核心：updatesiterate —— 刷新所有市场模式站点汇率。</summary>
    private async Task<object?> UpdatesiterateCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var bases = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.SiteID == siteId)
            .Select(r => r.BaseCurrency)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (bases.Count == 0)
        {
            var cur = await GetBaseCurrencyAsync(cancellationToken);
            bases.Add(cur.BaseValue);
        }

        foreach (var b in bases.Distinct())
            await SyncSysExchangeAsync(b, cancellationToken);
        return null;
    }


    /// <summary>
    /// 对齐旧 savecurrency/GetSysExchange：缺币对插入，市场模式同步总后台价。
    /// </summary>
    /// <param name="baseCurrency">基准币</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SyncFromSystemAsync(int baseCurrency, CancellationToken cancellationToken = default) =>
        SyncSysExchangeAsync(baseCurrency, cancellationToken);


    private async Task SyncSysExchangeAsync(int baseCurrency, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var siteBase = await GetBaseCurrencyAsync(cancellationToken);
        var sysBases = await _db.Queryable<SystemExchange>()
            .Where(p => p.BaseCurrency == baseCurrency)
            .ToListAsync(cancellationToken);
        var siteBases = await _db.Queryable<SiteExchangeRate>()
            .Where(p => p.SiteID == siteId && p.BaseCurrency == baseCurrency)
            .ToListAsync(cancellationToken);
        var siteKeys = siteBases
            .Select(t => t.ExchangeCurrency)
            .ToHashSet();

        foreach (var item in sysBases.Where(p => !siteKeys.Contains(p.ExchangeCurrency)))
        {
            await _db.Insertable(new SiteExchangeRate
            {
                SiteID = siteId,
                BaseCurrency = item.BaseCurrency,
                ExchangeCurrency = item.ExchangeCurrency,
                MarketPrice = item.MarketPrice,
                ExchangeMode = 0,
                Point = item.MarketPrice,
                Parameter = 0,
                IsBase = siteBase.BaseValue == baseCurrency,
                ExchangeFee = 0,
                ExchangeCharge = 0,
                ExchangeLimit = 0,
                IsCheck = false,
                Status = 1
            }).ExecuteCommandAsync(cancellationToken);
        }

        siteBases = await _db.Queryable<SiteExchangeRate>()
            .Where(p => p.SiteID == siteId && p.BaseCurrency == baseCurrency && p.ExchangeMode == 0)
            .ToListAsync(cancellationToken);
        foreach (var item in siteBases)
        {
            var sys = sysBases.FirstOrDefault(t =>
                t.BaseCurrency == item.BaseCurrency && t.ExchangeCurrency == item.ExchangeCurrency);
            if (sys == null) continue;
            item.MarketPrice = sys.MarketPrice;
            item.Point = sys.MarketPrice;
            await _db.Updateable(item)
                .UpdateColumns(r => new { r.MarketPrice, r.Point })
                .ExecuteCommandAsync(cancellationToken);
        }
    }


    private static int ReadInt(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var n)) return n;
        }
        return 0;
    }
}

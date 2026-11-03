using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点币种。对齐旧 /admin/site/currency/*。
/// </summary>
public sealed class SiteCurrencyAdminService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>注入。</summary>
    public SiteCurrencyAdminService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>对齐 list：SysOpen 币种管理列表。</summary>
    public async Task<object> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteCurrency>()
            .Where(t => t.SiteID == siteId && t.SysOpen)
            .OrderBy(t => t.Sort)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new
        {
            t.ID,
            t.IsOpen,
            Currency = OldApiEnumDisplay.Currency(t.Currency),
            Image = t.Currency,
            t.Accuracy,
            t.Sort,
            t.IsRegister
        }).ToList();
    }

    /// <summary>对齐 info。</summary>
    public async Task<object> InfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteCurrency>()
            .Where(t => t.SiteID == siteId && t.ID == id).Take(1).ToListAsync(cancellationToken);
        var currency = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new
        {
            currency.ID,
            currency.IsOpen,
            Name = OldApiEnumDisplay.Currency(currency.Currency),
            currency.Sort,
            currency.Accuracy,
            currency.IsRegister
        };
    }

    /// <summary>对齐 save。</summary>
    public async Task SaveAsync(int id, int sort, bool isOpen, int accuracy, bool isRegister, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteCurrency>()
            .Where(t => t.SiteID == siteId && t.ID == id).Take(1).ToListAsync(cancellationToken);
        var currency = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        currency.Sort = sort;
        currency.IsOpen = isOpen;
        currency.Accuracy = accuracy;
        currency.IsRegister = isRegister;
        await _db.Updateable(currency)
            .UpdateColumns(c => new { c.Sort, c.IsOpen, c.Accuracy, c.IsRegister })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>对齐 getcurrency。</summary>
    public async Task<object> GetCurrencyAsync(bool? isOpen, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<SiteCurrency>().Where(t => t.SiteID == siteId);
        if (isOpen.HasValue)
            query = query.Where(t => t.IsOpen == isOpen.Value);
        var rows = await query.OrderBy(t => t.Sort).ToListAsync(cancellationToken);
        return rows.Select(t => new { Value = t.Currency, Name = OldApiEnumDisplay.Currency(t.Currency) }).ToList();
    }

    /// <summary>对齐 show：已开启币种字典 Currency→名。</summary>
    public async Task<object> ShowAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteCurrency>()
            .Where(t => t.SiteID == siteId && t.IsOpen)
            .OrderBy(t => t.Sort)
            .ToListAsync(cancellationToken);
        var dic = new Dictionary<string, string>();
        foreach (var item in rows)
        {
            var name = OldApiEnumDisplay.Currency(item.Currency);
            dic[item.Currency.ToString(System.Globalization.CultureInfo.InvariantCulture)] = name;
            dic[name] = name;
        }
        return dic;
    }

    /// <summary>对齐 showreport。</summary>
    public async Task<object> ShowReportAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteCurrency>()
            .Where(t => t.SiteID == siteId && t.IsOpen)
            .OrderBy(t => t.Sort)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new { key = (byte)t.Currency, value = OldApiEnumDisplay.Currency(t.Currency) }).ToList();
    }

    /// <summary>对齐 report（含未开）。</summary>
    public async Task<object> ReportAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteCurrency>()
            .Where(t => t.SiteID == siteId)
            .OrderBy(t => t.Sort)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new { key = t.Currency, value = OldApiEnumDisplay.Currency(t.Currency) }).ToList();
    }

    /// <summary>对齐 sort：按 Currency 逗号序重排 Sort。</summary>
    public async Task SortAsync(string currencyCsv, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var codes = (currencyCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : -1)
            .Where(n => n >= 0)
            .ToArray();
        if (codes.Length == 0)
            throw Oops.Oh("#参数错误#");
        var rows = await _db.Queryable<SiteCurrency>()
            .Where(t => t.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var sort = 0;
        foreach (var code in codes)
        {
            var row = rows.FirstOrDefault(t => t.Currency == code);
            if (row == null) continue;
            row.Sort = sort++;
            await _db.Updateable(row).UpdateColumns(c => c.Sort).ExecuteCommandAsync(cancellationToken);
        }
    }

    /// <summary>对齐 topone：返回站基础币（Site.Currency）。</summary>
    public async Task<object> TopOneAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var currency = await SiteCurrencyFx.GetBaseCurrencyAsync(_db, siteId, cancellationToken);
        return new { Currency = (byte)currency };
    }

    /// <summary>对齐 InitSiteCurrency：缺币种时按 <see cref="Data.Currency"/> 全量补全 SysOpen=false。</summary>
    public async Task InitSiteCurrencyAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var existing = await _db.Queryable<SiteCurrency>()
            .Where(t => t.SiteID == siteId)
            .Select(t => t.Currency)
            .ToListAsync(cancellationToken);
        var set = existing.ToHashSet();
        var inserts = new List<SiteCurrency>();
        foreach (Currency c in Enum.GetValues<Currency>())
        {
            var i = (int)c;
            if (set.Contains(i)) continue;
            inserts.Add(new SiteCurrency
            {
                SiteID = siteId,
                Currency = i,
                IsOpen = false,
                SysOpen = true,
                Sort = i,
                Accuracy = 2,
                IsRegister = false
            });
        }

        if (inserts.Count > 0)
            await _db.Insertable(inserts).ExecuteCommandAsync(cancellationToken);
    }
}

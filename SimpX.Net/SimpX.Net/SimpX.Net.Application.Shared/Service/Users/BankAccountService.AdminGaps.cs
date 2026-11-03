using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 银行信息缺口：超商地址树、国家银行列表别名。
/// </summary>
public sealed partial class BankAccountService
{
    /// <summary>
    /// 银行类型下拉。对齐旧 bankcountrylist（全量枚举名表，非站内去重）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Type/Bank</returns>
    public Task<List<BankTypeOptionDto>> BankCountryListAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _ = _site.RequireSiteId();
        return Task.FromResult(BankTypeNameMap.All());
    }

    /// <summary>
    /// 超商城市。对齐旧 bankcitylist。
    /// </summary>
    /// <param name="type">超商类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选项</returns>
    public async Task<List<BankTypeOptionDto>> BankCityListAsync(
        int type,
        CancellationToken cancellationToken = default)
    {
        var cities = await _db.Queryable<SiteStore>()
            .Where(s => s.Type == type)
            .Select(s => s.City)
            .Distinct()
            .ToListAsync(cancellationToken);
        return cities
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .OrderBy(c => c)
            .Select(c => new BankTypeOptionDto { Type = type, Bank = c })
            .ToList();
    }

    /// <summary>
    /// 超商区县。对齐旧 bankdistrictlist。
    /// </summary>
    /// <param name="type">超商类型</param>
    /// <param name="city">城市</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选项</returns>
    public async Task<List<BankTypeOptionDto>> BankDistrictListAsync(
        int type,
        string city,
        CancellationToken cancellationToken = default)
    {
        var key = (city ?? string.Empty).Trim();
        var query = _db.Queryable<SiteStore>().Where(s => s.Type == type);
        if (!string.IsNullOrEmpty(key))
            query = query.Where(s => s.City == key);
        var districts = await query
            .Select(s => s.District)
            .Distinct()
            .ToListAsync(cancellationToken);
        return districts
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .OrderBy(d => d)
            .Select(d => new BankTypeOptionDto { Type = type, Bank = d })
            .ToList();
    }

    /// <summary>
    /// 超商门店地址。对齐旧 bankadresslist。
    /// </summary>
    /// <param name="type">超商类型</param>
    /// <param name="city">城市</param>
    /// <param name="district">区县</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选项（Type=门店编号，Bank=地址名）</returns>
    public async Task<List<BankTypeOptionDto>> BankAdressListAsync(
        int type,
        string city,
        string district,
        CancellationToken cancellationToken = default)
    {
        var cityKey = (city ?? string.Empty).Trim();
        var districtKey = (district ?? string.Empty).Trim();
        var query = _db.Queryable<SiteStore>().Where(s => s.Type == type);
        if (!string.IsNullOrEmpty(cityKey))
            query = query.Where(s => s.City == cityKey);
        if (!string.IsNullOrEmpty(districtKey))
            query = query.Where(s => s.District == districtKey);
        var rows = await query.OrderBy(s => s.ID).Take(500).ToListAsync(cancellationToken);
        return rows.Select(s => new BankTypeOptionDto
        {
            Type = s.ID,
            Bank = $"{s.Adress ?? string.Empty},{s.StoreName ?? string.Empty}".Trim(',')
        }).ToList();
    }

    /// <summary>
    /// 按 IsVir 返回添加弹层选项。对齐旧 bankcountryinfo：info.list[].bank 为「编号→名称」字典。
    /// </summary>
    /// <param name="country">国家名过滤（电子钱包）</param>
    /// <param name="isVir">0 银行 / 1 虚拟 / 2 电子 / 4 超商</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧站 { list:[{ bank, country }] } 形态</returns>
    public async Task<object> BankCountryInfoAsync(
        string country,
        string isVir,
        CancellationToken cancellationToken = default)
    {
        _ = country;
        var vir = string.IsNullOrWhiteSpace(isVir) ? "0" : isVir.Trim();
        Dictionary<string, string> bank;
        switch (vir)
        {
            case "1":
                // 虚拟币种：沿用 bankcountrylist 中常见虚拟段（不含电子钱包 200）。
                bank = BankTypeNameMap.Names
                    .Where(kv => kv.Key is >= 250 and <= 299)
                    .OrderBy(kv => kv.Key)
                    .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
                break;
            case "2":
                // 电子钱包：旧站 bankcountryinfo IsVir=2；当前字典已知支付类编号。
                // 国家元数据未迁全量前，先回「默认」一国，便于 FE 级联下拉（对齐旧 list[].country）。
                bank = BankTypeNameMap.Names
                    .Where(kv => kv.Key is 200 or 7007 or 8061)
                    .OrderBy(kv => kv.Key)
                    .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
                return new
                {
                    list = new[]
                    {
                        new { bank, country = "默认" }
                    }
                };
            case "4":
                {
                    var types = await _db.Queryable<SiteStore>()
                        .Select(s => s.Type)
                        .Distinct()
                        .ToListAsync(cancellationToken);
                    if (types.Count == 0)
                    {
                        // 库无门店时仍给出旧站常见超商类型，便于级联探活。
                        types = new List<int> { 13197, 13198 };
                    }

                    bank = types
                        .OrderBy(t => t)
                        .ToDictionary(
                            t => t.ToString(),
                            t => BankTypeNameMap.Names.TryGetValue(t, out var n) ? n : t.ToString());
                    break;
                }
            default:
                bank = new Dictionary<string, string>();
                break;
        }

        return new
        {
            list = new[]
            {
                new { bank, country = string.Empty }
            }
        };
    }
}

/// <summary>银行/超商下拉项。</summary>
public sealed class BankTypeOptionDto
{
    /// <summary>类型或门店编号。</summary>
    public int Type { get; set; }

    /// <summary>显示名。</summary>
    public string Bank { get; set; }
}

/// <summary>银行国家信息行。</summary>
public sealed class BankCountryInfoDto
{
    /// <summary>国家名。</summary>
    public string Country { get; set; }

    /// <summary>币种。</summary>
    public string Currency { get; set; }

    /// <summary>银行 JSON。</summary>
    public string Bank { get; set; }

    /// <summary>国家类型整型。</summary>
    public int Type { get; set; }
}

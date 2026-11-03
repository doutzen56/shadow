using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点提现接口列表。对齐旧 channelist 主字段。
/// </summary>
public sealed partial class WithdrawChannelService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>注入。</summary>
    public WithdrawChannelService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>列出本站提现渠道。</summary>
    public async Task<List<WithdrawChannelListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.SiteID == siteId && !c.IsDeleted)
            .OrderBy(c => c.Sort, OrderByType.Desc)
            .OrderBy(c => c.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var sysIds = rows.Where(r => r.SysID > 0).Select(r => r.SysID).Distinct().ToList();
        var sysNames = new Dictionary<int, string>();
        if (sysIds.Count > 0)
        {
            var sys = await _db.Queryable<SystemWithdraw>()
                .Where(s => sysIds.Contains(s.ID))
                .Select(s => new { s.ID, s.Name })
                .ToListAsync(cancellationToken);
            foreach (var s in sys)
                sysNames[s.ID] = s.Name ?? string.Empty;
        }

        return rows.Select(c => new WithdrawChannelListItemDto
        {
            ID = c.ID,
            Name = c.Name ?? string.Empty,
            SysID = c.SysID,
            SysName = sysNames.GetValueOrDefault(c.SysID, string.Empty),
            IsOpen = c.IsOpen,
            Cost = c.Cost,
            CreateAt = c.CreateAt,
            Currency = c.Currency,
            Sort = c.Sort
        }).ToList();
    }

    /// <summary>
    /// 当前开放的提现渠道。对齐旧 openchannelist。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>开放渠道</returns>
    public async Task<List<WithdrawChannelOpenItemDto>> ListOpenAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.SiteID == siteId && c.IsOpen && !c.IsDeleted)
            .OrderBy(c => c.Sort, OrderByType.Desc)
            .OrderBy(c => c.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(c => new WithdrawChannelOpenItemDto
        {
            ID = c.ID,
            Name = c.Name ?? string.Empty,
            Currency = c.Currency
        }).ToList();
    }

    /// <summary>
    /// 当前排序最高的开放渠道。对齐旧 openchannefirst。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>渠道；没有则 null</returns>
    public async Task<WithdrawChannelOpenItemDto> OpenFirstAsync(CancellationToken cancellationToken = default)
    {
        var list = await ListOpenAsync(cancellationToken);
        return list.FirstOrDefault();
    }

    /// <summary>
    /// 当前开放且挂了系统代付的渠道。对齐旧 openchannessystem。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>代付渠道</returns>
    public async Task<List<WithdrawChannelOpenItemDto>> ListOpenSystemAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.SiteID == siteId && c.IsOpen && !c.IsDeleted && c.SysID != 0)
            .OrderBy(c => c.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(c => new WithdrawChannelOpenItemDto
        {
            ID = c.ID,
            Name = c.Name ?? string.Empty,
            Currency = c.Currency
        }).ToList();
    }

    // --- merged from WithdrawChannelService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 withdrawtest。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> WithdrawtestAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return WithdrawtestCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：withdrawtest</summary>
    private Task<object?> WithdrawtestCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        throw Oops.Oh("#功能尚未接通#");
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

/// <summary>开放提现渠道简项。</summary>
public sealed class WithdrawChannelOpenItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }
}

/// <summary>提现渠道行。</summary>
public sealed class WithdrawChannelListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>系统代付编号。</summary>
    public int SysID { get; set; }

    /// <summary>系统代付名。</summary>
    public string SysName { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>成本比例。</summary>
    public decimal Cost { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }
}

/// <summary>
/// 站点提现银行配置。读写 site_Config.Bank 文本。
/// </summary>
public sealed class WithdrawBankService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>注入。</summary>
    public WithdrawBankService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>返回站点银行配置原文（旧页 JSON/枚举串）。</summary>
    public async Task<WithdrawBankConfigDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteConfig>()
            .Where(c => c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var cfg = rows.FirstOrDefault();
        return new WithdrawBankConfigDto
        {
            SiteID = siteId,
            Bank = cfg?.Bank ?? string.Empty
        };
    }

    /// <summary>
    /// 写入当前站银行配置。没有配置行就插入 SiteID+Bank。
    /// </summary>
    /// <param name="bank">银行配置原文</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(string bank, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var value = bank ?? string.Empty;
        var exists = await _db.Queryable<SiteConfig>().AnyAsync(c => c.SiteID == siteId, cancellationToken);
        if (!exists)
        {
            await _db.Insertable(new SiteConfig
            {
                SiteID = siteId,
                Bank = value
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var n = await _db.Updateable<SiteConfig>()
            .SetColumns(c => c.Bank == value)
            .Where(c => c.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#更新失败#");
    }
}

/// <summary>提现银行配置。</summary>
public sealed class WithdrawBankConfigDto
{
    /// <summary>站点。</summary>
    public int SiteID { get; set; }

    /// <summary>银行配置原文。</summary>
    public string Bank { get; set; }
}

/// <summary>保存提现银行配置入参。</summary>
public sealed class WithdrawBankSaveInput
{
    /// <summary>银行配置原文。</summary>
    public string Bank { get; set; }
}

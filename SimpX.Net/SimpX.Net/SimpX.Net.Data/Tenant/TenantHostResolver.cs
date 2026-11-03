namespace SimpX.Net.Data;

/// <summary>
/// 按 SysTenant.Host 绑定域名解析租户（支持一租户多域名；忽略大小写）。
/// </summary>
public sealed class TenantHostResolver : ITenantHostResolver, ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入 SqlSugar 客户端。
    /// </summary>
    /// <param name="db">库客户端</param>
    public TenantHostResolver(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<long?> ResolveTenantIdAsync(string host, CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(host, cancellationToken);
        return tenant?.Id;
    }

    /// <inheritdoc />
    public async Task<SysTenant> ResolveTenantAsync(string host, CancellationToken cancellationToken = default)
    {
        var normalized = TenantHostBinding.NormalizeOne(host);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var candidates = TenantHostBinding.RequestCandidates(normalized);

        // 多域名存在同一字段里，用内存匹配；租户数量通常不大。空 Host 不能按域名确定租户。
        var tenants = await _db.Queryable<SysTenant>()
            .Where(u => u.Status == StatusEnum.Enable && u.Host != null && u.Host != "")
            .ToListAsync(cancellationToken);

        return tenants.FirstOrDefault(u => TenantHostBinding.MatchesAny(u.Host, candidates));
    }
}

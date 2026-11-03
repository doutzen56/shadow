namespace SimpX.Net.Application.Admin;

/// <summary>
/// 按后台租户配置取独立库连接（租户库连接信息存在租户表并有缓存）。
/// </summary>
public sealed class TenantDbConnectionProvider : ITenantDbConnectionProvider, ITransient
{
    private readonly TenantAppService _tenantAppService;

    /// <summary>
    /// 注入租户服务。
    /// </summary>
    /// <param name="tenantAppService">租户服务</param>
    public TenantDbConnectionProvider(TenantAppService tenantAppService)
    {
        _tenantAppService = tenantAppService;
    }

    /// <summary>
    /// 获取租户的数据库连接；租户没有独立库时返回 null。
    /// </summary>
    /// <param name="tenantId">租户 Id</param>
    /// <returns>租户库连接或 null</returns>
    public SqlSugarScopeProvider GetConnectionScope(long tenantId) => _tenantAppService.GetTenantDbConnectionScope(tenantId);
}

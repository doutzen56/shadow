namespace SimpX.Net.Application.Admin;

/// <summary>
/// Admin 进程探活，以及本请求按域名确定的租户信息，供网关或运维查看当前进程职责。
/// </summary>
[ApiDescriptionSettings("Admin", Name = "Host", Order = 1)]
[AllowAnonymous]
public class HostInfoAppService : IDynamicApiController, ITransient
{
    private readonly ICurrentTenantAccessor _currentTenant;

    /// <summary>
    /// 注入当前请求租户上下文。
    /// </summary>
    /// <param name="currentTenant">请求处理链写入的租户访问器</param>
    public HostInfoAppService(ICurrentTenantAccessor currentTenant)
    {
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 返回当前进程角色、是否存活，以及本请求按域名确定的租户结果。
    /// </summary>
    [HttpGet("info")]
    public object Info()
    {
        return new
        {
            role = "Admin",
            alive = true,
            utc = DateTime.UtcNow,
            resolveByHost = HostTenantMode.ResolveByHost,
            isPlatformHost = HostTenantMode.IsPlatformHost,
            tenantId = _currentTenant.TenantId,
            host = _currentTenant.Host
        };
    }
}

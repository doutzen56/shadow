namespace SimpX.Net.Application.WebApi;

/// <summary>
/// WebApi 宿主探活与是否按域名确定租户的信息，供网关或运维检查当前进程职责。
/// </summary>
[ApiDescriptionSettings(Name = "Host", Order = 1)]
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
    /// 返回当前宿主角色、是否存活，以及本请求按域名确定到的租户。
    /// </summary>
    [HttpGet("info")]
    public object Info()
    {
        return new
        {
            role = "WebApi",
            alive = true,
            utc = DateTime.UtcNow,
            resolveByHost = HostTenantMode.ResolveByHost,
            tenantId = _currentTenant.TenantId,
            host = _currentTenant.Host
        };
    }
}

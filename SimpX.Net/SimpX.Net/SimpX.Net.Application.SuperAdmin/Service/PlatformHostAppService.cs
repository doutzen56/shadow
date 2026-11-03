namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台宿主信息（给前端确认当前是平台站还是租户站）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "host", Order = 10)]
public class PlatformHostAppService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 返回当前进程是否按域名确定租户。
    /// </summary>
    /// <returns>是否总后台、是否按域名确定租户</returns>
    [AllowAnonymous]
    [HttpGet]
    [DisplayName("获取宿主是否按域名确定租户")]
    public object GetMode()
    {
        return new
        {
            IsPlatformHost = HostTenantMode.IsPlatformHost,
            ResolveByHost = HostTenantMode.ResolveByHost,
        };
    }
}

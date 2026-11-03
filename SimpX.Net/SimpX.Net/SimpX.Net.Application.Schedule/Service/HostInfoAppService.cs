namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 调度宿主探活与角色信息，供网关或运维检查当前进程职责。
/// </summary>
[ApiDescriptionSettings(Name = "Host", Order = 1)]
[AllowAnonymous]
public class HostInfoAppService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 返回当前宿主角色与进程是否存活。
    /// </summary>
    [HttpGet("info")]
    public object Info()
    {
        return new
        {
            role = "Schedule",
            alive = true,
            utc = DateTime.UtcNow
        };
    }
}

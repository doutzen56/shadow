namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台系统通知只读（JWT）。对齐旧版 /admin/system/updatenews。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "updateNews", Order = 477, Description = "系统通知")]
public class SystemUpdateNewsAppService : IDynamicApiController, ITransient
{
    private readonly SystemUpdateNewsService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public SystemUpdateNewsAppService(SystemUpdateNewsService svc) => _svc = svc;

    /// <summary>
    /// 列表。可按语种过滤。
    /// </summary>
    /// <param name="language">语种；不传则全部</param>
    /// <returns>列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpGet]
    [DisplayName("系统通知列表")]
    public Task<List<SystemUpdateNewsItemDto>> List([FromQuery] int? language = null) =>
        _svc.ListAsync(language, null, App.HttpContext?.RequestAborted ?? default);
}

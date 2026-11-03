namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台站点域名（JWT）。对齐旧版 /admin/site/domain/list|info|save|delete。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteDomain", Order = 468, Description = "域名管理")]
public class SiteDomainAppService : IDynamicApiController, ITransient
{
    private readonly SiteDomainService _svc;

    /// <summary>
    /// 注入域名服务。
    /// </summary>
    /// <param name="svc">域名服务</param>
    public SiteDomainAppService(SiteDomainService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("站点域名列表")]
    public Task<List<SiteDomainItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("站点域名详情")]
    public Task<SiteDomainDetailDto> Info([FromQuery] int id) =>
        _svc.InfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存站点域名")]
    public Task Save(SiteDomainSaveInput input) =>
        _svc.SaveAsync(input ?? new SiteDomainSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除站点域名")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}

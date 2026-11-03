namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台活动栏目（JWT）。对齐旧版 /admin/award/events/columnlist|columnsave|columndelete。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteEventColumn", Order = 464, Description = "活动栏目")]
public class SiteEventColumnAppService : IDynamicApiController, ITransient
{
    private readonly SiteEventColumnService _svc;

    /// <summary>注入服务。</summary>
    /// <param name="svc">服务</param>
    public SiteEventColumnAppService(SiteEventColumnService svc) => _svc = svc;

    /// <summary>栏目列表。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("活动栏目列表")]
    public Task<List<SiteEventColumnListItemDto>> List() =>
        _svc.ColumnListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存栏目。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存活动栏目")]
    public Task Save(SiteEventColumnSaveInput input) =>
        _svc.SaveAsync(input ?? new SiteEventColumnSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除栏目。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除活动栏目")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台内容区域（JWT）。对齐旧版 /admin/site/region 与 regioncontent。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteRegion", Order = 476, Description = "内容区域")]
public class SiteRegionAppService : IDynamicApiController, ITransient
{
    private readonly SiteRegionService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public SiteRegionAppService(SiteRegionService svc) => _svc = svc;

    /// <summary>
    /// 区域分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("内容区域分页")]
    public Task<SiteRegionPageOutput> Page(SiteRegionPageInput input) =>
        _svc.PageAsync(input ?? new SiteRegionPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 区域详情。
    /// </summary>
    /// <param name="id">区域编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("内容区域详情")]
    public Task<SiteRegionDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存区域。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存内容区域")]
    public Task Save(SiteRegionSaveInput input) =>
        _svc.SaveAsync(input ?? new SiteRegionSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除区域。
    /// </summary>
    /// <param name="id">区域编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除内容区域")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 区域内容列表。
    /// </summary>
    /// <param name="regionId">区域编号</param>
    /// <returns>内容行</returns>
    [ApiDescriptionSettings(Name = "ContentList"), HttpGet]
    [DisplayName("区域内容列表")]
    public Task<List<SiteRegionContentItemDto>> ContentList([FromQuery] int regionId) =>
        _svc.ContentListAsync(regionId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存区域内容。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "ContentSave"), HttpPost]
    [DisplayName("保存区域内容")]
    public Task ContentSave(SiteRegionContentSaveInput input) =>
        _svc.ContentSaveAsync(input ?? new SiteRegionContentSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除区域内容。
    /// </summary>
    /// <param name="id">内容编号</param>
    [ApiDescriptionSettings(Name = "ContentDelete"), HttpPost]
    [DisplayName("删除区域内容")]
    public Task ContentDelete([FromQuery] int id) =>
        _svc.ContentDeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台帮助中心（JWT）。对齐旧版 /admin/site/help/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteHelp", Order = 466, Description = "帮助中心")]
public partial class SiteHelpAppService : IDynamicApiController, ITransient
{
    private readonly SiteHelpService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public SiteHelpAppService(SiteHelpService svc) => _svc = svc;

    /// <summary>
    /// 帮助列表。
    /// </summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("帮助列表")]
    public Task<List<SiteHelpListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 栏目列表。
    /// </summary>
    /// <returns>栏目</returns>
    [ApiDescriptionSettings(Name = "ColumnList"), HttpPost]
    [DisplayName("帮助栏目")]
    public Task<List<SiteHelpColumnItemDto>> ColumnList() =>
        _svc.ColumnListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("帮助详情")]
    public Task<SiteHelpDetailDto> Info([FromQuery] int id) =>
        _svc.InfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存帮助")]
    public Task Save(SiteHelpSaveInput input) =>
        _svc.SaveAsync(input ?? new SiteHelpSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除帮助")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);

    // --- merged from SiteHelpAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 columnsort。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Columnsort"), HttpPost]
    [DisplayName("columnsort")]
    public Task<object?> Columnsort(Dictionary<string, object?>? input = null) =>
        _svc.ColumnsortAsync(input, App.HttpContext?.RequestAborted ?? default);
}

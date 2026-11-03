using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台积分项 / 日志 / 导入（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "sitePointItem", Order = 546, Description = "站点积分")]
public partial class SitePointItemAppService : IDynamicApiController, ITransient
{
    private readonly SitePointItemService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">积分</param>
    public SitePointItemAppService(SitePointItemService svc) => _svc = svc;

    /// <summary>读取积分项。</summary>
    /// <returns>配置</returns>
    [ApiDescriptionSettings(Name = "Get"), HttpPost]
    [DisplayName("积分项读取")]
    public Task<SitePointItemDto> Get() =>
        _svc.GetAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存积分项。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("积分项保存")]
    public Task Save(SitePointItemSaveInput input) =>
        _svc.SaveAsync(input ?? new SitePointItemSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>积分日志分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageLogs"), HttpPost]
    [DisplayName("积分日志分页")]
    public Task<PointLogPageOutput> PageLogs(PointLogPageInput input) =>
        _svc.PageLogsAsync(input ?? new PointLogPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>积分导入。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "ImportPoint"), HttpPost]
    [DisplayName("积分导入")]
    public Task ImportPoint(PointImportInput input) =>
        _svc.ImportPointAsync(input ?? new PointImportInput(), App.HttpContext?.RequestAborted ?? default);

    // --- merged from SitePointItemAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 clearallusers。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Clearallusers"), HttpPost]
    [DisplayName("clearallusers")]
    public Task<object?> Clearallusers(Dictionary<string, object?>? input = null) =>
        _svc.ClearallusersAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 list。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("list")]
    public Task<object?> List(Dictionary<string, object?>? input = null) =>
        _svc.ListAsync(input, App.HttpContext?.RequestAborted ?? default);
}

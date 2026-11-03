namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台优惠活动（JWT）。对齐旧版 /admin/award/events/list|info|save|delete。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteEvent", Order = 463, Description = "优惠列表")]
public partial class SiteEventAppService : IDynamicApiController, ITransient
{
    private readonly SiteEventService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public SiteEventAppService(SiteEventService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("优惠活动列表")]
    public Task<List<SiteEventListItemDto>> List(SiteEventListInput input) =>
        _svc.ListAsync(input ?? new SiteEventListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="type">新建时的模板类型</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("优惠活动详情")]
    public Task<SiteEventDetailDto> Info([FromQuery] int id, [FromQuery] int type = 0) =>
        _svc.InfoAsync(id, type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存优惠活动")]
    public Task Save(SiteEventSaveInput input) =>
        _svc.SaveAsync(input ?? new SiteEventSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除优惠活动")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);

    // --- merged from SiteEventAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 filtergame。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Filtergame"), HttpPost]
    [DisplayName("filtergame")]
    public Task<object?> Filtergame(Dictionary<string, object?>? input = null) =>
        _svc.FiltergameAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 getproductcount。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getproductcount"), HttpPost]
    [DisplayName("getproductcount")]
    public Task<object?> Getproductcount(Dictionary<string, object?>? input = null) =>
        _svc.GetproductcountAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 initplatform。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Initplatform"), HttpPost]
    [DisplayName("initplatform")]
    public Task<object?> Initplatform(Dictionary<string, object?>? input = null) =>
        _svc.InitplatformAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 inittransfer。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Inittransfer"), HttpPost]
    [DisplayName("inittransfer")]
    public Task<object?> Inittransfer(Dictionary<string, object?>? input = null) =>
        _svc.InittransferAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 pointlog。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Pointlog"), HttpPost]
    [DisplayName("pointlog")]
    public Task<object?> Pointlog(Dictionary<string, object?>? input = null) =>
        _svc.PointlogAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 show0。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Show0"), HttpPost]
    [DisplayName("show0")]
    public Task<object?> Show0(Dictionary<string, object?>? input = null) =>
        _svc.Show0Async(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 show1。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Show1"), HttpPost]
    [DisplayName("show1")]
    public Task<object?> Show1(Dictionary<string, object?>? input = null) =>
        _svc.Show1Async(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 show2。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Show2"), HttpPost]
    [DisplayName("show2")]
    public Task<object?> Show2(Dictionary<string, object?>? input = null) =>
        _svc.Show2Async(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 showtransfer0。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Showtransfer0"), HttpPost]
    [DisplayName("showtransfer0")]
    public Task<object?> Showtransfer0(Dictionary<string, object?>? input = null) =>
        _svc.Showtransfer0Async(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatedemo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatedemo"), HttpPost]
    [DisplayName("updatedemo")]
    public Task<object?> Updatedemo(Dictionary<string, object?>? input = null) =>
        _svc.UpdatedemoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updateproductcount。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updateproductcount"), HttpPost]
    [DisplayName("updateproductcount")]
    public Task<object?> Updateproductcount(Dictionary<string, object?>? input = null) =>
        _svc.UpdateproductcountAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatesort。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatesort"), HttpPost]
    [DisplayName("updatesort")]
    public Task<object?> Updatesort(Dictionary<string, object?>? input = null) =>
        _svc.UpdatesortAsync(input, App.HttpContext?.RequestAborted ?? default);
}

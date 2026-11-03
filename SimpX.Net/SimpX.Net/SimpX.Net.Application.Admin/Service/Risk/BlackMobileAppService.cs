namespace SimpX.Net.Application.Admin;

/// <summary>
/// 手机黑名单（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "blackMobile", Order = 467, Description = "手机黑名单")]
public partial class BlackMobileAppService : IDynamicApiController, ITransient
{
    private readonly BlackMobileService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public BlackMobileAppService(BlackMobileService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <param name="keyword">手机号关键字</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("手机黑名单列表")]
    public Task<List<BlackMobileListItemDto>> List([FromQuery] string? keyword) =>
        _svc.ListAsync(keyword, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存手机黑名单")]
    public Task Save(BlackMobileSaveInput input) =>
        _svc.SaveAsync(input ?? new BlackMobileSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除手机黑名单")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}

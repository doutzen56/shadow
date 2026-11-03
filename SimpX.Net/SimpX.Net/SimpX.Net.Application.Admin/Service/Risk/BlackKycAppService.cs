namespace SimpX.Net.Application.Admin;

/// <summary>
/// 证件黑名单（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "blackKyc", Order = 468, Description = "证件黑名单")]
public partial class BlackKycAppService : IDynamicApiController, ITransient
{
    private readonly BlackKycService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public BlackKycAppService(BlackKycService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <param name="keyword">证件号关键字</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("证件黑名单列表")]
    public Task<List<BlackKycListItemDto>> List([FromQuery] string? keyword) =>
        _svc.ListAsync(keyword, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存证件黑名单")]
    public Task Save(BlackKycSaveInput input) =>
        _svc.SaveAsync(input ?? new BlackKycSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除证件黑名单")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}

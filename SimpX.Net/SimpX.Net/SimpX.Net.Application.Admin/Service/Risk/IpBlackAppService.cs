namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台 IP 黑名单（JWT）。对齐旧版 /admin/risk/ip/blacklist|blackinfo|saveblackip|deleteblackip。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "ipBlack", Order = 464, Description = "IP黑名单")]
public partial class IpBlackAppService : IDynamicApiController, ITransient
{
    private readonly IpBlackService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public IpBlackAppService(IpBlackService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("IP黑名单列表")]
    public Task<List<IpBlackListItemDto>> List() =>
        _svc.ListAsync(keyword: null, cancellationToken: App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("IP黑名单详情")]
    public Task<IpBlackListItemDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存IP黑名单")]
    public Task Save(IpBlackSaveInput input) =>
        _svc.SaveAsync(input ?? new IpBlackSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除IP黑名单")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}

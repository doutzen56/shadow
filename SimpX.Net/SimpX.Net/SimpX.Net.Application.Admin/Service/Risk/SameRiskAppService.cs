namespace SimpX.Net.Application.Admin;

/// <summary>
/// 同 IP / 同设备查询（JWT）。对齐旧 /admin/risk/ip/same、/admin/risk/device/same。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "sameRisk", Order = 466, Description = "同IP同设备查询")]
public class SameRiskAppService : IDynamicApiController, ITransient
{
    private readonly SameRiskService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public SameRiskAppService(SameRiskService svc) => _svc = svc;

    /// <summary>
    /// 同 IP 查询。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>命中会员</returns>
    [ApiDescriptionSettings(Name = "SameIp"), HttpPost]
    [DisplayName("同IP查询")]
    public Task<List<SameRiskListItemDto>> SameIp(SameRiskQueryInput input) =>
        _svc.SameIpAsync(input ?? new SameRiskQueryInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 同设备查询。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>命中会员</returns>
    [ApiDescriptionSettings(Name = "SameDevice"), HttpPost]
    [DisplayName("同设备查询")]
    public Task<List<SameRiskListItemDto>> SameDevice(SameRiskQueryInput input) =>
        _svc.SameDeviceAsync(input ?? new SameRiskQueryInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 同设备对比详情。对齐旧 device/detail、ip/detail。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpPost]
    [DisplayName("同设备详情")]
    public Task<List<SameRiskListItemDto>> Detail(SameRiskQueryInput input) =>
        _svc.SameDeviceAsync(input ?? new SameRiskQueryInput(), App.HttpContext?.RequestAborted ?? default);
}

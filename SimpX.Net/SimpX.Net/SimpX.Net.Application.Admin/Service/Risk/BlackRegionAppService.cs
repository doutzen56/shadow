namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台国别限制（JWT）。对齐旧 /admin/risk/ip/getblackregion|saveblackregion。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "blackRegion", Order = 465, Description = "国别设置")]
public class BlackRegionAppService : IDynamicApiController, ITransient
{
    private readonly BlackRegionService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public BlackRegionAppService(BlackRegionService svc) => _svc = svc;

    /// <summary>
    /// 已限制地区列表。
    /// </summary>
    /// <returns>地区名</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("国别限制列表")]
    public Task<BlackRegionListOutput> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 加入或移除地区。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存国别限制")]
    public Task Save(BlackRegionSaveInput input) =>
        _svc.SaveAsync(input ?? new BlackRegionSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 getblackregion。</summary>
    /// <returns>地区</returns>
    [ApiDescriptionSettings(Name = "GetBlackRegion"), HttpPost]
    [DisplayName("国别列表别名")]
    public Task<BlackRegionListOutput> GetBlackRegion() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 saveblackregion。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "SaveBlackRegion"), HttpPost]
    [DisplayName("保存国别别名")]
    public Task SaveBlackRegion(BlackRegionSaveInput input) =>
        _svc.SaveAsync(input ?? new BlackRegionSaveInput(), App.HttpContext?.RequestAborted ?? default);
}

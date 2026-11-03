namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台返水设定（JWT）。配置读写，不发放。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "rebateSetting", Order = 481, Description = "返水设定")]
public class RebateSettingAppService : IDynamicApiController, ITransient
{
    private readonly RebateSettingService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">返水设定</param>
    public RebateSettingAppService(RebateSettingService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpGet]
    [DisplayName("返水设定列表")]
    public Task<List<RebateSettingListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("返水设定详情")]
    public Task<RebateSettingDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("返水设定保存")]
    public Task<int> Save(RebateSettingSaveInput input) =>
        _svc.SaveAsync(input ?? new RebateSettingSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>改自动发放。</summary>
    /// <param name="input">编号与开关</param>
    [ApiDescriptionSettings(Name = "UpdateAuto"), HttpPost]
    [DisplayName("返水设定改自动")]
    public Task UpdateAuto(RebateSettingUpdateAutoInput input) =>
        _svc.UpdateAutoAsync(input?.ID ?? 0, input?.IsAuto ?? false, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 切换启用/停用。对齐旧 rebate/updateenabled。
    /// </summary>
    /// <param name="input">编号</param>
    /// <returns>切换后是否启用</returns>
    [ApiDescriptionSettings(Name = "UpdateEnabled"), HttpPost]
    [DisplayName("返水设定切换启用")]
    public Task<bool> UpdateEnabled(RebateSettingDeleteInput input) =>
        _svc.ToggleEnabledAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("返水设定删除")]
    public Task Delete(RebateSettingDeleteInput input) =>
        _svc.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 返水类型字典。对齐旧 returntype。
    /// </summary>
    /// <returns>键值</returns>
    [ApiDescriptionSettings(Name = "ReturnType"), HttpGet]
    [DisplayName("返水类型字典")]
    public object ReturnType() => _svc.ReturnType();
}

/// <summary>改自动发放入参。</summary>
public sealed class RebateSettingUpdateAutoInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>是否自动。</summary>
    public bool IsAuto { get; set; }
}

/// <summary>删除返水设定入参。</summary>
public sealed class RebateSettingDeleteInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }
}

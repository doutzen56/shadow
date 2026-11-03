namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台参数配置（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemConfig", Order = 489, Description = "参数配置")]
public class PlatformSystemConfigAppService : IDynamicApiController, ITransient
{
    private readonly SystemConfigService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">参数配置</param>
    public PlatformSystemConfigAppService(SystemConfigService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="name">名称模糊</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpGet]
    [DisplayName("平台参数配置列表")]
    public Task<List<SystemConfigItemDto>> List([FromQuery] string name = null) =>
        _svc.ListAsync(name, App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="name">键名</param>
    /// <returns>配置</returns>
    [ApiDescriptionSettings(Name = "Get"), HttpGet]
    [DisplayName("平台参数配置详情")]
    public Task<SystemConfigItemDto> Get([FromQuery] string name) =>
        _svc.GetAsync(name, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("平台参数配置保存")]
    public Task Save(SystemConfigSaveInput input) =>
        _svc.SaveAsync(input ?? new SystemConfigSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="input">键名</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("平台参数配置删除")]
    public Task Delete(SystemConfigDeleteInput input) =>
        _svc.DeleteAsync(input?.Name, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>删除配置入参。</summary>
public sealed class SystemConfigDeleteInput
{
    /// <summary>键名。</summary>
    public string Name { get; set; }
}

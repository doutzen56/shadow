namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台语种翻译配置（JWT）。只读写 sys_Config，不调机翻。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemTranslateConfig", Order = 480, Description = "语种翻译")]
public class PlatformSystemTranslateConfigAppService : IDynamicApiController, ITransient
{
    private readonly SystemTranslateConfigService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">语种翻译配置</param>
    public PlatformSystemTranslateConfigAppService(SystemTranslateConfigService svc) => _svc = svc;

    /// <summary>读取配置。</summary>
    /// <param name="channel">频道；可空</param>
    /// <returns>配置</returns>
    [ApiDescriptionSettings(Name = "Get"), HttpGet]
    [DisplayName("平台语种翻译配置")]
    public Task<SystemTranslateConfigDto> Get([FromQuery] string channel = null) =>
        _svc.GetAsync(channel, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存配置。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("平台语种翻译保存")]
    public Task Save(SystemTranslateConfigSaveInput input) =>
        _svc.SaveAsync(input ?? new SystemTranslateConfigSaveInput(), App.HttpContext?.RequestAborted ?? default);
}

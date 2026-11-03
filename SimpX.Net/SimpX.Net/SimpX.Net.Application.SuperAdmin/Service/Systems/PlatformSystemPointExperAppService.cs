namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台积分项配置（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemPointExper", Order = 484, Description = "积分项")]
public class PlatformSystemPointExperAppService : IDynamicApiController, ITransient
{
    private readonly SystemPointExperService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">积分项</param>
    public PlatformSystemPointExperAppService(SystemPointExperService svc) => _svc = svc;

    /// <summary>读取。</summary>
    /// <returns>配置</returns>
    [ApiDescriptionSettings(Name = "Get"), HttpGet]
    [DisplayName("平台积分项配置")]
    public Task<SystemPointExperDto> Get() =>
        _svc.GetAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("平台积分项保存")]
    public Task<int> Save(SystemPointExperSaveInput input) =>
        _svc.SaveAsync(input ?? new SystemPointExperSaveInput(), App.HttpContext?.RequestAborted ?? default);
}

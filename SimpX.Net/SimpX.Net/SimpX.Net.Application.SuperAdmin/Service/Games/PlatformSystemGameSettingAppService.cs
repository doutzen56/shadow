namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台游戏配置（JWT，不定站）。对齐旧 Web.System /setting/game/ 列表主能力。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemGameSetting", Order = 472, Description = "游戏配置")]
public class PlatformSystemGameSettingAppService : IDynamicApiController, ITransient
{
    private readonly SystemGameSettingService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public PlatformSystemGameSettingAppService(SystemGameSettingService svc) => _svc = svc;

    /// <summary>
    /// 系统游戏配置列表。
    /// </summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("平台游戏配置列表")]
    public Task<List<SystemGameSettingListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);
}

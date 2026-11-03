namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台厂商 API（JWT）。对齐旧版 /admin/game/setting/gamesetlist|info。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "gameSetting", Order = 459, Description = "厂商API")]
public partial class GameSettingAppService : IDynamicApiController, ITransient
{
    private readonly GameSettingService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public GameSettingAppService(GameSettingService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// 列表。对齐旧版 gamesetlist。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("厂商API列表")]
    public Task<List<GameSettingListItemDto>> List(GameSettingListInput input) =>
        _svc.ListAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。对齐旧版 info（Type=GameType 整型）。
    /// </summary>
    /// <param name="type">游戏类型</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("厂商API详情")]
    public Task<GameSettingDetailDto> Detail([FromQuery] int type) =>
        _svc.DetailAsync(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。对齐旧版 /admin/game/setting/save。
    /// </summary>
    /// <param name="input">入参</param>
    /// <returns>任务</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存厂商API")]
    public Task Save(GameSettingSaveInput input) =>
        _svc.SaveAsync(input ?? new GameSettingSaveInput(), App.HttpContext?.RequestAborted ?? default);
}

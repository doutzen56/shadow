namespace SimpX.Net.Application.Admin;

/// <summary>
/// 厂商 API 旧动作名。
/// </summary>
public partial class GameSettingAppService
{
    /// <summary>对齐旧 gamesetlist。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GameSetList"), HttpPost]
    [DisplayName("厂商API列表别名")]
    public Task<List<GameSettingListItemDto>> GameSetList(GameSettingListInput input) =>
        _svc.ListAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 info。</summary>
    /// <param name="type">类型</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpGet]
    [DisplayName("厂商API详情别名")]
    public Task<GameSettingDetailDto> Info([FromQuery] int type) =>
        _svc.DetailAsync(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 gamelist。</summary>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GameList"), HttpPost]
    [DisplayName("游戏简表")]
    public Task<List<GameSettingOpenItemDto>> GameList() =>
        _svc.GameListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 gameopenlist。</summary>
    /// <param name="cate">分类</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GameOpenList"), HttpPost]
    [DisplayName("开通游戏下拉")]
    public Task<List<GameSettingOpenItemDto>> GameOpenList([FromQuery] string? cate) =>
        _svc.GameOpenListAsync(cate, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 gameopenlistreward。</summary>
    /// <param name="cate">分类</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GameOpenListReward"), HttpPost]
    [DisplayName("打赏开通下拉")]
    public Task<List<GameSettingOpenItemDto>> GameOpenListReward([FromQuery] string? cate) =>
        _svc.GameOpenListRewardAsync(cate, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 gamelistbycategory。</summary>
    /// <param name="category">分类</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GameListByCategory"), HttpPost]
    [DisplayName("按分类游戏")]
    public Task<List<GameSettingOpenItemDto>> GameListByCategory([FromQuery] string? category) =>
        _svc.GameListByCategoryAsync(category, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 gamesort。</summary>
    /// <param name="type">CSV</param>
    [ApiDescriptionSettings(Name = "GameSort"), HttpPost]
    [DisplayName("厂商排序")]
    public Task GameSort([FromQuery] string? type) =>
        _svc.GameSortAsync(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 gamecurrencylist。</summary>
    /// <param name="type">类型</param>
    /// <returns>币种</returns>
    [ApiDescriptionSettings(Name = "GameCurrencyList"), HttpPost]
    [DisplayName("游戏币种")]
    public Task<List<string>> GameCurrencyList([FromQuery] int type) =>
        _svc.GameCurrencyListAsync(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 maintaininfo。</summary>
    /// <param name="type">类型</param>
    /// <returns>维护</returns>
    [ApiDescriptionSettings(Name = "MaintainInfo"), HttpPost]
    [DisplayName("维护信息")]
    public Task<GameMaintainDto> MaintainInfo([FromQuery] int type) =>
        _svc.MaintainInfoAsync(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 savemaintain。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "SaveMaintain"), HttpPost]
    [DisplayName("保存维护")]
    public Task SaveMaintain(GameMaintainSaveInput input) =>
        _svc.SaveMaintainAsync(input ?? new GameMaintainSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 gametag。</summary>
    /// <param name="type">类型</param>
    /// <returns>标签</returns>
    [ApiDescriptionSettings(Name = "GameTag"), HttpPost]
    [DisplayName("游戏标签")]
    public Task<GameTagSettingDto> GameTag([FromQuery] int type) =>
        _svc.GameTagAsync(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 setgametag。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "SetGameTag"), HttpPost]
    [DisplayName("保存游戏标签")]
    public Task SetGameTag(GameTagSettingDto input) =>
        _svc.SetGameTagAsync(input ?? new GameTagSettingDto(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 sitegamecate。</summary>
    /// <returns>分类名</returns>
    [ApiDescriptionSettings(Name = "SiteGameCate"), HttpPost]
    [DisplayName("站点游戏分类名")]
    public Task<List<string>> SiteGameCate() =>
        _svc.SiteGameCateNamesAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>流水配置占位。对齐旧 washflow。</summary>
    /// <returns>空列表</returns>
    [ApiDescriptionSettings(Name = "WashFlow"), HttpPost]
    [DisplayName("流水比例占位")]
    public object WashFlow() => new { data = Array.Empty<object>(), message = "#流水比例表未迁#" };

    /// <summary>保存流水占位。</summary>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "SaveWashFlow"), HttpPost]
    [DisplayName("保存流水占位")]
    public object SaveWashFlow() => new { message = "#流水比例表未迁#" };

    /// <summary>游戏初始化占位。对齐旧 gameinit。</summary>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "GameInit"), HttpPost]
    [DisplayName("游戏初始化占位")]
    public object GameInit() => new { message = "#请用总后台系统游戏配置#" };

    /// <summary>批量转出占位。对齐旧 transfergamemoneytocenter。</summary>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "TransferGameMoneyToCenter"), HttpPost]
    [DisplayName("批量转出占位")]
    public object TransferGameMoneyToCenter() => new { message = "#请用转账动作逐笔处理#" };
}

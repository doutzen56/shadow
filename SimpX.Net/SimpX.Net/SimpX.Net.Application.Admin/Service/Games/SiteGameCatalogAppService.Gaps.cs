using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 游戏列表旧动作名。
/// </summary>
public partial class SiteGameCatalogAppService
{
    /// <summary>列表。对齐旧 getlist。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GetList"), HttpPost]
    [DisplayName("游戏列表")]
    public Task<List<SiteGameListDetailDto>> GetList(SiteGameListFilterInput input) =>
        _svc.GetListAsync(input ?? new SiteGameListFilterInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。对齐旧 gamelistinfo。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "GameListInfo"), HttpPost]
    [DisplayName("游戏列表详情")]
    public Task<SiteGameListDetailDto> GameListInfo([FromQuery] int id) =>
        _svc.GameListInfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>分类树。对齐旧 getcatetree。</summary>
    /// <returns>树</returns>
    [ApiDescriptionSettings(Name = "GetCateTree"), HttpPost]
    [DisplayName("游戏分类树")]
    public Task<List<SiteGameCateTreeDto>> GetCateTree() =>
        _svc.GetCateTreeAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。对齐旧 savegamelist。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveGameList"), HttpPost]
    [DisplayName("保存游戏列表")]
    public Task SaveGameList(SiteGameListDetailSaveInput input) =>
        _svc.SaveGameListAsync(input ?? new SiteGameListDetailSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>排序。对齐旧 updatesort。</summary>
    /// <param name="id">编号</param>
    /// <param name="isTop">置顶</param>
    [ApiDescriptionSettings(Name = "UpdateSort"), HttpPost]
    [DisplayName("游戏列表排序")]
    public Task UpdateSort([FromQuery] int id, [FromQuery] bool isTop) =>
        _svc.UpdateSortAsync(id, isTop, App.HttpContext?.RequestAborted ?? default);

    /// <summary>推荐列表。</summary>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GetRecommendList"), HttpPost]
    [DisplayName("推荐游戏")]
    public Task<List<SiteGameListDetailDto>> GetRecommendList() =>
        _svc.GetRecommendListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存推荐。</summary>
    /// <param name="recommends">id:value CSV</param>
    [ApiDescriptionSettings(Name = "SaveRecommendList"), HttpPost]
    [DisplayName("保存推荐")]
    public Task SaveRecommendList([FromQuery] string? recommends) =>
        _svc.SaveRecommendListAsync(recommends, App.HttpContext?.RequestAborted ?? default);

    /// <summary>最新列表。</summary>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GetNewList"), HttpPost]
    [DisplayName("最新游戏")]
    public Task<List<SiteGameListDetailDto>> GetNewList() =>
        _svc.GetNewListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存最新。</summary>
    /// <param name="news">id:value CSV</param>
    [ApiDescriptionSettings(Name = "SaveNewList"), HttpPost]
    [DisplayName("保存最新")]
    public Task SaveNewList([FromQuery] string? news) =>
        _svc.SaveNewListAsync(news, App.HttpContext?.RequestAborted ?? default);

    /// <summary>补预设。</summary>
    /// <param name="type">类型</param>
    /// <returns>新增数</returns>
    [ApiDescriptionSettings(Name = "CreateGameList"), HttpPost]
    [DisplayName("创建游戏列表")]
    public Task<int> CreateGameList([FromQuery] int type) =>
        _svc.CreateGameListAsync(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>全量补预设。</summary>
    /// <returns>新增数</returns>
    [ApiDescriptionSettings(Name = "CreateGameListAll"), HttpPost]
    [DisplayName("全量创建游戏列表")]
    public Task<int> CreateGameListAll() =>
        _svc.CreateGameListAllAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>刷新缓存占位。</summary>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "RefreshCache"), HttpPost]
    [DisplayName("刷新游戏缓存")]
    public Task<string> RefreshCache() => _svc.RefreshCacheAsync();

    /// <summary>分类开关。对齐旧 cate/updatestatus。</summary>
    /// <param name="id">编号</param>
    /// <param name="type">1开启 2PC 3App</param>
    /// <param name="value">开/关</param>
    [ApiDescriptionSettings(Name = "UpdateStatus"), HttpPost]
    [DisplayName("分类开关")]
    public Task UpdateStatus([FromQuery] int id, [FromQuery] int type, [FromQuery] bool value) =>
        _svc.UpdateCateStatusAsync(id, type, value, App.HttpContext?.RequestAborted ?? default);

    /// <summary>厂商透传占位。对齐旧 game/api/action（需厂商适配）。</summary>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "Action"), HttpPost]
    [DisplayName("游戏API透传占位")]
    public object Action() => new { message = "#厂商透传待接 Gateway 适配#" };
}

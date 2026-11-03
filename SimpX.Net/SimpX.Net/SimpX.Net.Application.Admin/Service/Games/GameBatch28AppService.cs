using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 游戏分类/列表/预设（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteGameCatalog", Order = 554, Description = "游戏目录")]
public partial class SiteGameCatalogAppService : IDynamicApiController, ITransient
{
    private readonly SiteGameCatalogService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">目录</param>
    public SiteGameCatalogAppService(SiteGameCatalogService svc) => _svc = svc;

    /// <summary>分类。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListCates"), HttpPost]
    [DisplayName("游戏分类列表")]
    public Task<List<SiteGameCateItemDto>> ListCates() =>
        _svc.ListCatesAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存分类。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveCate"), HttpPost]
    [DisplayName("游戏分类保存")]
    public Task SaveCate(SiteGameCateSaveInput input) =>
        _svc.SaveCateAsync(input ?? new SiteGameCateSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>游戏分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageGames"), HttpPost]
    [DisplayName("游戏列表分页")]
    public Task<SiteGameListPageOutput> PageGames(SiteGameListPageInput input) =>
        _svc.PageGamesAsync(input ?? new SiteGameListPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存游戏开关。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveGame"), HttpPost]
    [DisplayName("游戏预设保存")]
    public Task SaveGame(SiteGameListSaveInput input) =>
        _svc.SaveGameAsync(input ?? new SiteGameListSaveInput(), App.HttpContext?.RequestAborted ?? default);
}

/// <summary>
/// 游戏注单汇总与报表文件（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "gameOrderSummary", Order = 555, Description = "游戏注单汇总")]
public partial class GameOrderSummaryAppService : IDynamicApiController, ITransient
{
    private readonly GameOrderSummaryService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">汇总</param>
    public GameOrderSummaryAppService(GameOrderSummaryService svc) => _svc = svc;

    /// <summary>注单汇总分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("游戏注单汇总")]
    public Task<GameOrderSummaryPageOutput> Page(GameOrderSummaryPageInput input) =>
        _svc.PageAsync(input ?? new GameOrderSummaryPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>报表文件。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListFiles"), HttpPost]
    [DisplayName("报表文件列表")]
    public Task<List<SiteFileReportItemDto>> ListFiles() =>
        _svc.ListFilesAsync(App.HttpContext?.RequestAborted ?? default);
}

/// <summary>
/// 代理团队（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "proxyTeam", Order = 556, Description = "代理团队")]
public class ProxyTeamAppService : IDynamicApiController, ITransient
{
    private readonly ProxyTeamService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">团队</param>
    public ProxyTeamAppService(ProxyTeamService svc) => _svc = svc;

    /// <summary>下级列表。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("代理团队列表")]
    public Task<List<ProxyTeamItemDto>> List(ProxyTeamListInput input) =>
        _svc.ListAsync(input ?? new ProxyTeamListInput(), App.HttpContext?.RequestAborted ?? default);
}

/// <summary>
/// 首页汇总（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "homeDashboard", Order = 557, Description = "首页汇总")]
public class HomeDashboardAppService : IDynamicApiController, ITransient
{
    private readonly HomeDashboardService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">首页</param>
    public HomeDashboardAppService(HomeDashboardService svc) => _svc = svc;

    /// <summary>今日汇总。</summary>
    /// <returns>汇总</returns>
    [ApiDescriptionSettings(Name = "Get"), HttpPost]
    [DisplayName("首页汇总")]
    public Task<HomeDashboardDto> Get() =>
        _svc.GetAsync(App.HttpContext?.RequestAborted ?? default);
}

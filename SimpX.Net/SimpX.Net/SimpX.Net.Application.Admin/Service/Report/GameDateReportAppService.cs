using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台游戏日报与注册留存（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "gameDateReport", Order = 545, Description = "游戏日报")]
public partial class GameDateReportAppService : IDynamicApiController, ITransient
{
    private readonly GameDateReportService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">报表</param>
    public GameDateReportAppService(GameDateReportService svc) => _svc = svc;

    /// <summary>游戏日汇总。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("游戏日报列表")]
    public Task<List<GameDateReportItemDto>> List(GameDateReportListInput input) =>
        _svc.ListAsync(input ?? new GameDateReportListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>注册留存简化。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListRegisterRetain"), HttpPost]
    [DisplayName("注册留存列表")]
    public Task<List<RegisterRetainItemDto>> ListRegisterRetain(SiteFundDailyListInput input) =>
        _svc.ListRegisterRetainAsync(input ?? new SiteFundDailyListInput(), App.HttpContext?.RequestAborted ?? default);

    // --- merged from GameDateReportAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 agentstatistics。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Agentstatistics"), HttpPost]
    [DisplayName("agentstatistics")]
    public Task<object?> Agentstatistics(Dictionary<string, object?>? input = null) =>
        _svc.AgentstatisticsAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 fields。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Fields"), HttpPost]
    [DisplayName("fields")]
    public Task<object?> Fields(Dictionary<string, object?>? input = null) =>
        _svc.FieldsAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 info。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("info")]
    public Task<object?> Info(Dictionary<string, object?>? input = null) =>
        _svc.InfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 siteview。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Siteview"), HttpPost]
    [DisplayName("siteview")]
    public Task<object?> Siteview(Dictionary<string, object?>? input = null) =>
        _svc.SiteviewAsync(input, App.HttpContext?.RequestAborted ?? default);
}

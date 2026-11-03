using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台活动日志（JWT）：列表 + 审核。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "eventLog", Order = 520, Description = "活动日志")]
public partial class EventLogCheckAppService : IDynamicApiController, ITransient
{
    private readonly IEventLogCheckService _svc;
    private readonly EventLogService _query;
    private readonly ICurrentSiteAccessor _site;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>
    /// 注入审核、查询、定站、管理员会话。
    /// </summary>
    /// <param name="svc">活动审核</param>
    /// <param name="query">活动日志查询</param>
    /// <param name="site">定站</param>
    /// <param name="adminAuth">站点管理员</param>
    public EventLogCheckAppService(
        IEventLogCheckService svc,
        EventLogService query,
        ICurrentSiteAccessor site,
        SiteAdminAuthService adminAuth)
    {
        _svc = svc;
        _query = query;
        _site = site;
        _adminAuth = adminAuth;
    }

    /// <summary>
    /// 活动日志列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("活动日志列表")]
    public Task<List<EventLogListItemDto>> List(EventLogListInput input) =>
        _query.ListAsync(input ?? new EventLogListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 审核一笔活动日志（通过/拒绝）。
    /// </summary>
    /// <param name="input">审核入参</param>
    [ApiDescriptionSettings(Name = "Check"), HttpPost]
    [DisplayName("审核活动日志")]
    public async Task Check(EventLogCheckInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        var siteId = _site.RequireSiteId();
        await _svc.CheckAsync(
            input.ID,
            input.Status,
            input.CheckMessage,
            admin?.ID ?? 0,
            siteId,
            ct);
    }

    // --- merged from EventLogCheckAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 agree。
    /// </summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Agree"), HttpPost]
    [DisplayName("agree")]
    public async Task Agree(Dictionary<string, object?>? input = null)
    {
        input ??= new Dictionary<string, object?>();
        var id = 0;
        if (input.TryGetValue("ID", out var v) && v != null)
            int.TryParse(v.ToString(), out id);
        if (id <= 0 && input.TryGetValue("Id", out v) && v != null)
            int.TryParse(v.ToString(), out id);
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        var siteId = _site.RequireSiteId();
        await _svc.CheckAsync(id, 2, null, admin?.ID ?? 0, siteId, ct);
    }


    /// <summary>
    /// 对齐旧 info。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>结果</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("info")]
    public Task<object?> Info(Dictionary<string, object?>? input = null) =>
        _query.InfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 manualsigninrewart。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>结果</returns>
    [ApiDescriptionSettings(Name = "Manualsigninrewart"), HttpPost]
    [DisplayName("manualsigninrewart")]
    public Task<object?> Manualsigninrewart(Dictionary<string, object?>? input = null) =>
        _query.ManualsigninrewartAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 reject。
    /// </summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Reject"), HttpPost]
    [DisplayName("reject")]
    public async Task Reject(Dictionary<string, object?>? input = null)
    {
        input ??= new Dictionary<string, object?>();
        var id = 0;
        if (input.TryGetValue("ID", out var v) && v != null)
            int.TryParse(v.ToString(), out id);
        if (id <= 0 && input.TryGetValue("Id", out v) && v != null)
            int.TryParse(v.ToString(), out id);
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        var siteId = _site.RequireSiteId();
        await _svc.CheckAsync(id, 1, null, admin?.ID ?? 0, siteId, ct);
    }


    /// <summary>
    /// 对齐旧 save。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>结果</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("save")]
    public Task<object?> Save(Dictionary<string, object?>? input = null) =>
        _query.SaveAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 savebet。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>结果</returns>
    [ApiDescriptionSettings(Name = "Savebet"), HttpPost]
    [DisplayName("savebet")]
    public Task<object?> Savebet(Dictionary<string, object?>? input = null) =>
        _query.SavebetAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 savelotterycount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>结果</returns>
    [ApiDescriptionSettings(Name = "Savelotterycount"), HttpPost]
    [DisplayName("savelotterycount")]
    public Task<object?> Savelotterycount(Dictionary<string, object?>? input = null) =>
        _query.SavelotterycountAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 saveredenvelopecount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>结果</returns>
    [ApiDescriptionSettings(Name = "Saveredenvelopecount"), HttpPost]
    [DisplayName("saveredenvelopecount")]
    public Task<object?> Saveredenvelopecount(Dictionary<string, object?>? input = null) =>
        _query.SaveredenvelopecountAsync(input, App.HttpContext?.RequestAborted ?? default);
}

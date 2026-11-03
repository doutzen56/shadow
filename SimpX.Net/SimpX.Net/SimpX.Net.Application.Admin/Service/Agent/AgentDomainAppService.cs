namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台代理域名（JWT）。对齐旧版 /admin/user/agent/domain/list|info|save|delete。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "agentDomain", Order = 458, Description = "代理域名")]
public partial class AgentDomainAppService : IDynamicApiController, ITransient
{
    private readonly AgentDomainService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public AgentDomainAppService(AgentDomainService svc) => _svc = svc;

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("代理域名分页")]
    public Task<AgentDomainPageOutput> Page(AgentDomainPageInput input) =>
        _svc.PageAsync(input ?? new AgentDomainPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">域名编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("代理域名详情")]
    public Task<AgentDomainDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存代理域名")]
    public Task Save(AgentDomainSaveInput input) =>
        _svc.SaveAsync(input ?? new AgentDomainSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除代理域名")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);

    // --- merged from AgentDomainAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 background。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Background"), HttpPost]
    [DisplayName("background")]
    public Task<object?> Background(Dictionary<string, object?>? input = null) =>
        _svc.BackgroundAsync(input, App.HttpContext?.RequestAborted ?? default);
}

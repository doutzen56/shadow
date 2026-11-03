namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台返佣方案（JWT）。配置读写，不结算。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "agentCommission", Order = 482, Description = "返佣设定")]
public partial class AgentCommissionAppService : IDynamicApiController, ITransient
{
    private readonly AgentCommissionService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">返佣方案</param>
    public AgentCommissionAppService(AgentCommissionService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpGet]
    [DisplayName("返佣方案列表")]
    public Task<List<AgentCommissionListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("返佣方案详情")]
    public Task<AgentCommissionDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("返佣方案保存")]
    public Task<int> Save(AgentCommissionSaveInput input) =>
        _svc.SaveAsync(input ?? new AgentCommissionSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("返佣方案删除")]
    public Task Delete(AgentCommissionDeleteInput input) =>
        _svc.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    // --- merged from AgentCommissionAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 agentrate。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Agentrate"), HttpPost]
    [DisplayName("agentrate")]
    public Task<object?> Agentrate(Dictionary<string, object?>? input = null) =>
        _svc.AgentrateAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updateagentcount。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updateagentcount"), HttpPost]
    [DisplayName("updateagentcount")]
    public Task<object?> Updateagentcount(Dictionary<string, object?>? input = null) =>
        _svc.UpdateagentcountAsync(input, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>删除返佣方案入参。</summary>
public sealed class AgentCommissionDeleteInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }
}

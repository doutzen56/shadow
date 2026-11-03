namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台返点方案（JWT）。配置读写，不结算。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "agentReturn", Order = 483, Description = "返点设定")]
public partial class AgentReturnAppService : IDynamicApiController, ITransient
{
    private readonly AgentReturnService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">返点方案</param>
    public AgentReturnAppService(AgentReturnService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpGet]
    [DisplayName("返点方案列表")]
    public Task<List<AgentReturnListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("返点方案详情")]
    public Task<AgentReturnDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("返点方案保存")]
    public Task<int> Save(AgentReturnSaveInput input) =>
        _svc.SaveAsync(input ?? new AgentReturnSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("返点方案删除")]
    public Task Delete(AgentReturnDeleteInput input) =>
        _svc.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    // --- merged from AgentReturnAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 reexereturnmoney。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Reexereturnmoney"), HttpPost]
    [DisplayName("reexereturnmoney")]
    public Task<object?> Reexereturnmoney(Dictionary<string, object?>? input = null) =>
        _svc.ReexereturnmoneyAsync(input, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>删除返点方案入参。</summary>
public sealed class AgentReturnDeleteInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }
}

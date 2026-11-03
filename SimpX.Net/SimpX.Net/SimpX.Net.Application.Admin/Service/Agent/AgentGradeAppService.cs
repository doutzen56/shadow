namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台代理层级（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "agentGrade", Order = 487, Description = "层级设置")]
public class AgentGradeAppService : IDynamicApiController, ITransient
{
    private readonly AgentGradeService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">层级</param>
    public AgentGradeAppService(AgentGradeService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("代理层级列表")]
    public Task<List<AgentGradeListItemDto>> List(AgentGradeListInput input) =>
        _svc.ListAsync(input ?? new AgentGradeListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存（设代理/锁定）。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("代理层级保存")]
    public Task Save(AgentGradeSaveInput input) =>
        _svc.SaveAsync(input ?? new AgentGradeSaveInput(), App.HttpContext?.RequestAborted ?? default);
}

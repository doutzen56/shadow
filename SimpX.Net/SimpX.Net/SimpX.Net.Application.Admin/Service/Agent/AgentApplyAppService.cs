namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台代理审核（JWT）。对齐旧版 /admin/user/agent/info/applylist|execapply。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "agentApply", Order = 475, Description = "代理审核")]
public class AgentApplyAppService : IDynamicApiController, ITransient
{
    private readonly AgentApplyService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public AgentApplyAppService(AgentApplyService svc) => _svc = svc;

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("代理申请分页")]
    public Task<AgentApplyPageOutput> Page(AgentApplyPageInput input) =>
        _svc.PageAsync(input ?? new AgentApplyPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 审核（通过/拒绝）。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Check"), HttpPost]
    [DisplayName("代理申请审核")]
    public Task Check(AgentApplyCheckInput input) =>
        _svc.CheckAsync(input ?? new AgentApplyCheckInput(), App.HttpContext?.RequestAborted ?? default);
}

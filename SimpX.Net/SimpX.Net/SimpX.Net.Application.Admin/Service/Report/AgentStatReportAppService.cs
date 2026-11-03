using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台代理日统计（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "agentStatReport", Order = 531, Description = "代理统计")]
public class AgentStatReportAppService : IDynamicApiController, ITransient
{
    private readonly AgentStatReportService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">代理统计</param>
    public AgentStatReportAppService(AgentStatReportService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("代理统计列表")]
    public Task<List<AgentStatReportItemDto>> List(AgentStatReportListInput input) =>
        _svc.ListAsync(input ?? new AgentStatReportListInput(), App.HttpContext?.RequestAborted ?? default);
}

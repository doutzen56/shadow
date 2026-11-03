namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台打赏报表（JWT）。对齐旧版 /admin/report/bill/rewarddate。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "tipReport", Order = 471, Description = "打赏报表")]
public class TipReportAppService : IDynamicApiController, ITransient
{
    private readonly TipReportService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public TipReportAppService(TipReportService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行与合计</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("打赏报表")]
    public Task<TipReportListOutput> List(TipReportListInput input) =>
        _svc.ListAsync(input ?? new TipReportListInput(), App.HttpContext?.RequestAborted ?? default);
}

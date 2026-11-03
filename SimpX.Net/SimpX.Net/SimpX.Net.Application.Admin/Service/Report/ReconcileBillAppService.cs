namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台对账报表（JWT）。对齐旧版 /admin/report/bill/billdate。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "reconcileBill", Order = 470, Description = "对账报表")]
public class ReconcileBillAppService : IDynamicApiController, ITransient
{
    private readonly ReconcileBillService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public ReconcileBillAppService(ReconcileBillService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行与合计</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("对账报表")]
    public Task<ReconcileBillListOutput> List(ReconcileBillListInput input) =>
        _svc.ListAsync(input ?? new ReconcileBillListInput(), App.HttpContext?.RequestAborted ?? default);
}

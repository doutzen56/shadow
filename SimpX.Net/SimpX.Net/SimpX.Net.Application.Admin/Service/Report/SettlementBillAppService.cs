namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台交收表（JWT）。对齐旧版 /admin/report/bill/list。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "settlementBill", Order = 462, Description = "交收表")]
public class SettlementBillAppService : IDynamicApiController, ITransient
{
    private readonly SettlementBillService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public SettlementBillAppService(SettlementBillService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// 列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行与合计</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("交收表")]
    public Task<SettlementBillListOutput> List(SettlementBillListInput input) =>
        _svc.ListAsync(input, App.HttpContext?.RequestAborted ?? default);
}

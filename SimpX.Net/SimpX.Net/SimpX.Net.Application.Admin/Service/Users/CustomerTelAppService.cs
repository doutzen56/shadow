namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台电销列表（JWT）。联系方式脱敏，本刀不发短信。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "customerTel", Order = 493, Description = "电销系统")]
public class CustomerTelAppService : IDynamicApiController, ITransient
{
    private readonly CustomerTelService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">电销</param>
    public CustomerTelAppService(CustomerTelService svc) => _svc = svc;

    /// <summary>分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("电销会员分页")]
    public Task<CustomerTelPageOutput> Page(CustomerTelPageInput input) =>
        _svc.PageAsync(input ?? new CustomerTelPageInput(), App.HttpContext?.RequestAborted ?? default);
}

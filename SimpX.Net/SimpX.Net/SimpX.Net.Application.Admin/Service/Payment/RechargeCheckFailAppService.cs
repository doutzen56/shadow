namespace SimpX.Net.Application.Admin;

/// <summary>
/// 比对失败充值单只读（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "rechargeCheckFail", Order = 487, Description = "比对失败审核")]
public class RechargeCheckFailAppService : IDynamicApiController, ITransient
{
    private readonly RechargeCheckFailService _svc;

    /// <summary>注入。</summary>
    public RechargeCheckFailAppService(RechargeCheckFailService svc) => _svc = svc;

    /// <summary>分页。</summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("比对失败充值分页")]
    public Task<RechargeCheckFailPageOutput> Page(RechargeCheckFailPageInput input) =>
        _svc.PageAsync(input ?? new RechargeCheckFailPageInput(), App.HttpContext?.RequestAborted ?? default);
}

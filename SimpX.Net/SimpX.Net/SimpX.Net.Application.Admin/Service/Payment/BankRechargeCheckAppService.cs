namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台入款审核（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "bankRechargeCheck", Order = 484, Description = "入款审核")]
public class BankRechargeCheckAppService : IDynamicApiController, ITransient
{
    private readonly BankRechargeCheckService _svc;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>注入。</summary>
    public BankRechargeCheckAppService(BankRechargeCheckService svc, SiteAdminAuthService adminAuth)
    {
        _svc = svc;
        _adminAuth = adminAuth;
    }

    /// <summary>入款审核列表。</summary>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("入款审核列表")]
    public Task<List<BankRechargeCheckListItemDto>> List(BankRechargeCheckListInput input) =>
        _svc.ListAsync(input ?? new BankRechargeCheckListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>审核通过/拒绝。</summary>
    [ApiDescriptionSettings(Name = "Check"), HttpPost]
    [DisplayName("入款审核")]
    public async Task Check(BankRechargeCheckInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _svc.CheckAsync(input ?? new BankRechargeCheckInput(), admin?.ID ?? 0, ct);
    }
}

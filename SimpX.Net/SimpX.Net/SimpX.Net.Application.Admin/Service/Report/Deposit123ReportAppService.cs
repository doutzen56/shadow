using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台存款一二三（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "deposit123", Order = 532, Description = "存款一二三")]
public class Deposit123ReportAppService : IDynamicApiController, ITransient
{
    private readonly Deposit123ReportService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">存款一二三</param>
    public Deposit123ReportAppService(Deposit123ReportService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("存款一二三列表")]
    public Task<List<Deposit123ItemDto>> List(Deposit123ListInput input) =>
        _svc.ListAsync(input ?? new Deposit123ListInput(), App.HttpContext?.RequestAborted ?? default);
}

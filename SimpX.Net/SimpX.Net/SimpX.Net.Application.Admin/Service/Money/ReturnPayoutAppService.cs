namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台返点发放（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "returnPayout", Order = 489, Description = "返点发放")]
public class ReturnPayoutAppService : IDynamicApiController, ITransient
{
    private readonly ReturnPayoutService _svc;

    /// <summary>注入。</summary>
    public ReturnPayoutAppService(ReturnPayoutService svc) => _svc = svc;

    /// <summary>返点日志列表。</summary>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("返点日志列表")]
    public Task<List<ReturnLogListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>按日期区间生成待发返点（不写账变）。</summary>
    [ApiDescriptionSettings(Name = "Create"), HttpPost]
    [DisplayName("生成返点")]
    public Task<ReturnCreateResultDto> Create(ReturnCreateInput input) =>
        _svc.CreateAsync(
            input?.StartDate ?? default,
            input?.EndDate ?? default,
            now: null,
            App.HttpContext?.RequestAborted ?? default);

    /// <summary>发放一笔。</summary>
    [ApiDescriptionSettings(Name = "Pay"), HttpPost]
    [DisplayName("返点发放")]
    public Task<bool> Pay(ReturnPayInput input) =>
        _svc.PayAsync(input?.LogID ?? 0, App.HttpContext?.RequestAborted ?? default);
}

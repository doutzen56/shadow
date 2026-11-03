namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台佣金发放（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "commissionPayout", Order = 488, Description = "佣金发放")]
public class CommissionPayoutAppService : IDynamicApiController, ITransient
{
    private readonly CommissionPayoutService _svc;

    /// <summary>注入。</summary>
    public CommissionPayoutAppService(CommissionPayoutService svc) => _svc = svc;

    /// <summary>佣金日志列表。</summary>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("佣金日志列表")]
    public Task<List<CommissionLogListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 佣金日志列表（旧方法名别名）。对齐旧 loglist。
    /// </summary>
    [ApiDescriptionSettings(Name = "LogList"), HttpPost]
    [DisplayName("佣金日志列表LogList")]
    public Task<List<CommissionLogListItemDto>> LogList() => List();

    /// <summary>按日期区间生成待发佣金（不写账变）。</summary>
    [ApiDescriptionSettings(Name = "Create"), HttpPost]
    [DisplayName("生成佣金")]
    public Task<CommissionCreateResultDto> Create(CommissionCreateInput input) =>
        _svc.CreateAsync(
            input?.StartDate ?? default,
            input?.EndDate ?? default,
            now: null,
            App.HttpContext?.RequestAborted ?? default);

    /// <summary>发放一笔。</summary>
    [ApiDescriptionSettings(Name = "Pay"), HttpPost]
    [DisplayName("佣金发放")]
    public Task<bool> Pay(CommissionPayInput input) =>
        _svc.PayAsync(input?.LogID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 通过佣金审核。对齐旧 logagree。
    /// </summary>
    /// <param name="input">日志号</param>
    /// <returns>是否成功</returns>
    [ApiDescriptionSettings(Name = "LogAgree"), HttpPost]
    [DisplayName("通过佣金审核")]
    public Task<bool> LogAgree(CommissionLogIdInput input) =>
        _svc.PayAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 拒绝佣金审核。对齐旧 logreject。
    /// </summary>
    /// <param name="input">日志号</param>
    [ApiDescriptionSettings(Name = "LogReject"), HttpPost]
    [DisplayName("拒绝佣金审核")]
    public Task LogReject(CommissionLogIdInput input) =>
        _svc.RejectAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 佣金详情。对齐旧 logdetail。
    /// </summary>
    /// <param name="input">日志号</param>
    /// <returns>详情 JSON</returns>
    [ApiDescriptionSettings(Name = "LogDetail"), HttpPost]
    [DisplayName("佣金详情")]
    public Task<CommissionLogDetailDto> LogDetail(CommissionLogIdInput input) =>
        _svc.DetailAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);
}

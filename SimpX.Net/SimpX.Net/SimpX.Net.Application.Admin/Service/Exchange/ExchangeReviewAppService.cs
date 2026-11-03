namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台兑币审核与汇兑总账（JWT）。对齐旧版 /admin/exchange/review/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "exchangeReview", Order = 462, Description = "兑币审核")]
public class ExchangeReviewAppService : IDynamicApiController, ITransient
{
    private readonly ExchangeReviewService _svc;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>
    /// 注入服务与站点后台会话。
    /// </summary>
    /// <param name="svc">服务</param>
    /// <param name="adminAuth">站点后台会话（审核人）</param>
    public ExchangeReviewAppService(ExchangeReviewService svc, SiteAdminAuthService adminAuth)
    {
        _svc = svc;
        _adminAuth = adminAuth;
    }

    /// <summary>
    /// 待审核列表。对齐旧 list。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("待审核兑币列表")]
    public Task<List<ExchangeReviewListItemDto>> List(ExchangeReviewListInput input) =>
        _svc.ListPendingAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 已审核列表。对齐旧 log。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "Log"), HttpPost]
    [DisplayName("已审核兑币列表")]
    public Task<List<ExchangeReviewListItemDto>> Log(ExchangeReviewLogInput input) =>
        _svc.ListLogAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 汇兑总账。对齐旧 summary。
    /// </summary>
    /// <param name="input">日期</param>
    /// <returns>按日与合计</returns>
    [ApiDescriptionSettings(Name = "Summary"), HttpPost]
    [DisplayName("汇兑总账")]
    public Task<ExchangeSummaryOutput> Summary(ExchangeSummaryInput input) =>
        _svc.SummaryAsync(input ?? new ExchangeSummaryInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 审核兑币。对齐旧 check（解锁 + 成功时账变）。
    /// </summary>
    /// <param name="input">审核入参</param>
    [ApiDescriptionSettings(Name = "Check"), HttpPost]
    [DisplayName("审核兑币")]
    public async Task Check(ExchangeReviewCheckInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _svc.CheckAsync(input, admin?.ID ?? 0, ct);
    }
}

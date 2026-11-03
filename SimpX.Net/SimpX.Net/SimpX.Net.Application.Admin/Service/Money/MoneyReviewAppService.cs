namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台加减款审核（JWT）。对齐旧 /admin/money/moneyreview/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "moneyReview", Order = 481, Description = "加减款审核")]
public class MoneyReviewAppService : IDynamicApiController, ITransient
{
    private readonly MoneyReviewService _svc;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>
    /// 注入服务与站点后台会话。
    /// </summary>
    /// <param name="svc">服务</param>
    /// <param name="adminAuth">站点管理员</param>
    public MoneyReviewAppService(MoneyReviewService svc, SiteAdminAuthService adminAuth)
    {
        _svc = svc;
        _adminAuth = adminAuth;
    }

    /// <summary>
    /// 审核单列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("加减款审核列表")]
    public Task<List<MoneyReviewListItemDto>> List(MoneyReviewListInput input) =>
        _svc.ListAsync(input ?? new MoneyReviewListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 审核一笔（通过/拒绝）。
    /// </summary>
    /// <param name="input">审核入参</param>
    [ApiDescriptionSettings(Name = "Check"), HttpPost]
    [DisplayName("审核加减款")]
    public async Task Check(MoneyReviewCheckInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _svc.CheckAsync(input ?? new MoneyReviewCheckInput(), admin?.ID ?? 0, ct);
    }

    /// <summary>
    /// 批量通过。对齐旧 batchsuccess。
    /// </summary>
    /// <param name="input">Ids</param>
    [ApiDescriptionSettings(Name = "BatchSuccess"), HttpPost]
    [DisplayName("批量通过加减款")]
    public async Task BatchSuccess(MoneyReviewBatchInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _svc.BatchCheckAsync(
            input?.Ids,
            MoneyReviewService.StatusSuccess,
            admin?.ID ?? 0,
            ct);
    }

    /// <summary>
    /// 批量拒绝。对齐旧 batchfail。
    /// </summary>
    /// <param name="input">Ids</param>
    [ApiDescriptionSettings(Name = "BatchFail"), HttpPost]
    [DisplayName("批量拒绝加减款")]
    public async Task BatchFail(MoneyReviewBatchInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _svc.BatchCheckAsync(
            input?.Ids,
            MoneyReviewService.StatusFailed,
            admin?.ID ?? 0,
            ct);
    }
}

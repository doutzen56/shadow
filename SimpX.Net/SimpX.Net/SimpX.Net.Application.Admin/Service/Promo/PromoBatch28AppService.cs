using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 轮盘/红包日志（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "promoChanceLog", Order = 551, Description = "轮盘红包日志")]
public class PromoChanceLogAppService : IDynamicApiController, ITransient
{
    private readonly PromoChanceLogService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">日志</param>
    public PromoChanceLogAppService(PromoChanceLogService svc) => _svc = svc;

    /// <summary>轮盘分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageDelottery"), HttpPost]
    [DisplayName("轮盘次数日志")]
    public Task<ChanceLogPageOutput> PageDelottery(ChanceLogPageInput input) =>
        _svc.PageDelotteryAsync(input ?? new ChanceLogPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>红包分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageRedEnvelope"), HttpPost]
    [DisplayName("红包次数日志")]
    public Task<ChanceLogPageOutput> PageRedEnvelope(ChanceLogPageInput input) =>
        _svc.PageRedEnvelopeAsync(input ?? new ChanceLogPageInput(), App.HttpContext?.RequestAborted ?? default);
}

/// <summary>
/// 调查问卷（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "surveyAdmin", Order = 552, Description = "调查问卷")]
public partial class SurveyAdminAppService : IDynamicApiController, ITransient
{
    private readonly SurveyAdminService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">问卷</param>
    public SurveyAdminAppService(SurveyAdminService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("问卷列表")]
    public Task<List<SurveyItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("问卷保存")]
    public Task<int> Save(SurveySaveInput input) =>
        _svc.SaveAsync(input ?? new SurveySaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>答卷分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageLogs"), HttpPost]
    [DisplayName("问卷记录")]
    public Task<SurveyLogPageOutput> PageLogs(SurveyLogPageInput input) =>
        _svc.PageLogsAsync(input ?? new SurveyLogPageInput(), App.HttpContext?.RequestAborted ?? default);
}

/// <summary>
/// 彩金兑换（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "voucherAdmin", Order = 553, Description = "彩金兑换")]
public partial class VoucherAdminAppService : IDynamicApiController, ITransient
{
    private readonly VoucherAdminService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">彩金</param>
    public VoucherAdminAppService(VoucherAdminService svc) => _svc = svc;

    /// <summary>产品列表。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListProducts"), HttpPost]
    [DisplayName("彩金产品列表")]
    public Task<List<VoucherProductItemDto>> ListProducts() =>
        _svc.ListProductsAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存产品。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "SaveProduct"), HttpPost]
    [DisplayName("彩金产品保存")]
    public Task<int> SaveProduct(VoucherProductSaveInput input) =>
        _svc.SaveProductAsync(input ?? new VoucherProductSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>订单分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageOrders"), HttpPost]
    [DisplayName("彩金订单分页")]
    public Task<VoucherOrderPageOutput> PageOrders(VoucherOrderPageInput input) =>
        _svc.PageOrdersAsync(input ?? new VoucherOrderPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>改订单状态。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "UpdateOrderStatus"), HttpPost]
    [DisplayName("彩金订单改状态")]
    public Task UpdateOrderStatus(VoucherOrderStatusInput input) =>
        _svc.UpdateOrderStatusAsync(input ?? new VoucherOrderStatusInput(), App.HttpContext?.RequestAborted ?? default);
}

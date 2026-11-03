namespace SimpX.Net.Application.Admin;

/// <summary>
/// 返水发放入口（JWT）：计划列表、生成计划、发放。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "rebatePayout", Order = 485, Description = "返水发放")]
public partial class RebatePayoutAppService : IDynamicApiController, ITransient
{
    private readonly RebatePayoutService _svc;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>注入。</summary>
    public RebatePayoutAppService(RebatePayoutService svc, SiteAdminAuthService adminAuth)
    {
        _svc = svc;
        _adminAuth = adminAuth;
    }

    /// <summary>返水计划列表。</summary>
    [ApiDescriptionSettings(Name = "PlanList"), HttpPost]
    [DisplayName("返水计划列表")]
    public Task<List<RebatePlanListItemDto>> PlanList() =>
        _svc.ListPlansAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>从待发日志生成计划。</summary>
    [ApiDescriptionSettings(Name = "Generate"), HttpPost]
    [DisplayName("生成返水计划")]
    public async Task<int> Generate(RebateGeneratePlanInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        return await _svc.GeneratePlanAsync(input ?? new RebateGeneratePlanInput(), admin?.ID ?? 0, ct);
    }

    /// <summary>
    /// 建计划并发放。对齐旧 rebate/execute。
    /// </summary>
    /// <param name="input">时间段与标题</param>
    /// <returns>计划号与成功笔数</returns>
    [ApiDescriptionSettings(Name = "Execute"), HttpPost]
    [DisplayName("执行返水发放")]
    public async Task<RebateExecuteResultDto> Execute(RebateExecuteInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        return await _svc.ExecuteAsync(input ?? new RebateExecuteInput(), admin?.ID ?? 0, ct);
    }

    /// <summary>发放计划待审日志。</summary>
    [ApiDescriptionSettings(Name = "Pay"), HttpPost]
    [DisplayName("返水发放")]
    public Task<int> Pay(RebatePayInput input) =>
        _svc.PayPlanAsync(input?.PlanID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 按日投注和返水设定重算未发放日志，不入账。
    /// </summary>
    /// <param name="input">时间段与筛选</param>
    /// <returns>写入或更新的笔数</returns>
    [ApiDescriptionSettings(Name = "Recalculate"), HttpPost]
    [DisplayName("重算返水日志")]
    public Task<int> Recalculate(RebateGeneratePlanInput input) =>
        _svc.RecalculateAsync(input ?? new RebateGeneratePlanInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 拒绝一笔待发返水。对齐旧 rebate/reject。
    /// </summary>
    /// <param name="input">计划与日志号</param>
    [ApiDescriptionSettings(Name = "Reject"), HttpPost]
    [DisplayName("拒绝返水")]
    public Task Reject(RebateLogActionInput input) =>
        _svc.RejectAsync(input?.PlanID ?? 0, input?.LogID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 重发待审返水。对齐旧 rebate/reextcute。
    /// </summary>
    /// <param name="input">计划与日志号</param>
    /// <returns>成功笔数</returns>
    [ApiDescriptionSettings(Name = "ReExecute"), HttpPost]
    [DisplayName("重发返水")]
    public Task<int> ReExecute(RebateLogActionInput input) =>
        _svc.ReExecuteAsync(input?.PlanID ?? 0, input?.LogID ?? 0, App.HttpContext?.RequestAborted ?? default);

    // --- merged from RebatePayoutAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 BlockWhenAutoReturnV2Enabled。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "BlockWhenAutoReturnV2Enabled"), HttpPost]
    [DisplayName("BlockWhenAutoReturnV2Enabled")]
    public Task<object?> BlockWhenAutoReturnV2Enabled(Dictionary<string, object?>? input = null) =>
        _svc.BlockWhenAutoReturnV2EnabledAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 exportlogdetail。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Exportlogdetail"), HttpPost]
    [DisplayName("exportlogdetail")]
    public Task<object?> Exportlogdetail(Dictionary<string, object?>? input = null) =>
        _svc.ExportlogdetailAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 grouplist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Grouplist"), HttpPost]
    [DisplayName("grouplist")]
    public Task<object?> Grouplist(Dictionary<string, object?>? input = null) =>
        _svc.GrouplistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 logdetail。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Logdetail"), HttpPost]
    [DisplayName("logdetail")]
    public Task<object?> Logdetail(Dictionary<string, object?>? input = null) =>
        _svc.LogdetailAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 loglist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Loglist"), HttpPost]
    [DisplayName("loglist")]
    public Task<object?> Loglist(Dictionary<string, object?>? input = null) =>
        _svc.LoglistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updateenabled。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updateenabled"), HttpPost]
    [DisplayName("updateenabled")]
    public Task<object?> Updateenabled(Dictionary<string, object?>? input = null) =>
        _svc.UpdateenabledAsync(input, App.HttpContext?.RequestAborted ?? default);
}

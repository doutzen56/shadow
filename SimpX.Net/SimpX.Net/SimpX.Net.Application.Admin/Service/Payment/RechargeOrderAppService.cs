namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台充值订单（JWT）。对齐旧 orderlist / checkorder / deleteorder。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "rechargeOrder", Order = 482, Description = "充值订单")]
public partial class RechargeOrderAppService : IDynamicApiController, ITransient
{
    private readonly RechargeOrderService _query;
    private readonly RechargeOrderCheckService _check;
    private readonly RechargeOrderDeleteService _delete;
    private readonly RechargeOrderCreateService _create;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>
    /// 注入查询、建单、到账、删除、后台身份。
    /// </summary>
    /// <param name="query">分页查询</param>
    /// <param name="check">确认到账</param>
    /// <param name="delete">拒绝删除</param>
    /// <param name="create">建单并提交网关</param>
    /// <param name="adminAuth">当前管理员</param>
    public RechargeOrderAppService(
        RechargeOrderService query,
        RechargeOrderCheckService check,
        RechargeOrderDeleteService delete,
        RechargeOrderCreateService create,
        SiteAdminAuthService adminAuth)
    {
        _query = query;
        _check = check;
        _delete = delete;
        _create = create;
        _adminAuth = adminAuth;
    }

    /// <summary>
    /// 充值订单分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("充值订单分页")]
    public Task<RechargeOrderPageOutput> Page(RechargeOrderPageInput input) =>
        _query.PageAsync(input ?? new RechargeOrderPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 代客建单并提交网关。对齐旧 CreateRechargeOrder 主路径。
    /// </summary>
    /// <param name="input">会员、通道、金额等</param>
    /// <returns>订单号与支付信息</returns>
    [ApiDescriptionSettings(Name = "Create"), HttpPost]
    [DisplayName("代客充值建单")]
    public async Task<RechargeOrderCreateOutput> Create(RechargeOrderCreateInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        _ = await _adminAuth.TryGetCurrentAdminAsync(ct);
        input ??= new RechargeOrderCreateInput();
        input.IsProxyRecharge = true;
        if (string.IsNullOrWhiteSpace(input.ClientIP))
            input.ClientIP = "0.0.0.0";
        return await _create.CreateAsync(input, ct);
    }

    /// <summary>
    /// 确认到账。对齐旧 recharge/checkorder。
    /// </summary>
    /// <param name="input">订单号、网关单号、实付、手续费、成本</param>
    [ApiDescriptionSettings(Name = "Check"), HttpPost]
    [DisplayName("充值确认到账")]
    public async Task Check(RechargeOrderCheckInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _check.CheckAsync(input ?? new RechargeOrderCheckInput(), admin?.ID ?? 0, expectedPayId: null, ct);
    }

    /// <summary>
    /// 拒绝删除未到账单。对齐旧 recharge/deleteorder。
    /// </summary>
    /// <param name="input">订单号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("拒绝充值订单")]
    public async Task Delete(RechargeOrderDeleteInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _delete.DeleteAsync(input?.ID, admin?.ID ?? 0, ct);
    }

    // --- merged from RechargeOrderAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 exportrechargeorder。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Exportrechargeorder"), HttpPost]
    [DisplayName("exportrechargeorder")]
    public Task<object?> Exportrechargeorder(Dictionary<string, object?>? input = null) =>
        _query.ExportrechargeorderAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 orderinfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Orderinfo"), HttpPost]
    [DisplayName("orderinfo")]
    public Task<object?> Orderinfo(Dictionary<string, object?>? input = null) =>
        _query.OrderinfoAsync(input, App.HttpContext?.RequestAborted ?? default);
}

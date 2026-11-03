namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台提现订单（JWT）：列表 + 审核 + 人工出款。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "withdrawOrder", Order = 483, Description = "提现订单")]
public partial class WithdrawOrderAppService : IDynamicApiController, ITransient
{
    private readonly WithdrawOrderService _query;
    private readonly WithdrawOrderOperateService _operate;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>注入。</summary>
    public WithdrawOrderAppService(
        WithdrawOrderService query,
        WithdrawOrderOperateService operate,
        SiteAdminAuthService adminAuth)
    {
        _query = query;
        _operate = operate;
        _adminAuth = adminAuth;
    }

    /// <summary>提现订单分页。</summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("提现订单分页")]
    public Task<WithdrawOrderPageOutput> Page(WithdrawOrderPageInput input) =>
        _query.PageAsync(input ?? new WithdrawOrderPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 提现订单分页（旧方法名别名）。对齐旧 orderlist。
    /// </summary>
    [ApiDescriptionSettings(Name = "OrderList"), HttpPost]
    [DisplayName("提现订单列表OrderList")]
    public Task<WithdrawOrderPageOutput> OrderList(WithdrawOrderPageInput input) => Page(input);

    /// <summary>
    /// 提现订单详情。对齐旧 orderinfo。
    /// </summary>
    /// <param name="id">订单号</param>
    [ApiDescriptionSettings(Name = "OrderInfo"), HttpPost]
    [DisplayName("提现订单详情")]
    public Task<WithdrawOrderInfoDto> OrderInfo([FromQuery] int id) =>
        _query.InfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 提现订单操作日志。对齐旧 orderlog。
    /// </summary>
    /// <param name="orderID">订单号</param>
    [ApiDescriptionSettings(Name = "OrderLog"), HttpPost]
    [DisplayName("提现订单日志")]
    public Task<List<WithdrawOrderLogItemDto>> OrderLog([FromQuery] int orderID) =>
        _query.ListLogsAsync(orderID, App.HttpContext?.RequestAborted ?? default);

    /// <summary>审核订单。</summary>
    [ApiDescriptionSettings(Name = "Check"), HttpPost]
    [DisplayName("提现订单审核")]
    public async Task Check(WithdrawOrderCheckInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _operate.CheckAsync(input ?? new WithdrawOrderCheckInput(), admin?.ID ?? 0, ct);
    }

    /// <summary>
    /// 审核订单（旧方法名别名）。对齐旧 checkorder。
    /// </summary>
    [ApiDescriptionSettings(Name = "CheckOrder"), HttpPost]
    [DisplayName("提现订单审核CheckOrder")]
    public Task CheckOrder(WithdrawOrderCheckInput input) => Check(input);

    /// <summary>处理订单（人工或代付提交）。</summary>
    [ApiDescriptionSettings(Name = "Exec"), HttpPost]
    [DisplayName("提现订单处理")]
    public async Task Exec(WithdrawOrderExecInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _operate.ExecAsync(input ?? new WithdrawOrderExecInput(), admin?.ID ?? 0, ct);
    }

    /// <summary>
    /// 处理订单（旧方法名别名）。对齐旧 execorder。
    /// </summary>
    [ApiDescriptionSettings(Name = "ExecOrder"), HttpPost]
    [DisplayName("提现订单处理ExecOrder")]
    public Task ExecOrder(WithdrawOrderExecInput input) => Exec(input);

    /// <summary>代付确认到账（Stub/回调入口）。</summary>
    [ApiDescriptionSettings(Name = "ConfirmGateway"), HttpPost]
    [DisplayName("代付确认到账")]
    public async Task ConfirmGateway(WithdrawGatewayOrderInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _operate.ConfirmGatewayAsync(input?.OrderID ?? 0, admin?.ID ?? 0, ct);
    }

    /// <summary>代付失败回写（Stub/回调入口）。</summary>
    [ApiDescriptionSettings(Name = "FailGateway"), HttpPost]
    [DisplayName("代付失败")]
    public async Task FailGateway(WithdrawGatewayFailInput input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _operate.FailGatewayAsync(input?.OrderID ?? 0, admin?.ID ?? 0, input?.Reason ?? string.Empty, ct);
    }

    /// <summary>代付查单。</summary>
    [ApiDescriptionSettings(Name = "QueryGateway"), HttpPost]
    [DisplayName("代付查单")]
    public Task<WithdrawGatewayResult> QueryGateway(WithdrawGatewayOrderInput input) =>
        _operate.QueryGatewayAsync(input?.OrderID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 代付查单（旧方法名别名）。对齐旧 queryorder。
    /// </summary>
    [ApiDescriptionSettings(Name = "QueryOrder"), HttpPost]
    [DisplayName("代付查单QueryOrder")]
    public Task<WithdrawGatewayResult> QueryOrder(WithdrawGatewayOrderInput input) => QueryGateway(input);

    /// <summary>代付回调（验签后完结；本波需登录，公开匿名回调下波再开）。</summary>
    [ApiDescriptionSettings(Name = "GatewayCallback"), HttpPost]
    [DisplayName("代付回调")]
    public async Task GatewayCallback(WithdrawGatewayCallbackRaw input)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        await _operate.HandleGatewayCallbackAsync(input ?? new WithdrawGatewayCallbackRaw(), admin?.ID ?? 0, ct);
    }

    // --- merged from WithdrawOrderAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 channelinfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Channelinfo"), HttpPost]
    [DisplayName("channelinfo")]
    public Task<object?> Channelinfo(Dictionary<string, object?>? input = null) =>
        _query.ChannelinfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 channelist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Channelist"), HttpPost]
    [DisplayName("channelist")]
    public Task<object?> Channelist(Dictionary<string, object?>? input = null) =>
        _query.ChannelistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 deletechannel。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Deletechannel"), HttpPost]
    [DisplayName("deletechannel")]
    public Task<object?> Deletechannel(Dictionary<string, object?>? input = null) =>
        _query.DeletechannelAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 exportwithdraworder。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Exportwithdraworder"), HttpPost]
    [DisplayName("exportwithdraworder")]
    public Task<object?> Exportwithdraworder(Dictionary<string, object?>? input = null) =>
        _query.ExportwithdraworderAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 openchannefirst。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Openchannefirst"), HttpPost]
    [DisplayName("openchannefirst")]
    public Task<object?> Openchannefirst(Dictionary<string, object?>? input = null) =>
        _query.OpenchannefirstAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 openchannelist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Openchannelist"), HttpPost]
    [DisplayName("openchannelist")]
    public Task<object?> Openchannelist(Dictionary<string, object?>? input = null) =>
        _query.OpenchannelistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 openchannessystem。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Openchannessystem"), HttpPost]
    [DisplayName("openchannessystem")]
    public Task<object?> Openchannessystem(Dictionary<string, object?>? input = null) =>
        _query.OpenchannessystemAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 savechannel。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Savechannel"), HttpPost]
    [DisplayName("savechannel")]
    public Task<object?> Savechannel(Dictionary<string, object?>? input = null) =>
        _query.SavechannelAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 washflowinfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Washflowinfo"), HttpPost]
    [DisplayName("washflowinfo")]
    public Task<object?> Washflowinfo(Dictionary<string, object?>? input = null) =>
        _query.WashflowinfoAsync(input, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>代付订单号入参。</summary>
public sealed class WithdrawGatewayOrderInput
{
    /// <summary>订单号。</summary>
    public int OrderID { get; set; }
}

/// <summary>代付失败入参。</summary>
public sealed class WithdrawGatewayFailInput
{
    /// <summary>订单号。</summary>
    public int OrderID { get; set; }

    /// <summary>原因。</summary>
    public string Reason { get; set; }
}

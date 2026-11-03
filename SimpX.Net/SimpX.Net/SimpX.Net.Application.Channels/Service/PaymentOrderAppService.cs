namespace SimpX.Net.Application.Channels;

/// <summary>
/// 支付单 HTTP 入口。领域逻辑在 Shared PaymentOrderService；本宿主按域名确定租户。
/// </summary>
[ApiDescriptionSettings(Module = "pay", Name = "order", Order = 100)]
public class PaymentOrderAppService : IDynamicApiController, ITransient
{
    private readonly PaymentOrderService _paymentOrderService;

    /// <summary>
    /// 注入共享支付领域服务。
    /// </summary>
    /// <param name="paymentOrderService">支付领域服务</param>
    public PaymentOrderAppService(PaymentOrderService paymentOrderService)
    {
        _paymentOrderService = paymentOrderService;
    }

    /// <summary>
    /// 创建支付单并返回调起参数。只接受业务系统的内部签名调用；请求 Host 要能按域名确定到租户。
    /// </summary>
    /// <param name="input">创建支付请求</param>
    /// <returns>支付单号与渠道调起参数</returns>
    [Authorize(AuthenticationSchemes = InternalCallDefaults.AuthenticationScheme)]
    [HttpPost("create")]
    [DisplayName("创建支付单")]
    public Task<PaymentCreateResult> Create([FromBody] PaymentCreateRequest input)
    {
        return _paymentOrderService.Create(input);
    }

    /// <summary>
    /// 按支付单号查询。
    /// </summary>
    /// <param name="paymentOrderNo">支付单号</param>
    /// <returns>支付单</returns>
    [Authorize(AuthenticationSchemes = InternalCallDefaults.AuthenticationScheme)]
    [HttpGet("get")]
    [DisplayName("查询支付单")]
    public Task<PaymentOrder> Get([FromQuery] string paymentOrderNo)
    {
        return _paymentOrderService.Get(paymentOrderNo);
    }

    /// <summary>
    /// 渠道回调入口。Demo：paymentOrderNo=xxx&amp;success=true
    /// </summary>
    /// <param name="channelCode">渠道编码</param>
    /// <returns>渠道约定的成功应答</returns>
    [AllowAnonymous]
    [HttpPost("notify/{channelCode}")]
    [DisplayName("支付渠道回调")]
    public async Task<string> Notify(string channelCode)
    {
        using var reader = new StreamReader(App.HttpContext.Request.Body);
        var rawBody = await reader.ReadToEndAsync();
        return await _paymentOrderService.Notify(channelCode, rawBody);
    }
}

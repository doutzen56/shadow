using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Channels;

/// <summary>
/// 模拟第三方支付网关。只给 <c>mock-http</c> 通道当靶子，演示通道如何用 IHttpCaller 出站。
/// 不是真实支付，不要当生产网关用。
/// </summary>
[ApiDescriptionSettings(Module = "pay", Name = "mock-gateway", Order = 110)]
public class MockPaymentGatewayAppService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 模拟厂商「统一下单」。不用登录也能调，仿真实外部网关。
    /// </summary>
    /// <param name="input">支付中心单号、金额等</param>
    /// <returns>渠道单号与假收银台地址</returns>
    [AllowAnonymous]
    [HttpPost("create")]
    [DisplayName("模拟网关下单")]
    public MockGatewayCreateOutput Create([FromBody] MockGatewayCreateInput input)
    {
        if (string.IsNullOrWhiteSpace(input?.PaymentOrderNo))
            throw Oops.Oh("paymentOrderNo 不能为空");
        if (input.Amount <= 0)
            throw Oops.Oh("金额必须大于 0");

        var tradeNo = $"MG{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        return new MockGatewayCreateOutput
        {
            ChannelTradeNo = tradeNo,
            CashierUrl = $"/mock-pay/checkout?tradeNo={tradeNo}&orderNo={Uri.EscapeDataString(input.PaymentOrderNo)}",
            Message = "ok"
        };
    }
}

/// <summary>模拟网关下单入参。</summary>
public class MockGatewayCreateInput
{
    /// <summary>支付中心单号。</summary>
    [Required]
    [JsonPropertyName("paymentOrderNo")]
    public string PaymentOrderNo { get; set; }

    /// <summary>金额（元）。</summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>标题。</summary>
    [JsonPropertyName("subject")]
    public string Subject { get; set; }

    /// <summary>异步通知地址（示例里仅回显，不真实回调）。</summary>
    [JsonPropertyName("notifyUrl")]
    public string NotifyUrl { get; set; }
}

/// <summary>模拟网关下单出参。</summary>
public class MockGatewayCreateOutput
{
    /// <summary>渠道侧交易号。</summary>
    [JsonPropertyName("channelTradeNo")]
    public string ChannelTradeNo { get; set; }

    /// <summary>假收银台地址。</summary>
    [JsonPropertyName("cashierUrl")]
    public string CashierUrl { get; set; }

    /// <summary>说明。</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }
}

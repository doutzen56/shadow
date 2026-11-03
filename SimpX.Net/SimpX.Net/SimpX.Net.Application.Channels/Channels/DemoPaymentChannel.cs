namespace SimpX.Net.Application.Channels;

/// <summary>
/// Demo 支付渠道。不发 HTTP，只在内存里拼收银台参数；要看 IHttpCaller 出站示例用 mock-http。
/// </summary>
public class DemoPaymentChannel : IPaymentChannel, ITransient
{
    /// <inheritdoc />
    public string ChannelCode => "demo";

    /// <inheritdoc />
    public Task<Dictionary<string, string>> CreateAsync(PaymentOrder order, CancellationToken cancellationToken = default)
    {
        var payParams = new Dictionary<string, string>
        {
            ["cashierUrl"] = $"/demo-pay/checkout?orderNo={order.PaymentOrderNo}",
            ["mock"] = "true",
            ["amount"] = order.Amount.ToString("0.00")
        };
        return Task.FromResult(payParams);
    }

    /// <inheritdoc />
    public Task<PaymentNotifyContext> ParseNotifyAsync(string rawBody, CancellationToken cancellationToken = default)
    {
        var map = rawBody.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]), StringComparer.OrdinalIgnoreCase);

        if (!map.TryGetValue("paymentOrderNo", out var orderNo) || string.IsNullOrWhiteSpace(orderNo))
            throw Oops.Oh("Demo 回调缺少 paymentOrderNo");

        var success = !map.TryGetValue("success", out var successText) ||
                      successText.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                      successText == "1";

        return Task.FromResult(new PaymentNotifyContext
        {
            ChannelCode = ChannelCode,
            PaymentOrderNo = orderNo,
            ChannelTradeNo = $"DEMO-{orderNo}",
            Status = success ? PaymentStatus.Paid : PaymentStatus.Failed,
            RawBody = rawBody
        });
    }
}

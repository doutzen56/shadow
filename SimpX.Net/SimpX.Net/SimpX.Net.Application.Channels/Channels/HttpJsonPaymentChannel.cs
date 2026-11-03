using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace SimpX.Net.Application.Channels;

/// <summary>
/// 可配置 JSON HTTP 支付通道骨架。渠道编码 <c>http-json</c>。
/// 在 Payment:HttpJson 配好 CreateUrl / 回调字段后即可联调真实或沙箱 HTTP 接口；未配置时明确报错，不当静默 Demo。
/// 厂商专用签名算法在子类或后续专用通道里扩展，本类只示范「配置 + IHttpCaller」。
/// </summary>
public sealed class HttpJsonPaymentChannel : IPaymentChannel, ITransient
{
    private readonly IHttpCaller _httpCaller;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 注入 HTTP 调用与配置。
    /// </summary>
    public HttpJsonPaymentChannel(IHttpCaller httpCaller, IConfiguration configuration)
    {
        _httpCaller = httpCaller;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public string ChannelCode => "http-json";

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> CreateAsync(PaymentOrder order, CancellationToken cancellationToken = default)
    {
        var createUrl = _configuration["Payment:HttpJson:CreateUrl"];
        if (string.IsNullOrWhiteSpace(createUrl))
            throw Oops.Oh("未配置 Payment:HttpJson:CreateUrl，无法使用 http-json 通道");

        var notifyBase = (_configuration["Payment:NotifyBaseUrl"] ?? "").TrimEnd('/');
        var body = new Dictionary<string, object>
        {
            ["paymentOrderNo"] = order.PaymentOrderNo,
            ["amount"] = order.Amount,
            ["subject"] = order.Subject ?? order.BusinessOrderNo,
            ["notifyUrl"] = string.IsNullOrWhiteSpace(notifyBase)
                ? ""
                : $"{notifyBase}/api/pay/order/notify/{ChannelCode}"
        };

        var response = await _httpCaller.SendAsync(new HttpCallRequest
        {
            Method = HttpMethod.Post,
            Url = createUrl,
            JsonContent = body,
            Timeout = TimeSpan.FromSeconds(30),
            EnsureSuccessStatusCode = false
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw Oops.Oh($"http-json 下单失败：HTTP {(int)response.StatusCode}");

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(response.Body) ? "{}" : response.Body);
        var root = doc.RootElement;
        // 兼容统一包装 result 或扁平 JSON
        if (root.TryGetProperty("result", out var result) || root.TryGetProperty("Result", out result))
            root = result;

        var tradeNo = GetString(root, "channelTradeNo", "ChannelTradeNo", "tradeNo", "TradeNo");
        var cashier = GetString(root, "cashierUrl", "CashierUrl", "payUrl", "PayUrl");

        return new Dictionary<string, string>
        {
            ["cashierUrl"] = cashier ?? "",
            ["channelTradeNo"] = tradeNo ?? "",
            ["raw"] = response.Body ?? ""
        };
    }

    /// <inheritdoc />
    public Task<PaymentNotifyContext> ParseNotifyAsync(string rawBody, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawBody) ? "{}" : rawBody);
        var root = doc.RootElement;
        var orderNo = GetString(root, "paymentOrderNo", "PaymentOrderNo", "orderNo");
        if (string.IsNullOrWhiteSpace(orderNo))
            throw Oops.Oh("http-json 回调缺少 paymentOrderNo");

        var successText = GetString(root, "success", "Success", "status", "Status");
        var success = successText is null
            || successText.Equals("true", StringComparison.OrdinalIgnoreCase)
            || successText.Equals("1", StringComparison.OrdinalIgnoreCase)
            || successText.Equals("paid", StringComparison.OrdinalIgnoreCase)
            || successText.Equals("success", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(new PaymentNotifyContext
        {
            ChannelCode = ChannelCode,
            PaymentOrderNo = orderNo,
            ChannelTradeNo = GetString(root, "channelTradeNo", "ChannelTradeNo", "tradeNo") ?? $"HJ-{orderNo}",
            Status = success ? PaymentStatus.Paid : PaymentStatus.Failed,
            RawBody = rawBody
        });
    }

    private static string GetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var el))
            {
                return el.ValueKind switch
                {
                    JsonValueKind.String => el.GetString(),
                    JsonValueKind.Number => el.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => el.ToString()
                };
            }
        }
        return null;
    }
}

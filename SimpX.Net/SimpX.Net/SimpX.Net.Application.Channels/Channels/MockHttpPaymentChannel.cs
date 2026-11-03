using Microsoft.Extensions.Configuration;
using SimpX.Net.Feature.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Channels;

/// <summary>
/// 模拟支付渠道（示例）：下单时用 <see cref="IHttpCaller"/> 调本机「假网关」，演示真实通道怎么接 HTTP。
/// 渠道编码 <c>mock-http</c>。不接真实厂商。
/// </summary>
public sealed class MockHttpPaymentChannel : IPaymentChannel, ITransient
{
    private readonly IHttpCaller _httpCaller;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 注入 HTTP 调用与配置。
    /// </summary>
    /// <param name="httpCaller">外部 HTTP 调用</param>
    /// <param name="configuration">配置（读 Payment:MockHttp:GatewayBaseUrl）</param>
    public MockHttpPaymentChannel(IHttpCaller httpCaller, IConfiguration configuration)
    {
        _httpCaller = httpCaller;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public string ChannelCode => "mock-http";

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> CreateAsync(PaymentOrder order, CancellationToken cancellationToken = default)
    {
        var gatewayBase = (_configuration["Payment:MockHttp:GatewayBaseUrl"] ?? "http://127.0.0.1:5008").TrimEnd('/');
        var notifyBase = (_configuration["Payment:NotifyBaseUrl"] ?? gatewayBase).TrimEnd('/');
        var createUrl = $"{gatewayBase}/api/pay/mock-gateway/create";

        var body = new MockGatewayCreateRequest
        {
            PaymentOrderNo = order.PaymentOrderNo,
            Amount = order.Amount,
            Subject = order.Subject,
            NotifyUrl = $"{notifyBase}/api/pay/order/notify/{ChannelCode}"
        };

        // 示例：通道出站一律走 IHttpCaller，不要 new HttpClient
        var response = await _httpCaller.SendAsync(new HttpCallRequest
        {
            Method = HttpMethod.Post,
            Url = createUrl,
            JsonContent = body,
            Timeout = TimeSpan.FromSeconds(15),
            EnsureSuccessStatusCode = false
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw Oops.Oh($"模拟网关下单失败：HTTP {(int)response.StatusCode}");

        // 本机网关也走统一结果包装 { code, result }，这里拆出 result
        var gateway = UnwrapApiResult<MockGatewayCreateResponse>(response.Body)
                      ?? throw Oops.Oh("模拟网关返回无法解析");
        if (string.IsNullOrWhiteSpace(gateway.ChannelTradeNo))
            throw Oops.Oh(gateway.Message ?? "模拟网关未返回渠道单号");

        return new Dictionary<string, string>
        {
            ["cashierUrl"] = gateway.CashierUrl ?? string.Empty,
            ["channelTradeNo"] = gateway.ChannelTradeNo,
            ["mockHttp"] = "true",
            ["amount"] = order.Amount.ToString("0.00")
        };
    }

    /// <inheritdoc />
    public Task<PaymentNotifyContext> ParseNotifyAsync(string rawBody, CancellationToken cancellationToken = default)
    {
        // 示例回调：JSON { "paymentOrderNo","channelTradeNo","success" }
        MockGatewayNotifyBody notify;
        try
        {
            notify = JsonSerializer.Deserialize<MockGatewayNotifyBody>(rawBody);
        }
        catch (JsonException)
        {
            throw Oops.Oh("模拟通道回调不是合法 JSON");
        }

        if (notify == null || string.IsNullOrWhiteSpace(notify.PaymentOrderNo))
            throw Oops.Oh("模拟通道回调缺少 paymentOrderNo");

        return Task.FromResult(new PaymentNotifyContext
        {
            ChannelCode = ChannelCode,
            PaymentOrderNo = notify.PaymentOrderNo,
            ChannelTradeNo = string.IsNullOrWhiteSpace(notify.ChannelTradeNo)
                ? $"MOCK-{notify.PaymentOrderNo}"
                : notify.ChannelTradeNo,
            Status = notify.Success ? PaymentStatus.Paid : PaymentStatus.Failed,
            RawBody = rawBody
        });
    }

    /// <summary>
    /// 拆统一结果包装；没有包装时按正文本身反序列化。
    /// </summary>
    private static T UnwrapApiResult<T>(string body) where T : class
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (root.TryGetProperty("code", out var codeEl) || root.TryGetProperty("Code", out codeEl))
        {
            var code = codeEl.ValueKind == JsonValueKind.Number ? codeEl.GetInt32() : 0;
            if (code != 0 && code != 200)
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString()
                    : root.TryGetProperty("Message", out m) ? m.GetString()
                    : root.TryGetProperty("msg", out m) ? m.GetString()
                    : "业务失败";
                throw Oops.Oh($"模拟网关业务失败：{msg}");
            }
        }

        if (root.TryGetProperty("result", out var resultEl) || root.TryGetProperty("Result", out resultEl))
            return resultEl.Deserialize<T>(JsonOptions);

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>调假网关下单的请求体。</summary>
    private sealed class MockGatewayCreateRequest
    {
        [JsonPropertyName("paymentOrderNo")] public string PaymentOrderNo { get; set; }
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("subject")] public string Subject { get; set; }
        [JsonPropertyName("notifyUrl")] public string NotifyUrl { get; set; }
    }

    /// <summary>假网关下单返回。</summary>
    private sealed class MockGatewayCreateResponse
    {
        [JsonPropertyName("channelTradeNo")] public string ChannelTradeNo { get; set; }
        [JsonPropertyName("cashierUrl")] public string CashierUrl { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }
    }

    /// <summary>假网关异步通知正文。</summary>
    private sealed class MockGatewayNotifyBody
    {
        [JsonPropertyName("paymentOrderNo")] public string PaymentOrderNo { get; set; }
        [JsonPropertyName("channelTradeNo")] public string ChannelTradeNo { get; set; }
        [JsonPropertyName("success")] public bool Success { get; set; } = true;
    }
}

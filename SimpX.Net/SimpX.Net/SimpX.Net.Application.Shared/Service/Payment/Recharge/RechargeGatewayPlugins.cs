using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值出站网关。对齐旧 GoGateWay：提交后拿支付跳转/表单内容，不入账。
/// </summary>
public interface IRechargeGateway
{
    /// <summary>
    /// 向支付渠道提交一笔充值。
    /// </summary>
    /// <param name="request">请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>网关结果</returns>
    Task<RechargeGatewayResult> SubmitAsync(RechargeGatewayRequest request, CancellationToken cancellationToken = default);
}

/// <summary>充值出站请求。</summary>
public sealed class RechargeGatewayRequest
{
    /// <summary>站点。</summary>
    public int SiteID { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>站点通道编号。</summary>
    public int PayID { get; set; }

    /// <summary>系统通道编号。</summary>
    public int SysID { get; set; }

    /// <summary>订单号。</summary>
    public string OrderID { get; set; }

    /// <summary>金额。</summary>
    public decimal Money { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>付款人姓名（可选）。</summary>
    public string AccountName { get; set; }

    /// <summary>站点通道 Setting。</summary>
    public string ChannelSetting { get; set; }
}

/// <summary>充值出站结果。</summary>
public sealed class RechargeGatewayResult
{
    /// <summary>是否成功拿到支付信息。</summary>
    public bool Success { get; set; }

    /// <summary>失败说明。</summary>
    public string ErrorMessage { get; set; }

    /// <summary>跳转地址（可空）。</summary>
    public string PayUrl { get; set; }

    /// <summary>给前端的原文/表单（可空）。</summary>
    public string PayContent { get; set; }

    /// <summary>网关单号（可空，多数渠道回调才有）。</summary>
    public string SystemId { get; set; }
}

/// <summary>
/// 充值出站插件。按 Setting.Plugin 认领。
/// </summary>
public interface IRechargeGatewayPlugin
{
    /// <summary>插件名。</summary>
    string Name { get; }

    /// <summary>是否认领。</summary>
    /// <param name="request">请求</param>
    /// <returns>是否认领</returns>
    bool CanHandle(RechargeGatewayRequest request);

    /// <summary>提交。</summary>
    Task<RechargeGatewayResult> SubmitAsync(RechargeGatewayRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 路由：按 Plugin 选插件；未命中走 Stub（本地假支付页，不打外网）。
/// </summary>
public sealed class RechargeGatewayRouter : ITransient, IRechargeGateway
{
    private readonly StubRechargeGatewayPlugin _stub;
    private readonly IEnumerable<IRechargeGatewayPlugin> _plugins;

    /// <summary>
    /// 注入 Stub 与插件。
    /// </summary>
    /// <param name="stub">默认插件</param>
    /// <param name="plugins">全部插件</param>
    public RechargeGatewayRouter(StubRechargeGatewayPlugin stub, IEnumerable<IRechargeGatewayPlugin> plugins)
    {
        _stub = stub;
        _plugins = plugins ?? Enumerable.Empty<IRechargeGatewayPlugin>();
    }

    /// <inheritdoc />
    public Task<RechargeGatewayResult> SubmitAsync(
        RechargeGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plugin = _plugins.FirstOrDefault(p => p.CanHandle(request));
        if (plugin != null)
            return plugin.SubmitAsync(request, cancellationToken);

        var name = RechargeNotifyPluginSettings.ReadPlugin(request.ChannelSetting);
        if (!string.IsNullOrWhiteSpace(name) &&
            !string.Equals(name, "mock", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new RechargeGatewayResult
            {
                Success = false,
                ErrorMessage = $"未找到充值网关插件或配置不完整：{name}"
            });
        }

        return _stub.SubmitAsync(request, cancellationToken);
    }
}

/// <summary>
/// 本地 Stub：不打外网，返回可测的假支付地址。Plugin 空或 mock 时由路由回落。
/// </summary>
public sealed class StubRechargeGatewayPlugin : ITransient, IRechargeGatewayPlugin
{
    /// <inheritdoc />
    public string Name => "mock";

    /// <inheritdoc />
    public bool CanHandle(RechargeGatewayRequest request) => false;

    /// <inheritdoc />
    public Task<RechargeGatewayResult> SubmitAsync(
        RechargeGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orderId = (request.OrderID ?? string.Empty).Trim();
        var url = $"https://pay.local/mock?order={Uri.EscapeDataString(orderId)}&money={request.Money:0.00}";
        return Task.FromResult(new RechargeGatewayResult
        {
            Success = true,
            PayUrl = url,
            PayContent = url,
            SystemId = $"MOCK-{orderId}"
        });
    }
}

/// <summary>
/// 通用 HTTP 表单充值提交。Setting 须含 Plugin=http-md5、Gateway、Key。
/// </summary>
public sealed class HttpMd5RechargeGatewayPlugin : ITransient, IRechargeGatewayPlugin
{
    private readonly IHttpCaller _http;

    /// <inheritdoc />
    public string Name => "http-md5";

    /// <summary>
    /// 注入框架 HTTP 调用（IHttpCaller）。
    /// </summary>
    /// <param name="http">外部 HTTP</param>
    public HttpMd5RechargeGatewayPlugin(IHttpCaller http) =>
        _http = http;

    /// <inheritdoc />
    public bool CanHandle(RechargeGatewayRequest request)
    {
        var plugin = RechargeNotifyPluginSettings.ReadPlugin(request?.ChannelSetting);
        if (!string.Equals(plugin, Name, StringComparison.OrdinalIgnoreCase))
            return false;
        var gateway = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "Gateway", "");
        var key = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "Key", "");
        return !string.IsNullOrWhiteSpace(gateway) && !string.IsNullOrWhiteSpace(key);
    }

    /// <inheritdoc />
    public async Task<RechargeGatewayResult> SubmitAsync(
        RechargeGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gateway = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "Gateway", "");
        var key = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "Key", "");
        var merchantId = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "MerchantId", "");
        var notifyUrl = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "NotifyUrl", "");
        if (string.IsNullOrWhiteSpace(gateway) || string.IsNullOrWhiteSpace(key))
            return Fail("未配置 http-md5 Gateway/Key");

        var orderNo = (request.OrderID ?? string.Empty).Trim();
        var amount = request.Money.ToString("0.00");
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["merchant_id"] = merchantId,
            ["order_no"] = orderNo,
            ["amount"] = amount,
            ["account_name"] = request.AccountName ?? string.Empty,
            ["notify_url"] = notifyUrl
        };
        fields["sign"] = Md5Sign(fields, key);

        try
        {
            var resp = await _http.SendAsync(new HttpCallRequest
            {
                Method = HttpMethod.Post,
                Url = gateway,
                FormUrlEncodedContent = ToFormUrlEncoded(fields),
                EnsureSuccessStatusCode = false,
                Timeout = TimeSpan.FromSeconds(30)
            }, cancellationToken);
            var body = resp.Body ?? string.Empty;
            if (!resp.IsSuccessStatusCode)
                return Fail($"HTTP {(int)resp.StatusCode}: {Truncate(body, 80)}");

            var lower = body.ToLowerInvariant();
            if (lower.Contains("fail") || lower.Contains("error"))
                return Fail(Truncate(body, 120));

            var payUrl = TryPickPayUrl(body) ?? gateway;
            return new RechargeGatewayResult
            {
                Success = true,
                PayUrl = payUrl,
                PayContent = Truncate(body, 500),
                SystemId = TryPickSystemId(body)
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    private static RechargeGatewayResult Fail(string msg) => new()
    {
        Success = false,
        ErrorMessage = Truncate(msg, 120)
    };

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? string.Empty) : s[..max];

    /// <summary>拼 form-urlencoded 正文。</summary>
    private static string ToFormUrlEncoded(SortedDictionary<string, string> fields) =>
        string.Join("&", fields.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? string.Empty)}"));

    private static string Md5Sign(SortedDictionary<string, string> fields, string key)
    {
        var sb = new StringBuilder();
        foreach (var kv in fields)
        {
            if (string.Equals(kv.Key, "sign", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrEmpty(kv.Value))
                continue;
            if (sb.Length > 0)
                sb.Append('&');
            sb.Append(kv.Key).Append('=').Append(kv.Value);
        }

        sb.Append('&').Append("key=").Append(key ?? string.Empty);
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string TryPickPayUrl(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("pay_url", out var p) ||
                doc.RootElement.TryGetProperty("PayUrl", out p) ||
                doc.RootElement.TryGetProperty("url", out p))
                return p.GetString();
        }
        catch (JsonException)
        {
            // 非 JSON
        }

        if (body.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            body.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Truncate(body.Trim(), 500);
        return null;
    }

    private static string TryPickSystemId(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("system_id", out var p) ||
                doc.RootElement.TryGetProperty("SystemID", out p) ||
                doc.RootElement.TryGetProperty("trade_no", out p))
                return p.GetString();
        }
        catch (JsonException)
        {
            // ignore
        }

        return null;
    }
}

/// <summary>
/// 通用 HTTP JSON 充值提交。Setting 须含 Plugin=http-json-md5、Gateway、Key。
/// 与表单版并列：不少商户只要 JSON body + MD5。
/// </summary>
public sealed class HttpJsonMd5RechargeGatewayPlugin : ITransient, IRechargeGatewayPlugin
{
    private readonly IHttpCaller _http;

    /// <inheritdoc />
    public string Name => "http-json-md5";

    /// <summary>
    /// 注入框架 HTTP 调用（IHttpCaller）。
    /// </summary>
    /// <param name="http">外部 HTTP</param>
    public HttpJsonMd5RechargeGatewayPlugin(IHttpCaller http) =>
        _http = http;

    /// <inheritdoc />
    public bool CanHandle(RechargeGatewayRequest request)
    {
        var plugin = RechargeNotifyPluginSettings.ReadPlugin(request?.ChannelSetting);
        if (!string.Equals(plugin, Name, StringComparison.OrdinalIgnoreCase))
            return false;
        var gateway = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "Gateway", "");
        var key = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "Key", "");
        return !string.IsNullOrWhiteSpace(gateway) && !string.IsNullOrWhiteSpace(key);
    }

    /// <inheritdoc />
    public async Task<RechargeGatewayResult> SubmitAsync(
        RechargeGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gateway = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "Gateway", "");
        var key = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "Key", "");
        var merchantId = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "MerchantId", "");
        var notifyUrl = RechargeNotifyPluginSettings.ReadField(request.ChannelSetting, "NotifyUrl", "");
        if (string.IsNullOrWhiteSpace(gateway) || string.IsNullOrWhiteSpace(key))
            return JsonFail("未配置 http-json-md5 Gateway/Key");

        var orderNo = (request.OrderID ?? string.Empty).Trim();
        var amount = request.Money.ToString("0.00");
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["merchant_id"] = merchantId,
            ["order_no"] = orderNo,
            ["amount"] = amount,
            ["account_name"] = request.AccountName ?? string.Empty,
            ["notify_url"] = notifyUrl
        };
        fields["sign"] = JsonMd5Sign(fields, key);

        try
        {
            var resp = await _http.SendAsync(new HttpCallRequest
            {
                Method = HttpMethod.Post,
                Url = gateway,
                JsonContent = fields,
                EnsureSuccessStatusCode = false,
                Timeout = TimeSpan.FromSeconds(30)
            }, cancellationToken);
            var body = resp.Body ?? string.Empty;
            if (!resp.IsSuccessStatusCode)
                return JsonFail($"HTTP {(int)resp.StatusCode}: {JsonTruncate(body, 80)}");

            var lower = body.ToLowerInvariant();
            if (lower.Contains("fail") || lower.Contains("error"))
                return JsonFail(JsonTruncate(body, 120));

            var payUrl = TryPickJsonPayUrl(body) ?? gateway;
            return new RechargeGatewayResult
            {
                Success = true,
                PayUrl = payUrl,
                PayContent = JsonTruncate(body, 500),
                SystemId = TryPickJsonSystemId(body)
            };
        }
        catch (Exception ex)
        {
            return JsonFail(ex.Message);
        }
    }

    private static RechargeGatewayResult JsonFail(string msg) => new()
    {
        Success = false,
        ErrorMessage = JsonTruncate(msg, 120)
    };

    private static string JsonTruncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? string.Empty) : s[..max];

    private static string JsonMd5Sign(SortedDictionary<string, string> fields, string key)
    {
        var sb = new StringBuilder();
        foreach (var kv in fields)
        {
            if (string.Equals(kv.Key, "sign", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrEmpty(kv.Value))
                continue;
            if (sb.Length > 0)
                sb.Append('&');
            sb.Append(kv.Key).Append('=').Append(kv.Value);
        }

        sb.Append('&').Append("key=").Append(key ?? string.Empty);
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string TryPickJsonPayUrl(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("pay_url", out var p) ||
                doc.RootElement.TryGetProperty("PayUrl", out p) ||
                doc.RootElement.TryGetProperty("url", out p))
                return p.GetString();
        }
        catch (JsonException)
        {
            // 非 JSON
        }

        return null;
    }

    private static string TryPickJsonSystemId(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("system_id", out var p) ||
                doc.RootElement.TryGetProperty("SystemID", out p) ||
                doc.RootElement.TryGetProperty("trade_no", out p))
                return p.GetString();
        }
        catch (JsonException)
        {
            // ignore
        }

        return null;
    }
}

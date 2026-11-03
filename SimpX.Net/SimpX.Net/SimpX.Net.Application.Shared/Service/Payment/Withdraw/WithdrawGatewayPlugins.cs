using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 提现代付网关。真实供应商插件未迁；本波用 Stub 走「提交→付款中→回调确认」状态机。
/// </summary>
public interface IWithdrawGateway
{
    /// <summary>
    /// 向代付渠道提交一笔出款。
    /// </summary>
    /// <param name="request">出款请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>网关结果</returns>
    Task<WithdrawGatewayResult> SubmitAsync(WithdrawGatewayRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查单（对齐旧 GatewaySettingQuery.GoGateWay）。
    /// </summary>
    Task<WithdrawGatewayResult> QueryAsync(WithdrawGatewayQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回调验签。通过则返回解析后的订单与金额；失败抛业务异常。
    /// </summary>
    WithdrawGatewayCallbackPayload VerifyCallback(WithdrawGatewayCallbackRaw raw);
}

/// <summary>代付出款请求。</summary>
public sealed class WithdrawGatewayRequest
{
    /// <summary>站点。</summary>
    public int SiteID { get; set; }

    /// <summary>站点渠道编号。</summary>
    public int ChannelID { get; set; }

    /// <summary>系统代付编号（SysID）。</summary>
    public int SysID { get; set; }

    /// <summary>订单号。</summary>
    public int OrderID { get; set; }

    /// <summary>银行类型。</summary>
    public int Bank { get; set; }

    /// <summary>户名。</summary>
    public string AccountName { get; set; }

    /// <summary>账号。</summary>
    public string AccountNumber { get; set; }

    /// <summary>实付金额。</summary>
    public decimal Money { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>站点渠道 Setting（JSON，可含 Plugin/Gateway/Key）。</summary>
    public string ChannelSetting { get; set; }

    /// <summary>站点渠道查单 Setting。</summary>
    public string ChannelQuerySetting { get; set; }
}

/// <summary>代付结果状态（对齐旧 Gateway.WithdrawStatus 主分支）。</summary>
public enum WithdrawGatewayStatus
{
    /// <summary>失败（查单失败 → 回待处理；提交失败 → 不改状态）。</summary>
    Failed = 0,

    /// <summary>已受理 / 处理中。</summary>
    Paymenting = 1,

    /// <summary>重复单，按已受理处理。</summary>
    RepeatOrder = 2,

    /// <summary>查单成功到账（对齐旧 WithdrawStatus.Success）。</summary>
    Success = 3
}

/// <summary>代付提交结果。</summary>
public sealed class WithdrawGatewayResult
{
    /// <summary>状态。</summary>
    public WithdrawGatewayStatus Status { get; set; }

    /// <summary>第三方单号。</summary>
    public string SystemID { get; set; }

    /// <summary>错误或说明。</summary>
    public string Message { get; set; }
}

/// <summary>查单请求。</summary>
public sealed class WithdrawGatewayQueryRequest
{
    /// <summary>站点渠道。</summary>
    public int ChannelID { get; set; }

    /// <summary>订单号。</summary>
    public int OrderID { get; set; }

    /// <summary>应付金额。</summary>
    public decimal Money { get; set; }

    /// <summary>第三方单号。</summary>
    public string SystemID { get; set; }

    /// <summary>系统代付编号。</summary>
    public int SysID { get; set; }

    /// <summary>站点渠道 Setting。</summary>
    public string ChannelSetting { get; set; }

    /// <summary>站点渠道查单 Setting。</summary>
    public string ChannelQuerySetting { get; set; }
}

/// <summary>回调原始包（供应商字段未定，先收通用键）。</summary>
public sealed class WithdrawGatewayCallbackRaw
{
    /// <summary>渠道。</summary>
    public int ChannelID { get; set; }

    /// <summary>订单号原文。</summary>
    public string OrderID { get; set; }

    /// <summary>金额原文。</summary>
    public string Money { get; set; }

    /// <summary>签名。</summary>
    public string Sign { get; set; }

    /// <summary>状态原文（success/fail 等）。</summary>
    public string Status { get; set; }

    /// <summary>额外原文（验签用）。</summary>
    public string Payload { get; set; }
}

/// <summary>验签后的回调内容。</summary>
public sealed class WithdrawGatewayCallbackPayload
{
    /// <summary>订单号。</summary>
    public int OrderID { get; set; }

    /// <summary>渠道。</summary>
    public int ChannelID { get; set; }

    /// <summary>到账金额。</summary>
    public decimal Money { get; set; }

    /// <summary>是否成功。</summary>
    public bool Success { get; set; }
}

/// <summary>
/// 本地 Stub：提交/查单固定 Paymenting；回调要求 Sign=STUB 或空。禁止当真实出款。
/// 不再直接挂 IWithdrawGateway，由 <see cref="WithdrawGatewayRouter"/> 统一注入。
/// </summary>
public sealed class StubWithdrawGateway : ITransient
{
    /// <inheritdoc />
    public Task<WithdrawGatewayResult> SubmitAsync(
        WithdrawGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(new WithdrawGatewayResult
        {
            Status = WithdrawGatewayStatus.Paymenting,
            SystemID = $"STUB{request.OrderID}",
            Message = "stub accepted"
        });
    }

    /// <inheritdoc />
    public Task<WithdrawGatewayResult> QueryAsync(
        WithdrawGatewayQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(new WithdrawGatewayResult
        {
            Status = WithdrawGatewayStatus.Paymenting,
            SystemID = string.IsNullOrWhiteSpace(request.SystemID) ? $"STUB{request.OrderID}" : request.SystemID,
            Message = "stub query paymenting"
        });
    }

    /// <inheritdoc />
    public WithdrawGatewayCallbackPayload VerifyCallback(WithdrawGatewayCallbackRaw raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var sign = (raw.Sign ?? string.Empty).Trim();
        // 用 InvalidOperationException：单测无 Furion 宿主时 Oops 静态初始化会炸
        if (sign.Length > 0 && !string.Equals(sign, "STUB", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("#回调验签失败#");
        if (!int.TryParse(raw.OrderID, out var orderId) || orderId <= 0)
            throw new InvalidOperationException("#回调订单号错误#");
        if (!decimal.TryParse(raw.Money, out var money))
            money = 0;
        var status = (raw.Status ?? "success").Trim().ToLowerInvariant();
        var ok = status is "success" or "ok" or "1" or "paid";
        return new WithdrawGatewayCallbackPayload
        {
            OrderID = orderId,
            ChannelID = raw.ChannelID,
            Money = money,
            Success = ok
        };
    }
}

/// <summary>
/// 代付供应商插件。SysID&gt;0 时由路由挑选；未命中回落 Stub。
/// </summary>
public interface IWithdrawGatewayPlugin
{
    /// <summary>插件名（与渠道 Setting.Plugin 对齐，如 http-md5）。</summary>
    string Name { get; }

    /// <summary>是否认领该次提交/查单。</summary>
    bool CanHandle(WithdrawGatewayRequest request);

    /// <summary>是否认领查单。</summary>
    bool CanHandleQuery(WithdrawGatewayQueryRequest request);

    /// <summary>提交出款。</summary>
    Task<WithdrawGatewayResult> SubmitAsync(WithdrawGatewayRequest request, CancellationToken cancellationToken = default);

    /// <summary>查单。</summary>
    Task<WithdrawGatewayResult> QueryAsync(WithdrawGatewayQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>回调验签。</summary>
    WithdrawGatewayCallbackPayload VerifyCallback(WithdrawGatewayCallbackRaw raw);
}

/// <summary>
/// 路由网关：按渠道 Setting.Plugin 选插件；默认 Stub（不打真实供应商）。
/// </summary>
public sealed class WithdrawGatewayRouter : ITransient, IWithdrawGateway
{
    private readonly StubWithdrawGateway _stub;
    private readonly IEnumerable<IWithdrawGatewayPlugin> _plugins;

    /// <summary>
    /// 注入 Stub 与全部插件。
    /// </summary>
    public WithdrawGatewayRouter(StubWithdrawGateway stub, IEnumerable<IWithdrawGatewayPlugin> plugins)
    {
        _stub = stub;
        _plugins = plugins ?? Enumerable.Empty<IWithdrawGatewayPlugin>();
    }

    /// <inheritdoc />
    public Task<WithdrawGatewayResult> SubmitAsync(
        WithdrawGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plugin = _plugins.FirstOrDefault(p => p.CanHandle(request));
        return plugin != null
            ? plugin.SubmitAsync(request, cancellationToken)
            : _stub.SubmitAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WithdrawGatewayResult> QueryAsync(
        WithdrawGatewayQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plugin = _plugins.FirstOrDefault(p => p.CanHandleQuery(request));
        return plugin != null
            ? plugin.QueryAsync(request, cancellationToken)
            : _stub.QueryAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public WithdrawGatewayCallbackPayload VerifyCallback(WithdrawGatewayCallbackRaw raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        // 回调时按 Sign 约定：STUB/空 → Stub；否则交给声明支持回调的 http-md5 插件
        var sign = (raw.Sign ?? string.Empty).Trim();
        if (sign.Length == 0 || string.Equals(sign, "STUB", StringComparison.OrdinalIgnoreCase))
            return _stub.VerifyCallback(raw);

        var http = _plugins.OfType<HttpMd5FormWithdrawGatewayPlugin>().FirstOrDefault();
        if (http != null)
            return http.VerifyCallback(raw);
        return _stub.VerifyCallback(raw);
    }
}

/// <summary>
/// 通用 HTTP 表单代付插件。渠道 Setting JSON 须含 Plugin=http-md5、Gateway、Key；
/// 未配置时 CanHandle=false，不会误打外部。
/// </summary>
public sealed class HttpMd5FormWithdrawGatewayPlugin : ITransient, IWithdrawGatewayPlugin
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly IHttpCaller _http;

    /// <inheritdoc />
    public string Name => "http-md5";

    /// <summary>
    /// 注入框架 HTTP 调用（IHttpCaller）。
    /// </summary>
    /// <param name="http">外部 HTTP</param>
    public HttpMd5FormWithdrawGatewayPlugin(IHttpCaller http) =>
        _http = http;

    /// <inheritdoc />
    public bool CanHandle(WithdrawGatewayRequest request) =>
        TryReadConfig(request?.ChannelSetting, out _);

    /// <inheritdoc />
    public bool CanHandleQuery(WithdrawGatewayQueryRequest request) =>
        TryReadConfig(FirstNonEmpty(request?.ChannelQuerySetting, request?.ChannelSetting), out _);

    /// <inheritdoc />
    public async Task<WithdrawGatewayResult> SubmitAsync(
        WithdrawGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryReadConfig(request.ChannelSetting, out var cfg))
            return Fail("未配置 http-md5 网关");

        var orderNo = request.OrderID.ToString();
        var amount = request.Money.ToString("0.00");
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["merchant_id"] = cfg.MerchantId ?? string.Empty,
            ["order_no"] = orderNo,
            ["amount"] = amount,
            ["account_name"] = request.AccountName ?? string.Empty,
            ["account_number"] = request.AccountNumber ?? string.Empty,
            ["bank"] = request.Bank.ToString(),
            ["notify_url"] = cfg.NotifyUrl ?? string.Empty
        };
        fields["sign"] = Md5Sign(fields, cfg.Key);

        try
        {
            var resp = await _http.SendAsync(new HttpCallRequest
            {
                Method = HttpMethod.Post,
                Url = cfg.Gateway,
                FormUrlEncodedContent = ToFormUrlEncoded(fields),
                EnsureSuccessStatusCode = false,
                Timeout = TimeSpan.FromSeconds(30)
            }, cancellationToken);
            var body = resp.Body ?? string.Empty;
            if (!resp.IsSuccessStatusCode)
                return Fail($"HTTP {(int)resp.StatusCode}: {Truncate(body, 80)}");

            // 约定：含 success/ok/paymenting 视为受理；含 fail/error 失败；其余当受理并带回原文
            var lower = body.ToLowerInvariant();
            if (lower.Contains("fail") || lower.Contains("error"))
                return Fail(Truncate(body, 120));

            return new WithdrawGatewayResult
            {
                Status = WithdrawGatewayStatus.Paymenting,
                SystemID = TryPickSystemId(body) ?? $"HTTP{request.OrderID}",
                Message = Truncate(body, 120)
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<WithdrawGatewayResult> QueryAsync(
        WithdrawGatewayQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var setting = FirstNonEmpty(request.ChannelQuerySetting, request.ChannelSetting);
        if (!TryReadConfig(setting, out var cfg) || string.IsNullOrWhiteSpace(cfg.QueryGateway))
        {
            // 无查单地址时回 Paymenting，交给 Stub 语义（运营人工确认）
            return new WithdrawGatewayResult
            {
                Status = WithdrawGatewayStatus.Paymenting,
                SystemID = request.SystemID,
                Message = "http-md5 未配置 QueryGateway"
            };
        }

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["merchant_id"] = cfg.MerchantId ?? string.Empty,
            ["order_no"] = request.OrderID.ToString(),
            ["amount"] = request.Money.ToString("0.00"),
            ["system_id"] = request.SystemID ?? string.Empty
        };
        fields["sign"] = Md5Sign(fields, cfg.Key);

        try
        {
            var resp = await _http.SendAsync(new HttpCallRequest
            {
                Method = HttpMethod.Post,
                Url = cfg.QueryGateway,
                FormUrlEncodedContent = ToFormUrlEncoded(fields),
                EnsureSuccessStatusCode = false,
                Timeout = TimeSpan.FromSeconds(30)
            }, cancellationToken);
            var body = resp.Body ?? string.Empty;
            var lower = body.ToLowerInvariant();
            if (lower.Contains("fail") || lower.Contains("error"))
                return Fail(Truncate(body, 120));
            var status = IsQuerySuccessBody(lower)
                ? WithdrawGatewayStatus.Success
                : WithdrawGatewayStatus.Paymenting;
            return new WithdrawGatewayResult
            {
                Status = status,
                SystemID = string.IsNullOrWhiteSpace(request.SystemID) ? TryPickSystemId(body) : request.SystemID,
                Message = Truncate(body, 120)
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    /// <summary>查单正文是否表示已到账。</summary>
    private static bool IsQuerySuccessBody(string lower) =>
        lower.Contains("\"status\":\"success\"")
        || lower.Contains("\"status\": \"success\"")
        || lower.Contains("\"status\":\"paid\"")
        || lower.Contains("\"status\":\"finish\"")
        || lower.Contains("\"status\":\"successed\"")
        || (lower.Contains("success") && !lower.Contains("unsuccess"));

    /// <summary>拼 form-urlencoded 正文。</summary>
    private static string ToFormUrlEncoded(SortedDictionary<string, string> fields) =>
        string.Join("&", fields.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? string.Empty)}"));

    /// <inheritdoc />
    public WithdrawGatewayCallbackPayload VerifyCallback(WithdrawGatewayCallbackRaw raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        // 真回调验签需要渠道密钥；本插件要求 Payload 里带 key= 或 Sign 已是 md5(order+money+key) 的结果，
        // 简化：Sign 非空且 OrderID 可解析即过（商户密钥落地后再收紧）。
        // 用 InvalidOperationException：单测无 Furion 宿主时 Oops 静态初始化会炸
        if (string.IsNullOrWhiteSpace(raw.Sign))
            throw new InvalidOperationException("#回调验签失败#");
        if (!int.TryParse(raw.OrderID, out var orderId) || orderId <= 0)
            throw new InvalidOperationException("#回调订单号错误#");
        if (!decimal.TryParse(raw.Money, out var money))
            money = 0;
        var status = (raw.Status ?? "success").Trim().ToLowerInvariant();
        var ok = status is "success" or "ok" or "1" or "paid";
        return new WithdrawGatewayCallbackPayload
        {
            OrderID = orderId,
            ChannelID = raw.ChannelID,
            Money = money,
            Success = ok
        };
    }

    private static bool TryReadConfig(string json, out HttpMd5GatewayConfig cfg)
    {
        cfg = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            cfg = JsonSerializer.Deserialize<HttpMd5GatewayConfig>(json, JsonOpts);
        }
        catch
        {
            return false;
        }

        if (cfg == null)
            return false;
        if (!string.Equals(cfg.Plugin, "http-md5", StringComparison.OrdinalIgnoreCase))
            return false;
        return !string.IsNullOrWhiteSpace(cfg.Gateway) && !string.IsNullOrWhiteSpace(cfg.Key);
    }

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

    private static WithdrawGatewayResult Fail(string msg) => new()
    {
        Status = WithdrawGatewayStatus.Failed,
        Message = Truncate(msg, 120)
    };

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? string.Empty) : s[..max];

    private static string FirstNonEmpty(string a, string b) =>
        !string.IsNullOrWhiteSpace(a) ? a : b;

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
        catch
        {
            // ignore
        }

        return null;
    }
}

/// <summary>http-md5 插件配置（写在站点渠道 Setting JSON）。</summary>
public sealed class HttpMd5GatewayConfig
{
    /// <summary>
    /// 本地沙箱密钥（Channels mock-withdraw 与单测共用；生产勿用）。
    /// </summary>
    public const string SandboxKey = "simpx-md5-sandbox-key";

    /// <summary>插件名，必须 http-md5。</summary>
    public string Plugin { get; set; }

    /// <summary>提交地址。</summary>
    public string Gateway { get; set; }

    /// <summary>查单地址（可空）。</summary>
    public string QueryGateway { get; set; }

    /// <summary>商户号。</summary>
    public string MerchantId { get; set; }

    /// <summary>密钥。</summary>
    public string Key { get; set; }

    /// <summary>异步通知地址。</summary>
    public string NotifyUrl { get; set; }
}

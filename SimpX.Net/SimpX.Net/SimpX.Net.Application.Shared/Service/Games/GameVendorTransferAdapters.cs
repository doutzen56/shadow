using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 厂商资金转账适配。各 GameType 可各自实现；未认领时由 <see cref="GameTransferService"/> 回落 <see cref="StubGameVendorTransfer"/>。
/// </summary>
public interface IGameVendorTransfer
{
    /// <summary>
    /// 是否认领该游戏类型。
    /// </summary>
    /// <param name="gameType">旧版 GameType 整型</param>
    /// <returns>认领返回 true</returns>
    bool CanHandle(int gameType);

    /// <summary>
    /// 向厂商发起一笔转入/转出。
    /// </summary>
    /// <param name="context">转账上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>厂商结果</returns>
    Task<GameVendorTransferOutcome> TransferAsync(
        GameVendorTransferContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 厂商转账入参。
/// </summary>
public sealed class GameVendorTransferContext
{
    /// <summary>站点。</summary>
    public int SiteId { get; set; }

    /// <summary>会员。</summary>
    public int UserId { get; set; }

    /// <summary>游戏类型。</summary>
    public int GameType { get; set; }

    /// <summary>转账单号（本地 OrderID）。</summary>
    public int OrderId { get; set; }

    /// <summary>金额（正数）。</summary>
    public decimal Money { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>
    /// 方向：1=IN 中心→厂商，2=OUT 厂商→中心（对齐旧 DirectionType）。
    /// </summary>
    public int Action { get; set; }
}

/// <summary>
/// 厂商转账结果。
/// </summary>
public sealed class GameVendorTransferOutcome
{
    /// <summary>是否成功。</summary>
    public bool Ok { get; set; }

    /// <summary>失败或成功说明。</summary>
    public string Message { get; set; }

    /// <summary>远程订单号（可空）。</summary>
    public string ResultId { get; set; }
}

/// <summary>
/// 厂商转账 Stub。不认领具体 GameType；未命中真实适配时由 <see cref="GameTransferService"/> 回落至此。
/// 默认 AlwaysSuccess；单测或排障时可注入失败。
/// </summary>
public sealed class StubGameVendorTransfer : ITransient, IGameVendorTransfer
{
    /// <summary>
    /// 为 true 时一律成功；为 false 时一律失败。可在单测里改。
    /// </summary>
    public bool AlwaysSuccess { get; set; } = true;

    /// <summary>失败时返回的说明。</summary>
    public string FailMessage { get; set; } = "#厂商转账失败#";

    /// <inheritdoc />
    /// <remarks>Stub 永不主动认领，避免挡住真实 HTTP 适配。</remarks>
    public bool CanHandle(int gameType) => false;

    /// <inheritdoc />
    public Task<GameVendorTransferOutcome> TransferAsync(
        GameVendorTransferContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (AlwaysSuccess)
        {
            return Task.FromResult(new GameVendorTransferOutcome
            {
                Ok = true,
                Message = "#成功#",
                ResultId = $"stub-{context.OrderId}"
            });
        }

        return Task.FromResult(new GameVendorTransferOutcome
        {
            Ok = false,
            Message = string.IsNullOrWhiteSpace(FailMessage) ? "#厂商转账失败#" : FailMessage,
            ResultId = string.Empty
        });
    }
}

/// <summary>
/// 通用 JSON HTTP 厂商转账骨架。认领测试类型 <see cref="HandledGameType"/>（9999）。
/// 出站走 <see cref="IHttpCaller"/>，禁止 new HttpClient。
/// 配置键：Games:HttpJsonTransfer:TransferUrl。
/// </summary>
public sealed class HttpJsonGameVendorTransfer : ITransient, IGameVendorTransfer
{
    /// <summary>本适配认领的游戏类型（测试用；真实厂商另起实现并改常量）。</summary>
    public const int HandledGameType = 9999;

    private readonly IHttpCaller _http;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 单测可直接指定地址；生产为空时读配置 Games:HttpJsonTransfer:TransferUrl。
    /// </summary>
    public string TransferUrl { get; set; }

    /// <summary>
    /// 注入 HTTP 调用与配置。
    /// </summary>
    /// <param name="http">外部 HTTP</param>
    /// <param name="configuration">配置（可读 TransferUrl）</param>
    public HttpJsonGameVendorTransfer(IHttpCaller http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public bool CanHandle(int gameType) => gameType == HandledGameType;

    /// <inheritdoc />
    public async Task<GameVendorTransferOutcome> TransferAsync(
        GameVendorTransferContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var url = ResolveTransferUrl();
        if (string.IsNullOrWhiteSpace(url))
        {
            return Fail("未配置 Games:HttpJsonTransfer:TransferUrl");
        }

        var body = new Dictionary<string, object>
        {
            ["siteId"] = context.SiteId,
            ["userId"] = context.UserId,
            ["gameType"] = context.GameType,
            ["orderId"] = context.OrderId,
            ["money"] = context.Money,
            ["currency"] = context.Currency,
            ["action"] = context.Action
        };

        try
        {
            var response = await _http.SendAsync(new HttpCallRequest
            {
                Method = HttpMethod.Post,
                Url = url,
                JsonContent = body,
                EnsureSuccessStatusCode = false,
                Timeout = TimeSpan.FromSeconds(30)
            }, cancellationToken);

            var raw = response.Body ?? string.Empty;
            if (!response.IsSuccessStatusCode)
                return Fail($"HTTP {(int)response.StatusCode}: {Truncate(raw, 80)}");

            return MapBody(raw);
        }
        catch (Exception ex)
        {
            return Fail(string.IsNullOrWhiteSpace(ex.Message) ? "#转账失败#" : Truncate(ex.Message, 120));
        }
    }

    /// <summary>读 TransferUrl：属性优先，否则配置。</summary>
    private string ResolveTransferUrl()
    {
        if (!string.IsNullOrWhiteSpace(TransferUrl))
            return TransferUrl.Trim();
        return (_configuration?["Games:HttpJsonTransfer:TransferUrl"] ?? string.Empty).Trim();
    }

    /// <summary>
    /// 把厂商 JSON 映成成功/失败。约定字段：success|ok、message|msg、resultId|orderNo。
    /// </summary>
    private static GameVendorTransferOutcome MapBody(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Fail("#厂商无响应#");

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var ok = ReadBool(root, "success", "Success", "ok", "Ok");
            var message = ReadString(root, "message", "Message", "msg", "Msg") ?? string.Empty;
            var resultId = ReadString(root, "resultId", "ResultId", "orderNo", "OrderNo") ?? string.Empty;

            if (!ok)
            {
                return new GameVendorTransferOutcome
                {
                    Ok = false,
                    Message = string.IsNullOrWhiteSpace(message) ? "#转账失败#" : Truncate(message, 120),
                    ResultId = resultId
                };
            }

            return new GameVendorTransferOutcome
            {
                Ok = true,
                Message = string.IsNullOrWhiteSpace(message) ? "#成功#" : Truncate(message, 120),
                ResultId = resultId
            };
        }
        catch (JsonException)
        {
            return Fail("#厂商响应格式错误#");
        }
    }

    private static GameVendorTransferOutcome Fail(string message) => new()
    {
        Ok = false,
        Message = Truncate(message, 120),
        ResultId = string.Empty
    };

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? string.Empty) : s[..max];

    private static bool ReadBool(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var el))
                continue;
            return el.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => el.TryGetInt32(out var n) && n != 0,
                JsonValueKind.String => IsTruthy(el.GetString()),
                _ => false
            };
        }

        return false;
    }

    private static bool IsTruthy(string text) =>
        !string.IsNullOrWhiteSpace(text) &&
        (text.Equals("true", StringComparison.OrdinalIgnoreCase) ||
         text.Equals("1", StringComparison.OrdinalIgnoreCase) ||
         text.Equals("success", StringComparison.OrdinalIgnoreCase) ||
         text.Equals("ok", StringComparison.OrdinalIgnoreCase));

    private static string ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var el))
                continue;
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => el.ToString()
            };
        }

        return null;
    }
}

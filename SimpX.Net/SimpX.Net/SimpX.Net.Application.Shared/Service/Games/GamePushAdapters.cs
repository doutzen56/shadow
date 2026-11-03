using System.Security.Cryptography;
using System.Text;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏厂商推送适配。按 gameType 认领；未认领时入口返回 505。
/// </summary>
public interface IGamePushHandler
{
    /// <summary>是否支持该路径上的类型名或编号。</summary>
    /// <param name="gameType">路径段</param>
    /// <returns>支持返回 true</returns>
    bool Supports(string gameType);

    /// <summary>
    /// 验签并处理推送；成功时可触发入账。
    /// </summary>
    /// <param name="gameType">路径段</param>
    /// <param name="rawBody">原始正文</param>
    /// <param name="form">表单/查询合并</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>给厂商的应答正文与 HTTP 状态</returns>
    Task<GamePushOutcome> HandleAsync(
        string gameType,
        string rawBody,
        Dictionary<string, string> form,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 推送处理结果。
/// </summary>
public sealed class GamePushOutcome
{
    /// <summary>HTTP 状态码。</summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>应答正文。</summary>
    public string Body { get; set; } = "OK";

    /// <summary>是否验签通过并处理成功。</summary>
    public bool Ok { get; set; }
}

/// <summary>
/// Mock 游戏推送：路径 gameType=mock；验签 sign=md5(order+amount+key)；可选入账。
/// 不接真实厂商。
/// </summary>
public sealed class MockGamePushHandler : ITransient, IGamePushHandler
{
    /// <summary>沙箱密钥。</summary>
    public const string SandboxKey = "simpx-game-push-mock-key";

    private readonly IMoneyLedger _money;

    /// <summary>
    /// 注入账本（可空场景下仍注册）。
    /// </summary>
    /// <param name="money">账本</param>
    public MockGamePushHandler(IMoneyLedger money) => _money = money;

    /// <inheritdoc />
    public bool Supports(string gameType) =>
        string.Equals(gameType?.Trim(), "mock", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<GamePushOutcome> HandleAsync(
        string gameType,
        string rawBody,
        Dictionary<string, string> form,
        CancellationToken cancellationToken = default)
    {
        form ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var order = Get(form, "order", "Order", "orderNo");
        var amountText = Get(form, "amount", "Amount", "money");
        var sign = Get(form, "sign", "Sign");
        if (string.IsNullOrWhiteSpace(order) || string.IsNullOrWhiteSpace(amountText))
            return Fail("缺少 order/amount");
        if (!decimal.TryParse(amountText, out var amount) || amount == 0)
            return Fail("amount 无效");

        var expect = Md5Hex(order + amountText + SandboxKey);
        if (!string.Equals(expect, sign, StringComparison.OrdinalIgnoreCase))
            return Fail("sign 校验失败");

        // 可选入账：带 siteId+userId 才动余额；资金类型默认手工加款 250（避免走充值=1 业务禁区）
        if (int.TryParse(Get(form, "siteId", "SiteId"), out var siteId) && siteId > 0 &&
            int.TryParse(Get(form, "userId", "UserId"), out var userId) && userId > 0)
        {
            var currency = int.TryParse(Get(form, "currency", "Currency"), out var c) && c > 0 ? c : 1;
            var moneyType = int.TryParse(Get(form, "moneyType", "MoneyType"), out var mt) && mt > 0 ? mt : 250;
            var source = order.Length <= 20 ? order : order[..20];
            await _money.AddMoneyLogAsync(
                siteId,
                userId,
                amount,
                moneyType,
                source,
                "mock game push",
                currency,
                cancellationToken);
        }

        return new GamePushOutcome { Ok = true, StatusCode = 200, Body = "OK" };
    }

    private static GamePushOutcome Fail(string msg) => new()
    {
        Ok = false,
        StatusCode = 400,
        Body = msg
    };

    private static string Get(Dictionary<string, string> form, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (form.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return string.Empty;
    }

    private static string Md5Hex(string raw)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(raw ?? string.Empty));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

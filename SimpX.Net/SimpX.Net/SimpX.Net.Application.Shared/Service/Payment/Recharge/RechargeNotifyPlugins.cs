using System.Text.Json;
using SimpX.Net.Data.Entity.Sites;
using System.Security.Cryptography;
using System.Text;
using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值回调解析结果（插件输出）。
/// </summary>
public sealed class RechargeNotifyParseResult
{
    /// <summary>网关是否判成功。</summary>
    public bool Success { get; set; }

    /// <summary>失败说明。</summary>
    public string FailMessage { get; set; }

    /// <summary>充值订单号。</summary>
    public string OrderId { get; set; }

    /// <summary>网关单号。</summary>
    public string SystemId { get; set; }

    /// <summary>到账金额。</summary>
    public decimal Money { get; set; }

    /// <summary>付款账户。</summary>
    public string PayAccount { get; set; }
}

/// <summary>
/// 站点充值通道回调插件。按 Setting.Plugin 认领。
/// </summary>
public interface IRechargeNotifyPlugin
{
    /// <summary>插件名（与 Setting.Plugin 对齐）。</summary>
    string Name { get; }

    /// <summary>是否认领该通道。</summary>
    /// <param name="payment">站点通道</param>
    /// <returns>是否认领</returns>
    bool CanHandle(SitePayment payment);

    /// <summary>
    /// 解析并验签。失败时 Success=false 或抛业务异常。
    /// </summary>
    /// <param name="rawBody">回调正文</param>
    /// <param name="payment">站点通道</param>
    /// <returns>解析结果</returns>
    RechargeNotifyParseResult ParseAndVerify(string rawBody, SitePayment payment);
}

/// <summary>
/// 从 Setting 读 Plugin 名。
/// </summary>
public static class RechargeNotifyPluginSettings
{
    /// <summary>
    /// 读 Plugin；没有则空（走 mock）。
    /// </summary>
    /// <param name="settingRaw">Setting 原文</param>
    /// <returns>插件名</returns>
    public static string ReadPlugin(string settingRaw)
    {
        if (string.IsNullOrWhiteSpace(settingRaw))
            return string.Empty;
        var trimmed = settingRaw.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(settingRaw);
                if (doc.RootElement.TryGetProperty("Plugin", out var p) ||
                    doc.RootElement.TryGetProperty("plugin", out p))
                    return p.ValueKind == JsonValueKind.String ? (p.GetString() ?? string.Empty) : p.ToString();
            }
            catch (JsonException)
            {
                // query
            }
        }
        var map = SiteSettingQuery.Parse(settingRaw);
        return SiteSettingQuery.Get(map, "Plugin");
    }

    /// <summary>
    /// 读自定义字段名；没有用默认。
    /// </summary>
    /// <param name="settingRaw">Setting</param>
    /// <param name="name">键</param>
    /// <param name="fallback">默认</param>
    /// <returns>字段名</returns>
    public static string ReadField(string settingRaw, string name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(settingRaw))
            return fallback;
        var trimmed = settingRaw.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(settingRaw);
                if (doc.RootElement.TryGetProperty(name, out var p))
                {
                    var v = p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
                    if (!string.IsNullOrWhiteSpace(v))
                        return v!;
                }
            }
            catch (JsonException)
            {
                // query
            }
        }
        var map = SiteSettingQuery.Parse(settingRaw);
        var got = SiteSettingQuery.Get(map, name);
        return string.IsNullOrWhiteSpace(got) ? fallback : got;
    }
}

/// <summary>
/// 默认 Mock 表单插件（无 Plugin 或 Plugin=mock）。
/// </summary>
public sealed class MockFormRechargeNotifyPlugin : ITransient, IRechargeNotifyPlugin
{
    /// <inheritdoc />
    public string Name => "mock";

    /// <inheritdoc />
    public bool CanHandle(SitePayment payment)
    {
        var plugin = RechargeNotifyPluginSettings.ReadPlugin(payment?.SettingString);
        return string.IsNullOrWhiteSpace(plugin)
               || string.Equals(plugin, "mock", StringComparison.OrdinalIgnoreCase)
               || string.Equals(plugin, "demo", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public RechargeNotifyParseResult ParseAndVerify(string rawBody, SitePayment payment)
    {
        var parsed = RechargeNotifyMockBody.Parse(rawBody ?? string.Empty);
        var key = RechargeNotifySecurity.ReadNotifyKey(payment?.SettingString);
        if (!RechargeNotifySecurity.VerifyMockMd5(parsed.Fields, key))
            return new RechargeNotifyParseResult { Success = false, FailMessage = "#回调验签失败#" };

        if (!parsed.Success)
            return new RechargeNotifyParseResult
            {
                Success = false,
                FailMessage = string.IsNullOrEmpty(parsed.FailMessage) ? "回调失败" : parsed.FailMessage
            };

        return new RechargeNotifyParseResult
        {
            Success = true,
            OrderId = parsed.OrderId,
            SystemId = parsed.SystemId,
            Money = parsed.Money,
            PayAccount = parsed.PayAccount
        };
    }
}

/// <summary>
/// HTTP 表单 MD5 充值回调插件。Setting 须 Plugin=http-md5 且 Key/NotifyKey 非空。
/// 可选字段名：OrderField、MoneyField、TradeNoField、SuccessField、SuccessValue。
/// </summary>
public sealed class HttpMd5RechargeNotifyPlugin : ITransient, IRechargeNotifyPlugin
{
    /// <inheritdoc />
    public string Name => "http-md5";

    /// <inheritdoc />
    public bool CanHandle(SitePayment payment)
    {
        var plugin = RechargeNotifyPluginSettings.ReadPlugin(payment?.SettingString);
        if (!string.Equals(plugin, "http-md5", StringComparison.OrdinalIgnoreCase))
            return false;
        return !string.IsNullOrWhiteSpace(RechargeNotifySecurity.ReadNotifyKey(payment?.SettingString));
    }

    /// <inheritdoc />
    public RechargeNotifyParseResult ParseAndVerify(string rawBody, SitePayment payment)
    {
        var setting = payment?.SettingString ?? string.Empty;
        var key = RechargeNotifySecurity.ReadNotifyKey(setting);
        if (string.IsNullOrWhiteSpace(key))
            return new RechargeNotifyParseResult { Success = false, FailMessage = "通道未配置 Key" };

        var fields = ParseForm(rawBody ?? string.Empty);
        if (!RechargeNotifySecurity.VerifyMockMd5(fields, key))
            return new RechargeNotifyParseResult { Success = false, FailMessage = "#回调验签失败#" };

        var orderField = RechargeNotifyPluginSettings.ReadField(setting, "OrderField", "orderId");
        var moneyField = RechargeNotifyPluginSettings.ReadField(setting, "MoneyField", "money");
        var tradeField = RechargeNotifyPluginSettings.ReadField(setting, "TradeNoField", "systemId");
        var successField = RechargeNotifyPluginSettings.ReadField(setting, "SuccessField", "success");
        var successValue = RechargeNotifyPluginSettings.ReadField(setting, "SuccessValue", "true");

        string Pick(string primary, params string[] alts)
        {
            if (fields.TryGetValue(primary, out var v) && !string.IsNullOrWhiteSpace(v))
                return v;
            foreach (var a in alts)
            {
                if (fields.TryGetValue(a, out var x) && !string.IsNullOrWhiteSpace(x))
                    return x;
            }
            return string.Empty;
        }

        var successRaw = Pick(successField, "success", "status");
        var ok = string.IsNullOrEmpty(successRaw)
                 || successRaw.Equals(successValue, StringComparison.OrdinalIgnoreCase)
                 || successRaw == "1"
                 || successRaw.Equals("true", StringComparison.OrdinalIgnoreCase)
                 || successRaw.Equals("paid", StringComparison.OrdinalIgnoreCase);

        if (!ok)
            return new RechargeNotifyParseResult
            {
                Success = false,
                FailMessage = Pick("message", "msg") is { Length: > 0 } m ? m : "回调失败"
            };

        decimal.TryParse(Pick(moneyField, "money", "amount", "Paid"), out var money);
        return new RechargeNotifyParseResult
        {
            Success = true,
            OrderId = Pick(orderField, "orderId", "OrderID", "ID"),
            SystemId = Pick(tradeField, "systemId", "SystemID", "tradeNo"),
            Money = money,
            PayAccount = Pick("payAccount", "PayAccount", "account")
        };
    }

    private static Dictionary<string, string> ParseForm(string rawBody)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var text = rawBody.Trim();
        if (text.StartsWith('?'))
            text = text[1..];
        foreach (var part in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2)
                continue;
            map[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1]);
        }
        return map;
    }
}

/// <summary>
/// 按通道 Setting.Plugin 选充值回调插件；默认 mock。
/// </summary>
public sealed class RechargeNotifyPluginRouter : ITransient
{
    private readonly IEnumerable<IRechargeNotifyPlugin> _plugins;
    private readonly MockFormRechargeNotifyPlugin _mock;

    /// <summary>
    /// 注入全部插件与默认 mock。
    /// </summary>
    /// <param name="plugins">插件</param>
    /// <param name="mock">默认 mock</param>
    public RechargeNotifyPluginRouter(IEnumerable<IRechargeNotifyPlugin> plugins, MockFormRechargeNotifyPlugin mock)
    {
        _plugins = plugins ?? Enumerable.Empty<IRechargeNotifyPlugin>();
        _mock = mock;
    }

    /// <summary>
    /// 解析并验签。
    /// </summary>
    /// <param name="rawBody">正文</param>
    /// <param name="payment">通道</param>
    /// <returns>结果</returns>
    public RechargeNotifyParseResult ParseAndVerify(string rawBody, SitePayment payment)
    {
        var named = RechargeNotifyPluginSettings.ReadPlugin(payment?.SettingString);
        if (string.Equals(named, "http-md5", StringComparison.OrdinalIgnoreCase))
        {
            var http = _plugins.OfType<HttpMd5RechargeNotifyPlugin>().FirstOrDefault();
            if (http == null || !http.CanHandle(payment))
                return new RechargeNotifyParseResult { Success = false, FailMessage = "通道未配置 http-md5 Key" };
            return http.ParseAndVerify(rawBody, payment);
        }

        var hit = _plugins.FirstOrDefault(p => p.CanHandle(payment));
        return (hit ?? _mock).ParseAndVerify(rawBody, payment);
    }
}

/// <summary>
/// 充值回调安全：IP 白名单与 Mock MD5 验签。
/// </summary>
public static class RechargeNotifySecurity
{
    /// <summary>
    /// 读通道分组白名单。名单空表示不限制（对齐旧 GetCallbackIP 空列表）。
    /// </summary>
    /// <param name="db">业务库</param>
    /// <param name="payment">站点通道</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>白名单（已 Trim）</returns>
    public static async Task<List<string>> LoadCallbackIpsAsync(
        ISqlSugarClient db,
        SitePayment payment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(payment);
        var list = new List<string>();
        if (payment.SysID <= 0)
            return list;

        var sysRows = await db.Queryable<SystemPayment>()
            .Where(s => s.ID == payment.SysID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var sys = sysRows.FirstOrDefault();
        if (sys == null || sys.GroupID <= 0)
            return list;

        var groups = await db.Queryable<SystemPaymentGroup>()
            .Where(g => g.ID == sys.GroupID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var raw = groups.FirstOrDefault()?.CallbackIP;
        if (string.IsNullOrWhiteSpace(raw))
            return list;

        foreach (var part in raw.Split([',', '|'], StringSplitOptions.RemoveEmptyEntries))
        {
            var ip = part.Trim();
            if (ip.Length > 0)
                list.Add(ip);
        }
        return list;
    }

    /// <summary>
    /// 白名单非空时，客户端 IP 必须命中；空名单直接过。
    /// </summary>
    /// <param name="allowList">白名单</param>
    /// <param name="clientIp">客户端 IP</param>
    /// <returns>是否允许</returns>
    public static bool IsIpAllowed(IReadOnlyList<string> allowList, string clientIp)
    {
        if (allowList == null || allowList.Count == 0)
            return true;
        var ip = (clientIp ?? string.Empty).Trim();
        if (ip.Length == 0 || ip == "::1")
            ip = "127.0.0.1";
        return allowList.Any(a => string.Equals(a, ip, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 从通道 Setting 读 NotifyKey（query 或 JSON）。
    /// </summary>
    /// <param name="settingRaw">Setting 原文</param>
    /// <returns>密钥；没有则空</returns>
    public static string ReadNotifyKey(string settingRaw)
    {
        if (string.IsNullOrWhiteSpace(settingRaw))
            return string.Empty;

        var trimmed = settingRaw.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(settingRaw);
                if (doc.RootElement.TryGetProperty("NotifyKey", out var p) ||
                    doc.RootElement.TryGetProperty("notifyKey", out p) ||
                    doc.RootElement.TryGetProperty("Key", out p))
                {
                    return p.ValueKind == JsonValueKind.String
                        ? (p.GetString() ?? string.Empty)
                        : p.ToString();
                }
            }
            catch (JsonException)
            {
                // 再走 query
            }
        }

        var map = SiteSettingQuery.Parse(settingRaw);
        var key = SiteSettingQuery.Get(map, "NotifyKey");
        if (string.IsNullOrWhiteSpace(key))
            key = SiteSettingQuery.Get(map, "Key");
        return key ?? string.Empty;
    }

    /// <summary>
    /// 校验 Mock MD5：字段排序后拼 &amp;key=，小写 hex。无密钥则跳过。
    /// </summary>
    /// <param name="fields">表单字段（含 sign）</param>
    /// <param name="notifyKey">密钥</param>
    /// <returns>是否通过</returns>
    public static bool VerifyMockMd5(IDictionary<string, string> fields, string notifyKey)
    {
        if (string.IsNullOrWhiteSpace(notifyKey))
            return true;
        if (fields == null)
            return false;
        string got = null;
        foreach (var k in new[] { "sign", "Sign" })
        {
            if (fields.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
            {
                got = v;
                break;
            }
        }
        if (string.IsNullOrWhiteSpace(got))
            return false;

        var expected = ComputeMockMd5(fields, notifyKey);
        return string.Equals(expected, got.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 计算 Mock 签名（测试与网关共用）。
    /// </summary>
    /// <param name="fields">字段</param>
    /// <param name="notifyKey">密钥</param>
    /// <returns>小写 hex MD5</returns>
    public static string ComputeMockMd5(IDictionary<string, string> fields, string notifyKey)
    {
        var sorted = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (fields != null)
        {
            foreach (var kv in fields)
            {
                if (string.Equals(kv.Key, "sign", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.IsNullOrEmpty(kv.Value))
                    continue;
                sorted[kv.Key] = kv.Value;
            }
        }

        var sb = new StringBuilder();
        foreach (var kv in sorted)
        {
            if (sb.Length > 0)
                sb.Append('&');
            sb.Append(kv.Key).Append('=').Append(kv.Value);
        }
        sb.Append('&').Append("key=").Append(notifyKey ?? string.Empty);
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// 从 HTTP 头取客户端 IP（简化旧 GetIP：先 XFF 首段，再 X-Real-IP，再直连）。
    /// </summary>
    /// <param name="xff">X-Forwarded-For</param>
    /// <param name="xRealIp">X-Real-IP</param>
    /// <param name="remoteIp">直连 IP</param>
    /// <returns>IP 字符串</returns>
    public static string ResolveClientIp(string xff, string xRealIp, string remoteIp)
    {
        if (!string.IsNullOrWhiteSpace(xff))
        {
            var first = xff.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            if (first.Length > 0)
                return first;
        }
        if (!string.IsNullOrWhiteSpace(xRealIp))
            return xRealIp.Trim();
        var ip = (remoteIp ?? string.Empty).Trim();
        if (ip == "::1")
            return "127.0.0.1";
        return string.IsNullOrEmpty(ip) ? "0.0.0.0" : ip;
    }
}

/// <summary>
/// Mock 回调表单解析（原 RechargeNotifyService 静态方法，下沉到插件侧避免反向依赖）。
/// </summary>
public static class RechargeNotifyMockBody
{
    /// <summary>
    /// 解析 Mock 表单（供 mock 插件与单测）。
    /// </summary>
    /// <param name="rawBody">正文</param>
    /// <returns>解析结果</returns>
    public static MockNotifyParseResult Parse(string rawBody)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var text = (rawBody ?? string.Empty).Trim();
        if (text.StartsWith('?'))
            text = text[1..];
        foreach (var part in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2)
                continue;
            map[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1]);
        }

        var success = true;
        if (map.TryGetValue("success", out var s))
            success = s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";

        string Pick(params string[] keys)
        {
            foreach (var k in keys)
            {
                if (map.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v;
            }
            return string.Empty;
        }

        var moneyText = Pick("money", "Paid", "paid", "amount");
        decimal.TryParse(moneyText, out var money);

        if (!success)
        {
            return new MockNotifyParseResult
            {
                Success = false,
                FailMessage = Pick("message", "msg") is { Length: > 0 } m ? m : "回调失败",
                Fields = map
            };
        }

        return new MockNotifyParseResult
        {
            Success = true,
            OrderId = Pick("orderId", "OrderID", "ID", "id"),
            SystemId = Pick("systemId", "SystemID", "tradeNo", "ChannelTradeNo"),
            Money = money,
            PayAccount = Pick("payAccount", "PayAccount", "account"),
            Fields = map
        };
    }
}

/// <summary>
/// Mock 回调解析结果。
/// </summary>
public sealed class MockNotifyParseResult
{
    /// <summary>网关是否判成功。</summary>
    public bool Success { get; set; }
    /// <summary>失败说明。</summary>
    public string FailMessage { get; set; }
    /// <summary>订单号。</summary>
    public string OrderId { get; set; }
    /// <summary>网关单号。</summary>
    public string SystemId { get; set; }
    /// <summary>金额。</summary>
    public decimal Money { get; set; }
    /// <summary>付款账户。</summary>
    public string PayAccount { get; set; }
    /// <summary>全部字段（验签用）。</summary>
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

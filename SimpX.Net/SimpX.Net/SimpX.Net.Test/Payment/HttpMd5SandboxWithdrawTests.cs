using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SimpX.Net.Application.Shared;
using SimpX.Net.Feature.Http;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// http-md5 插件打本地沙箱语义（FakeHttpCaller，不打真实商户）。
/// </summary>
public sealed class HttpMd5SandboxWithdrawTests
{
    /// <summary>提交：签名正确 → Paymenting + system_id。</summary>
    [Fact]
    public async Task Submit_to_sandbox_handler_accepts()
    {
        var plugin = new HttpMd5FormWithdrawGatewayPlugin(new FakeHttpCaller(SandboxRespond));
        var setting = JsonSerializer.Serialize(new
        {
            Plugin = "http-md5",
            Gateway = "http://sandbox.local/submit",
            QueryGateway = "http://sandbox.local/query",
            MerchantId = "M1",
            Key = HttpMd5GatewayConfig.SandboxKey,
            NotifyUrl = "http://localhost/cb"
        });

        var r = await plugin.SubmitAsync(new WithdrawGatewayRequest
        {
            OrderID = 42,
            Money = 3.50m,
            AccountName = "张三",
            AccountNumber = "6222",
            Bank = 1,
            SysID = 9,
            ChannelSetting = setting
        });

        Assert.Equal(WithdrawGatewayStatus.Paymenting, r.Status);
        Assert.False(string.IsNullOrWhiteSpace(r.SystemID));
        Assert.StartsWith("MW", r.SystemID);
    }

    /// <summary>错误 Key → 沙箱拒签 → Failed。</summary>
    [Fact]
    public async Task Submit_bad_key_fails()
    {
        var plugin = new HttpMd5FormWithdrawGatewayPlugin(new FakeHttpCaller(SandboxRespond));
        var setting = """{"Plugin":"http-md5","Gateway":"http://sandbox.local/submit","MerchantId":"M1","Key":"wrong","NotifyUrl":"http://x"}""";

        var r = await plugin.SubmitAsync(new WithdrawGatewayRequest
        {
            OrderID = 43,
            Money = 1m,
            ChannelSetting = setting
        });
        Assert.Equal(WithdrawGatewayStatus.Failed, r.Status);
    }

    /// <summary>按 form 正文验签并回沙箱 JSON。</summary>
    private static HttpCallResponse SandboxRespond(HttpCallRequest request)
    {
        var body = request.FormUrlEncodedContent ?? string.Empty;
        var map = body.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(
                p => Uri.UnescapeDataString(p[0]),
                p => Uri.UnescapeDataString(p[1]),
                StringComparer.OrdinalIgnoreCase);

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in map)
        {
            if (string.Equals(kv.Key, "sign", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrEmpty(kv.Value))
                fields[kv.Key] = kv.Value;
        }

        var expect = Md5(fields, HttpMd5GatewayConfig.SandboxKey);
        if (!map.TryGetValue("sign", out var sign) ||
            !string.Equals(sign, expect, StringComparison.OrdinalIgnoreCase))
        {
            return new HttpCallResponse
            {
                StatusCode = HttpStatusCode.OK,
                Body = "{\"status\":\"fail\",\"message\":\"bad sign\"}"
            };
        }

        var trade = $"MW{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
        return new HttpCallResponse
        {
            StatusCode = HttpStatusCode.OK,
            Body = $"{{\"status\":\"success\",\"system_id\":\"{trade}\",\"message\":\"ok\"}}"
        };
    }

    private static string Md5(SortedDictionary<string, string> fields, string key)
    {
        var sb = new StringBuilder();
        foreach (var kv in fields)
        {
            if (sb.Length > 0)
                sb.Append('&');
            sb.Append(kv.Key).Append('=').Append(kv.Value);
        }

        sb.Append('&').Append("key=").Append(key);
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString()))).ToLowerInvariant();
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace SimpX.Net.Application.Channels;

/// <summary>
/// 本地代付 MD5 沙箱网关。只给 http-md5 插件联调，不接真实商户。
/// 路径：POST /api/pay/mock-withdraw/submit|query ，表单字段与 HttpMd5FormWithdrawGatewayPlugin 一致。
/// </summary>
[ApiDescriptionSettings(Module = "pay", Name = "mock-withdraw", Order = 120)]
public class MockMd5WithdrawGatewayAppService : IDynamicApiController, ITransient
{
    /// <summary>沙箱固定密钥（与 <see cref="HttpMd5GatewayConfig.SandboxKey"/> 对齐）。</summary>
    public const string SandboxKey = HttpMd5GatewayConfig.SandboxKey;

    /// <summary>
    /// 模拟代付提交。校验 sign 后返回 system_id。
    /// </summary>
    [AllowAnonymous]
    [HttpPost("submit")]
    [DisplayName("沙箱代付提交")]
    public MockWithdrawGatewayOutput Submit([FromForm] MockWithdrawGatewayForm form)
    {
        ValidateSign(form);
        var trade = $"MW{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
        return new MockWithdrawGatewayOutput
        {
            Status = "success",
            SystemId = trade,
            Message = "ok"
        };
    }

    /// <summary>
    /// 模拟代付查单。
    /// </summary>
    [AllowAnonymous]
    [HttpPost("query")]
    [DisplayName("沙箱代付查单")]
    public MockWithdrawGatewayOutput Query([FromForm] MockWithdrawGatewayForm form)
    {
        ValidateSign(form);
        return new MockWithdrawGatewayOutput
        {
            Status = "success",
            SystemId = string.IsNullOrWhiteSpace(form.SystemId) ? $"MQ{form.OrderNo}" : form.SystemId,
            Message = "paymenting"
        };
    }

    /// <summary>
    /// 校验表单 MD5（字段排序拼接 + key=）。
    /// </summary>
    private static void ValidateSign(MockWithdrawGatewayForm form)
    {
        if (form == null)
            throw Oops.Oh("表单为空");
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
        void Put(string k, string v)
        {
            if (!string.IsNullOrEmpty(v))
                fields[k] = v;
        }

        Put("merchant_id", form.MerchantId);
        Put("order_no", form.OrderNo);
        Put("amount", form.Amount);
        Put("account_name", form.AccountName);
        Put("account_number", form.AccountNumber);
        Put("bank", form.Bank);
        Put("notify_url", form.NotifyUrl);
        Put("system_id", form.SystemId);

        var expect = Md5Sign(fields, SandboxKey);
        if (!string.Equals(expect, form.Sign, StringComparison.OrdinalIgnoreCase))
            throw Oops.Oh("sign 校验失败");
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

        sb.Append('&').Append("key=").Append(key);
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>沙箱代付表单（与插件字段名对齐）。</summary>
public sealed class MockWithdrawGatewayForm
{
    /// <summary>商户号。</summary>
    [FromForm(Name = "merchant_id")]
    public string MerchantId { get; set; }

    /// <summary>订单号。</summary>
    [FromForm(Name = "order_no")]
    public string OrderNo { get; set; }

    /// <summary>金额。</summary>
    [FromForm(Name = "amount")]
    public string Amount { get; set; }

    /// <summary>户名。</summary>
    [FromForm(Name = "account_name")]
    public string AccountName { get; set; }

    /// <summary>账号。</summary>
    [FromForm(Name = "account_number")]
    public string AccountNumber { get; set; }

    /// <summary>银行。</summary>
    [FromForm(Name = "bank")]
    public string Bank { get; set; }

    /// <summary>通知地址。</summary>
    [FromForm(Name = "notify_url")]
    public string NotifyUrl { get; set; }

    /// <summary>供应商单号（查单）。</summary>
    [FromForm(Name = "system_id")]
    public string SystemId { get; set; }

    /// <summary>签名。</summary>
    [FromForm(Name = "sign")]
    public string Sign { get; set; }
}

/// <summary>沙箱代付返回。</summary>
public sealed class MockWithdrawGatewayOutput
{
    /// <summary>状态。</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>供应商单号。</summary>
    [JsonPropertyName("system_id")]
    public string SystemId { get; set; }

    /// <summary>说明。</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }
}

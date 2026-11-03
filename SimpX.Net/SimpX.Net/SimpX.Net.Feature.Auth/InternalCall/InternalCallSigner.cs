using System.Security.Cryptography;

namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 生成内部调用签名头。算法和 SignatureAuthenticationHandler 的校验保持一致：
/// HMACSHA256(secret, "METHOD&amp;path&amp;accessKey&amp;timestamp&amp;nonce")，Base64 输出。
/// </summary>
public static class InternalCallSigner
{
    /// <summary>
    /// 为一次请求生成签名相关的请求头。
    /// </summary>
    /// <param name="method">HTTP 方法（大写）</param>
    /// <param name="path">请求路径，以 / 开头，不含查询串</param>
    /// <param name="accessKey">调用方标识</param>
    /// <param name="accessSecret">签名密钥</param>
    /// <returns>请求头键值</returns>
    public static Dictionary<string, string> CreateHeaders(string method, string path, string accessKey, string accessSecret)
    {
        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(accessSecret))
            throw Oops.Oh("内部调用未配置 InternalCall:AccessKey / AccessSecret");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");
        var message = $"{method.ToUpperInvariant()}&{path}&{accessKey}&{timestamp}&{nonce}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(accessSecret));
        var sign = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));

        return new Dictionary<string, string>
        {
            ["accessKey"] = accessKey,
            ["timestamp"] = timestamp,
            ["nonce"] = nonce,
            ["sign"] = sign
        };
    }
}

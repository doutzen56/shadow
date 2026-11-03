using System.Security.Cryptography;
using System.Text;
using SimpX.Net.Feature.Auth;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 内部签名头生成与验算算法一致性（F5.3）。
/// </summary>
public class InternalCallSignerTests
{
    /// <summary>
    /// 方案名固定为 InternalSignature，给 Channels / Schedule 的 Authorize 用。
    /// </summary>
    [Fact]
    public void AuthenticationScheme_IsInternalSignature()
    {
        Assert.Equal("InternalSignature", InternalCallDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// CreateHeaders 产出四个头，且 sign 与 Handler 同一套 HMAC 公式一致。
    /// </summary>
    [Fact]
    public void CreateHeaders_SignMatchesHmacSha256Formula()
    {
        const string method = "POST";
        const string path = "/api/internal/job/page";
        const string accessKey = "test-ak";
        const string accessSecret = "test-secret";

        var headers = InternalCallSigner.CreateHeaders(method, path, accessKey, accessSecret);

        Assert.Equal(accessKey, headers["accessKey"]);
        Assert.True(headers.ContainsKey("timestamp"));
        Assert.True(headers.ContainsKey("nonce"));
        Assert.True(headers.ContainsKey("sign"));

        var message = $"{method}&{path}&{accessKey}&{headers["timestamp"]}&{headers["nonce"]}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(accessSecret));
        var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
        Assert.Equal(expected, headers["sign"]);
    }

    /// <summary>
    /// 未配置密钥时直接失败，避免空签出去。
    /// </summary>
    [Fact]
    public void CreateHeaders_EmptySecret_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            InternalCallSigner.CreateHeaders("GET", "/api/x", "ak", string.Empty));
    }
}

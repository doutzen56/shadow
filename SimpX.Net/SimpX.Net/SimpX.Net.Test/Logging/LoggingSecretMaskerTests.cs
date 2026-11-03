using SimpX.Net.Feature.Logging;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 日志正文令牌脱敏（F5.5）。
/// </summary>
public class LoggingSecretMaskerTests
{
    /// <summary>
    /// Authorization Bearer 令牌打成 ****。
    /// </summary>
    [Fact]
    public void Mask_AuthorizationBearer_ReplacesToken()
    {
        var raw = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.abc.def";
        var masked = LoggingSecretMasker.Mask(raw);
        Assert.Contains("Bearer ****", masked);
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", masked);
    }

    /// <summary>
    /// 单独出现的 Bearer 令牌同样打码。
    /// </summary>
    [Fact]
    public void Mask_BareBearer_ReplacesToken()
    {
        var raw = "授权信息：Bearer abcdef.ghijklmnop";
        var masked = LoggingSecretMasker.Mask(raw);
        Assert.Contains("Bearer ****", masked);
        Assert.DoesNotContain("abcdef.ghijklmnop", masked);
    }

    /// <summary>
    /// access-token / refresh-token 字段值打码。
    /// </summary>
    [Fact]
    public void Mask_AccessAndRefreshToken_ReplacesValues()
    {
        var raw = "access-token: tok_AAA111BBB222; refresh-token=tok_CCC333DDD444";
        var masked = LoggingSecretMasker.Mask(raw);
        Assert.Contains("****", masked);
        Assert.DoesNotContain("tok_AAA111BBB222", masked);
        Assert.DoesNotContain("tok_CCC333DDD444", masked);
    }

    /// <summary>
    /// 空串原样返回。
    /// </summary>
    [Fact]
    public void Mask_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, LoggingSecretMasker.Mask(string.Empty));
        Assert.Null(LoggingSecretMasker.Mask(null!));
    }
}

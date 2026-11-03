using System.Text.RegularExpressions;

namespace SimpX.Net.Feature.Logging;

/// <summary>
/// 日志脱敏：把日志正文里的 Bearer 令牌、Authorization 头、access-token 头打成 ****，
/// 避免控制台、文件、数据库三处日志把登录态原样落盘。
/// </summary>
public static partial class LoggingSecretMasker
{
    /// <summary>
    /// 匹配 Authorization: Bearer 令牌（含 JSON 里的键值写法）。
    /// </summary>
    [GeneratedRegex(@"(?i)(Authorization[""'\s:=]*)(Bearer\s+)([A-Za-z0-9\-._~+/]+=*)", RegexOptions.CultureInvariant)]
    private static partial Regex AuthorizationBearerRegex();

    /// <summary>
    /// 匹配正文里单独出现的 Bearer 令牌（Furion 监控日志的“授权信息”段常用这种写法）。
    /// </summary>
    [GeneratedRegex(@"(?i)\bBearer\s+([A-Za-z0-9\-._~+/]+=*)", RegexOptions.CultureInvariant)]
    private static partial Regex BareBearerRegex();

    /// <summary>
    /// 匹配 access-token / x-access-token / refresh-token 这类头或字段的值。
    /// </summary>
    [GeneratedRegex(@"(?i)((?:x-)?(?:access|refresh)[-_]?token[""'\s:=]+)([A-Za-z0-9\-._~+/]+=*)", RegexOptions.CultureInvariant)]
    private static partial Regex AccessTokenRegex();

    /// <summary>
    /// 对一段日志正文脱敏。空串原样返回。
    /// </summary>
    /// <param name="text">原始日志正文</param>
    /// <returns>脱敏后的正文</returns>
    public static string Mask(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        text = AuthorizationBearerRegex().Replace(text, "$1$2****");
        text = BareBearerRegex().Replace(text, "Bearer ****");
        text = AccessTokenRegex().Replace(text, "$1****");
        return text;
    }
}

using System.Security.Cryptography;
using System.Text;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 谷歌验证码校验（对齐 SP.Studio GoogleAuthenticator：密钥按 UTF-8 字节做 HMAC-SHA1 TOTP，默认漂移 ±5 分钟）。
/// </summary>
public static class GoogleTotpValidator
{
    private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 校验 6 位动态码是否在容差窗口内有效。
    /// </summary>
    /// <param name="accountSecretKey">密钥明文（旧版用 Guid.ToString("N")）</param>
    /// <param name="code">客户端提交的验证码</param>
    /// <param name="clockDrift">时间漂移容差；默认 5 分钟</param>
    /// <returns>是否通过</returns>
    public static bool Validate(string accountSecretKey, string code, TimeSpan? clockDrift = null)
    {
        if (string.IsNullOrWhiteSpace(accountSecretKey) || string.IsNullOrWhiteSpace(code))
            return false;

        var tolerance = clockDrift ?? TimeSpan.FromMinutes(5);
        var codes = GetCurrentPins(accountSecretKey, tolerance);
        return codes.Any(c => c == code.Trim());
    }

    /// <summary>
    /// 生成当前 30 秒窗口的 PIN（单测与排障用）。
    /// </summary>
    /// <param name="accountSecretKey">密钥明文</param>
    /// <returns>6 位码；密钥空则空串</returns>
    public static string GenerateCurrentPin(string accountSecretKey)
    {
        if (string.IsNullOrWhiteSpace(accountSecretKey))
            return string.Empty;
        return GeneratePin(accountSecretKey, GetCurrentCounter(DateTime.UtcNow));
    }

    /// <summary>
    /// 生成当前时间窗附近的 PIN 列表。
    /// </summary>
    private static IEnumerable<string> GetCurrentPins(string secret, TimeSpan tolerance)
    {
        var counter = GetCurrentCounter(DateTime.UtcNow);
        var offset = tolerance.TotalSeconds > 30
            ? Convert.ToInt32(tolerance.TotalSeconds / 30.0)
            : 0;

        for (var i = counter - offset; i <= counter + offset; i++)
            yield return GeneratePin(secret, i);
    }

    private static long GetCurrentCounter(DateTime utcNow) =>
        (long)(utcNow - Epoch).TotalSeconds / 30;

    private static string GeneratePin(string secret, long iteration, int digits = 6)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var counter = BitConverter.GetBytes(iteration);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counter);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0xf;
        var binary =
            ((hash[offset] & 0x7f) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];
        var password = binary % (int)Math.Pow(10, digits);
        return password.ToString(new string('0', digits));
    }
}

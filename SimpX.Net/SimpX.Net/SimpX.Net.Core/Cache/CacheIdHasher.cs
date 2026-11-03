using System.Security.Cryptography;

namespace SimpX.Net.Core;

/// <summary>
/// 把令牌、手机号收成缓存标识，避免原文出现在缓存键里。
/// </summary>
public static class CacheIdHasher
{
    /// <summary>
    /// 用 SHA256 算出十六进制字符串，当作缓存标识。
    /// </summary>
    /// <param name="value">原文；空字符串也会算出固定结果</param>
    /// <returns>大写十六进制</returns>
    public static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return Convert.ToHexString(bytes);
    }
}

using System.Security.Cryptography;
using System.Text;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// Users 表密码：UTF-8 做 MD5，再转成大写十六进制（跟旧版 MD5.toMD5 一样）。
/// </summary>
public static class Md5PasswordHasher
{
    /// <summary>
    /// 计算明文密码的存库哈希。
    /// </summary>
    /// <param name="plainPassword">明文密码</param>
    /// <returns>32 位大写十六进制；空输入返回空串</returns>
    public static string Hash(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            return string.Empty;

        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(plainPassword));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString().ToUpperInvariant();
    }

    /// <summary>
    /// 明文是否与库中哈希一致（库值可能大小写混用）。
    /// </summary>
    /// <param name="plainPassword">明文</param>
    /// <param name="storedHash">库中 Password</param>
    /// <returns>是否匹配</returns>
    public static bool Matches(string plainPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        var hash = Hash(plainPassword);
        return hash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
    }
}

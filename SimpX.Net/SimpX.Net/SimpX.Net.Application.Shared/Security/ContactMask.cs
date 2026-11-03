namespace SimpX.Net.Application.Shared;

/// <summary>
/// 联系方式脱敏（姓名 / 邮箱 / 手机），给会员接口和后台列表共用。
/// </summary>
public static class ContactMask
{
    /// <summary>
    /// 姓名脱敏：保留首尾，中间打星。
    /// </summary>
    /// <param name="value">原始姓名</param>
    /// <returns>脱敏后的姓名</returns>
    public static string MaskName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (value.Length <= 1)
            return "*";
        // 旧站两位姓名：首字 + **（如 rr → r**）
        if (value.Length == 2)
            return value[0] + "**";
        return value[0] + new string('*', value.Length - 2) + value[^1];
    }

    /// <summary>
    /// 邮箱脱敏：本地部分中间打星，域名保留。
    /// </summary>
    /// <param name="value">原始邮箱</param>
    /// <returns>脱敏后的邮箱</returns>
    public static string MaskEmail(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('@'))
            return value ?? string.Empty;
        var at = value.IndexOf('@');
        var name = value[..at];
        var domain = value[(at + 1)..];
        var masked = name.Length <= 2 ? "**" : name[..1] + "**" + name[^1];
        return masked + "@" + domain;
    }

    /// <summary>
    /// 手机脱敏：保留前三后四；兼容旧版「区号,手机号」。
    /// </summary>
    /// <param name="value">原始手机</param>
    /// <returns>脱敏后的手机</returns>
    public static string MaskMobile(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        var parts = value.Split(',');
        var mobile = parts.Length > 1 ? parts[^1] : value;
        if (mobile.Length < 7)
            return "****";
        return mobile[..3] + "****" + mobile[^4..];
    }
}

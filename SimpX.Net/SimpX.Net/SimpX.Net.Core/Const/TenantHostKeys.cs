namespace SimpX.Net.Core;

/// <summary>
/// 按域名确定租户后写入 HttpContext.Items 时用的键。
/// </summary>
public static class TenantHostKeys
{
    /// <summary>
    /// 已解析的租户 Id（long）。
    /// </summary>
    public const string ResolvedTenantId = "SimpX.ResolvedTenantId";

    /// <summary>
    /// 解析所用的规范化 Host。
    /// </summary>
    public const string ResolvedHost = "SimpX.ResolvedHost";
}

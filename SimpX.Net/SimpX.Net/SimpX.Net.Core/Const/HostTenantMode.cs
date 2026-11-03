namespace SimpX.Net.Core;

/// <summary>
/// 当前进程「是否按域名确定租户」的快照。由宿主启动入口写入，应用层登录/租户接口可读，避免 Application 再引用 Hosting。
/// </summary>
public static class HostTenantMode
{
    /// <summary>
    /// 是否按请求域名解析租户（Admin、WebApi 等为 true；总后台与 Schedule/Channels 为 false）。
    /// </summary>
    public static bool ResolveByHost { get; set; }

    /// <summary>
    /// 是否为总后台进程（不按域名确定租户，登录只接受总后台账号）。
    /// </summary>
    public static bool IsPlatformHost { get; set; }
}

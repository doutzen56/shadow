namespace SimpX.Net.Hosting;

/// <summary>
/// 宿主可选能力开关。由 <see cref="HostLauncher"/> 在启动前定好，公共服务和请求处理链按此注册，避免四宿主背同一套税。
/// </summary>
public sealed class HostCapabilityOptions
{
    /// <summary>验证码（Lazy.Captcha）。</summary>
    public bool Captcha { get; set; }

    /// <summary>ImageSharp 图片处理中间件（需要 wwwroot）。</summary>
    public bool ImageSharp { get; set; }

    /// <summary>Furion 视图引擎。</summary>
    public bool ViewEngine { get; set; }

    /// <summary>SignalR（含 MapHubs）。</summary>
    public bool SignalR { get; set; }

    /// <summary>本地文件存储抽象注册。</summary>
    public bool FileStorage { get; set; }

    /// <summary>启用 IP 归属地查询（IPTools）。Worker 默认关，避免无谓初始化。</summary>
    public bool IpLocation { get; set; }

    /// <summary>IP 归属地库整库载入内存（约多占 60～70MB）；需先开 IpLocation。</summary>
    public bool IpLocationInMemory { get; set; }

    /// <summary>多语言（MVC AddAppLocalization + UseAppLocalization）。</summary>
    public bool Localization { get; set; }

    /// <summary>Furion 虚拟文件服务（静态类型映射等）。</summary>
    public bool VirtualFileServer { get; set; }

    /// <summary>控制台启动 Logo。</summary>
    public bool ConsoleLogo { get; set; } = true;

    /// <summary>
    /// 是否按请求域名解析租户（Admin、WebApi 等开；总后台与 Schedule/Channels 关）。
    /// </summary>
    public bool ResolveTenantByHost { get; set; }

    /// <summary>
    /// 域名解析不到租户时是否直接失败（与 ResolveTenantByHost 一起用于租户站）。
    /// </summary>
    public bool FailIfTenantHostUnresolved { get; set; }

    /// <summary>
    /// 是否按请求域名解析站点 SiteID（WebApi、Admin 开；总后台与 Schedule 关）。
    /// 跟按域名解析租户的开关分开，禁止把 SiteID 当成 TenantId。
    /// </summary>
    public bool ResolveSiteByHost { get; set; }

    /// <summary>
    /// 域名解析不到站点时是否直接失败（跟 ResolveSiteByHost 一起，给必须认站点的进程用）。
    /// </summary>
    public bool FailIfSiteHostUnresolved { get; set; }

    /// <summary>
    /// 是否为总后台进程（登录只接受总后台账号，不按域名确定租户）。
    /// </summary>
    public bool IsPlatformHost { get; set; }
}

/// <summary>
/// 当前宿主能力快照，以及各宿主预设（与 knowledge 里的 Feature 开关表一致）。
/// </summary>
public static class HostCapabilities
{
    /// <summary>
    /// 当前进程生效的能力；<see cref="HostLauncher"/> 启动前写入。
    /// </summary>
    public static HostCapabilityOptions Current { get; internal set; } = new();

    /// <summary>
    /// Admin：后台需要的可选能力默认全开。
    /// </summary>
    /// <returns>Admin 预设</returns>
    public static HostCapabilityOptions Admin() => new()
    {
        Captcha = true,
        ImageSharp = true,
        ViewEngine = true,
        SignalR = true,
        FileStorage = true,
        IpLocation = true,
        IpLocationInMemory = true,
        Localization = true,
        VirtualFileServer = true,
        ConsoleLogo = true,
        ResolveTenantByHost = true,
        FailIfTenantHostUnresolved = true,
        ResolveSiteByHost = true,
        // 与 AUTH 矩阵一致：认不到站点直接失败。本地 Sqlite 无 site_Host 时，由 Host.Admin Development 软关。
        FailIfSiteHostUnresolved = true,
        IsPlatformHost = false
    };

    /// <summary>
    /// 总后台 SuperAdmin：能力与 Admin 相近，但不按域名确定租户、也不按域名确定站点（跨站靠参数 SiteID）。
    /// </summary>
    /// <returns>SuperAdmin 预设</returns>
    public static HostCapabilityOptions SuperAdmin() => new()
    {
        Captcha = true,
        ImageSharp = true,
        ViewEngine = true,
        SignalR = true,
        FileStorage = true,
        IpLocation = true,
        IpLocationInMemory = true,
        Localization = true,
        VirtualFileServer = true,
        ConsoleLogo = true,
        ResolveTenantByHost = false,
        FailIfTenantHostUnresolved = false,
        ResolveSiteByHost = false,
        FailIfSiteHostUnresolved = false,
        IsPlatformHost = true
    };

    /// <summary>
    /// WebApi：可选能力默认关；对外请求按域名确定租户，并按域名确定站点。
    /// </summary>
    /// <returns>WebApi 预设</returns>
    public static HostCapabilityOptions WebApi() => new()
    {
        Captcha = false,
        ImageSharp = false,
        ViewEngine = false,
        SignalR = false,
        FileStorage = false,
        IpLocation = false,
        IpLocationInMemory = false,
        Localization = false,
        VirtualFileServer = false,
        ConsoleLogo = true,
        ResolveTenantByHost = true,
        FailIfTenantHostUnresolved = true,
        ResolveSiteByHost = true,
        FailIfSiteHostUnresolved = true,
        IsPlatformHost = false
    };

    /// <summary>
    /// Schedule 等内部进程：不挂验证码、图片、SignalR、IP 内存库等，不按域名确定租户，也不按域名确定站点。
    /// </summary>
    /// <returns>内部进程预设</returns>
    public static HostCapabilityOptions Worker() => new()
    {
        Captcha = false,
        ImageSharp = false,
        ViewEngine = false,
        SignalR = false,
        FileStorage = false,
        IpLocation = false,
        IpLocationInMemory = false,
        Localization = false,
        VirtualFileServer = false,
        ConsoleLogo = true,
        ResolveTenantByHost = false,
        FailIfTenantHostUnresolved = false,
        ResolveSiteByHost = false,
        FailIfSiteHostUnresolved = false,
        IsPlatformHost = false
    };

    /// <summary>
    /// Channels（支付等）：能力同 Worker，但按域名确定租户；支付挂在具体租户下。先不按域名确定站点，站信息走业务参数。
    /// </summary>
    /// <returns>Channels 预设</returns>
    public static HostCapabilityOptions Channels() => new()
    {
        Captcha = false,
        ImageSharp = false,
        ViewEngine = false,
        SignalR = false,
        FileStorage = false,
        IpLocation = false,
        IpLocationInMemory = false,
        Localization = false,
        VirtualFileServer = false,
        ConsoleLogo = true,
        ResolveTenantByHost = true,
        FailIfTenantHostUnresolved = true,
        ResolveSiteByHost = false,
        FailIfSiteHostUnresolved = false,
        IsPlatformHost = false
    };
}

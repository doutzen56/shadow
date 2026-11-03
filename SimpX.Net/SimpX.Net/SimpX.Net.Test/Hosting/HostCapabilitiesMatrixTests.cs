using SimpX.Net.Hosting;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 各宿主可选能力预设对照表（F5.2）：跟 Program 里选用的 HostCapabilities.* 一致。
/// </summary>
public class HostCapabilitiesMatrixTests
{
    /// <summary>
    /// Admin：后台常用可选能力全开，并按域名定租+定站。
    /// </summary>
    [Fact]
    public void Admin_OptionalFeatures_AllOn_WithTenantAndSite()
    {
        var caps = HostCapabilities.Admin();
        AssertOptionalOn(caps);
        Assert.True(caps.ResolveTenantByHost);
        Assert.True(caps.FailIfTenantHostUnresolved);
        Assert.True(caps.ResolveSiteByHost);
        Assert.True(caps.FailIfSiteHostUnresolved);
        Assert.False(caps.IsPlatformHost);
    }

    /// <summary>
    /// SuperAdmin：可选能力与 Admin 相近，不定租不定站，平台宿主。
    /// </summary>
    [Fact]
    public void SuperAdmin_OptionalFeatures_On_NoTenantNoSite_PlatformHost()
    {
        var caps = HostCapabilities.SuperAdmin();
        AssertOptionalOn(caps);
        Assert.False(caps.ResolveTenantByHost);
        Assert.False(caps.FailIfTenantHostUnresolved);
        Assert.False(caps.ResolveSiteByHost);
        Assert.False(caps.FailIfSiteHostUnresolved);
        Assert.True(caps.IsPlatformHost);
    }

    /// <summary>
    /// WebApi：可选能力默认关，按域名定租+定站。
    /// </summary>
    [Fact]
    public void WebApi_OptionalFeatures_Off_WithTenantAndSite()
    {
        var caps = HostCapabilities.WebApi();
        AssertOptionalOff(caps);
        Assert.True(caps.ResolveTenantByHost);
        Assert.True(caps.FailIfTenantHostUnresolved);
        Assert.True(caps.ResolveSiteByHost);
        Assert.True(caps.FailIfSiteHostUnresolved);
        Assert.False(caps.IsPlatformHost);
    }

    /// <summary>
    /// Schedule（Worker）：可选能力关，不定租不定站。
    /// </summary>
    [Fact]
    public void Worker_OptionalFeatures_Off_NoTenantNoSite()
    {
        var caps = HostCapabilities.Worker();
        AssertOptionalOff(caps);
        Assert.False(caps.ResolveTenantByHost);
        Assert.False(caps.FailIfTenantHostUnresolved);
        Assert.False(caps.ResolveSiteByHost);
        Assert.False(caps.FailIfSiteHostUnresolved);
        Assert.False(caps.IsPlatformHost);
    }

    /// <summary>
    /// Channels：可选能力关，按域名定租，不定站（站信息走业务参数）。
    /// </summary>
    [Fact]
    public void Channels_OptionalFeatures_Off_TenantOnly_NoSite()
    {
        var caps = HostCapabilities.Channels();
        AssertOptionalOff(caps);
        Assert.True(caps.ResolveTenantByHost);
        Assert.True(caps.FailIfTenantHostUnresolved);
        Assert.False(caps.ResolveSiteByHost);
        Assert.False(caps.FailIfSiteHostUnresolved);
        Assert.False(caps.IsPlatformHost);
    }

    /// <summary>
    /// 后台类宿主应打开的可选能力。
    /// </summary>
    /// <param name="caps">能力快照</param>
    private static void AssertOptionalOn(HostCapabilityOptions caps)
    {
        Assert.True(caps.Captcha);
        Assert.True(caps.ImageSharp);
        Assert.True(caps.ViewEngine);
        Assert.True(caps.SignalR);
        Assert.True(caps.FileStorage);
        Assert.True(caps.IpLocation);
        Assert.True(caps.IpLocationInMemory);
        Assert.True(caps.Localization);
        Assert.True(caps.VirtualFileServer);
        Assert.True(caps.ConsoleLogo);
    }

    /// <summary>
    /// 对外/内部薄宿主应关闭的可选能力。
    /// </summary>
    /// <param name="caps">能力快照</param>
    private static void AssertOptionalOff(HostCapabilityOptions caps)
    {
        Assert.False(caps.Captcha);
        Assert.False(caps.ImageSharp);
        Assert.False(caps.ViewEngine);
        Assert.False(caps.SignalR);
        Assert.False(caps.FileStorage);
        Assert.False(caps.IpLocation);
        Assert.False(caps.IpLocationInMemory);
        Assert.False(caps.Localization);
        Assert.False(caps.VirtualFileServer);
        Assert.True(caps.ConsoleLogo);
    }
}

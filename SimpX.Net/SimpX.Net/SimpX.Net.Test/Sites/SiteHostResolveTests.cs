using Microsoft.AspNetCore.Http;
using SimpX.Net.Data;
using SimpX.Net.Hosting;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 按域名解析站点：主机规范化、请求头选取、上下文读写、进程能力开关（不连库）。
/// </summary>
public class SiteHostResolveTests
{
    /// <summary>
    /// 去掉端口、路径并转小写。
    /// </summary>
    [Fact]
    public void NormalizeHost_StripsPortPathAndLowercases()
    {
        Assert.Equal("a.example.com", SiteHostResolver.NormalizeHost("A.Example.COM:443/path"));
        Assert.Equal("127.0.0.1", SiteHostResolver.NormalizeHost("127.0.0.1:5005"));
        Assert.Null(SiteHostResolver.NormalizeHost("  "));
        Assert.Null(SiteHostResolver.NormalizeHost(null));
    }

    /// <summary>
    /// 从 Authority 抠 1xxx 站点号。
    /// </summary>
    [Fact]
    public void TryParseSiteIdFromHost_MatchesKnownRegex()
    {
        Assert.Equal(1001, SiteHostResolver.TryParseSiteIdFromHost("local1001.dev:8080"));
        Assert.Null(SiteHostResolver.TryParseSiteIdFromHost("a.example.com"));
    }

    /// <summary>
    /// 有 X-Forwarded-HostX 时优先用头，否则用 Host:Port。
    /// </summary>
    [Fact]
    public void PickRequestHost_PrefersForwardedHeader()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("internal.local", 5000);
        Assert.Equal("internal.local:5000", SiteHostResolveMiddleware.PickRequestHost(ctx.Request));

        ctx.Request.Headers[SiteHostResolver.ForwardedHostHeaderName] = "site1001.example.com";
        Assert.Equal("site1001.example.com", SiteHostResolveMiddleware.PickRequestHost(ctx.Request));
    }

    /// <summary>
    /// 站点解析结果写入 Items 后，访问器能读到 SiteID；换另一套 Items 不会串站。
    /// </summary>
    [Fact]
    public void HttpCurrentSiteAccessor_ReadsItems_NoCrossSiteBleed()
    {
        var accessor = new HttpContextAccessor();
        var siteAccessor = new HttpCurrentSiteAccessor(accessor);

        var siteA = new DefaultHttpContext();
        siteA.Items[SiteHostKeys.ResolvedSiteId] = 1001;
        siteA.Items[SiteHostKeys.ResolvedHost] = "a.example.com";
        accessor.HttpContext = siteA;
        Assert.Equal(1001, siteAccessor.SiteId);
        Assert.Equal("a.example.com", siteAccessor.Host);
        Assert.Equal(1001, siteAccessor.RequireSiteId());

        var siteB = new DefaultHttpContext();
        siteB.Items[SiteHostKeys.ResolvedSiteId] = 1002;
        siteB.Items[SiteHostKeys.ResolvedHost] = "b.example.com";
        accessor.HttpContext = siteB;
        Assert.Equal(1002, siteAccessor.SiteId);
        Assert.NotEqual(1001, siteAccessor.SiteId);
    }

    /// <summary>
    /// Admin/WebApi 打开按域名解析站点且认不到就失败；SuperAdmin/Worker/Channels 不定站。
    /// </summary>
    [Fact]
    public void HostCapabilities_SiteResolve_MatchesAuthMatrix()
    {
        Assert.True(HostCapabilities.Admin().ResolveSiteByHost);
        Assert.True(HostCapabilities.Admin().FailIfSiteHostUnresolved);
        Assert.True(HostCapabilities.Admin().ResolveTenantByHost);
        Assert.True(HostCapabilities.Admin().FailIfTenantHostUnresolved);

        Assert.True(HostCapabilities.WebApi().ResolveSiteByHost);
        Assert.True(HostCapabilities.WebApi().FailIfSiteHostUnresolved);
        Assert.True(HostCapabilities.WebApi().ResolveTenantByHost);

        Assert.False(HostCapabilities.SuperAdmin().ResolveSiteByHost);
        Assert.False(HostCapabilities.SuperAdmin().ResolveTenantByHost);
        Assert.True(HostCapabilities.SuperAdmin().IsPlatformHost);

        Assert.False(HostCapabilities.Worker().ResolveSiteByHost);
        Assert.False(HostCapabilities.Worker().ResolveTenantByHost);
        Assert.False(HostCapabilities.Channels().ResolveSiteByHost);
        Assert.True(HostCapabilities.Channels().ResolveTenantByHost);
    }
}

using SimpX.Net.Hosting;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// Schedule 宿主不定站不定租约定（F5.4）：Program 选用 Worker()，站点靠作业参数传入。
/// </summary>
public class ScheduleWorkerConventionTests
{
    /// <summary>
    /// Worker 预设：不定租、不定站、认不到也不失败（没有域名解析链路）。
    /// </summary>
    [Fact]
    public void Worker_DoesNotResolveTenantOrSite()
    {
        var caps = HostCapabilities.Worker();
        Assert.False(caps.ResolveTenantByHost);
        Assert.False(caps.FailIfTenantHostUnresolved);
        Assert.False(caps.ResolveSiteByHost);
        Assert.False(caps.FailIfSiteHostUnresolved);
        Assert.False(caps.IsPlatformHost);
    }

    /// <summary>
    /// Channels 与 Schedule 能力点不同：Channels 定租不定站；Schedule 两者都不定。
    /// </summary>
    [Fact]
    public void Channels_ResolvesTenant_ButNotSite_UnlikeWorker()
    {
        var channels = HostCapabilities.Channels();
        var worker = HostCapabilities.Worker();
        Assert.True(channels.ResolveTenantByHost);
        Assert.False(channels.ResolveSiteByHost);
        Assert.False(worker.ResolveTenantByHost);
        Assert.False(worker.ResolveSiteByHost);
    }
}

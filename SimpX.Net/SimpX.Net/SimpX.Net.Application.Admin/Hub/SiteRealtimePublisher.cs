using Furion.InstantMessaging;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// Admin 宿主：后台角标走 SiteNotifyHub；会员 Socket 本宿主不推（由 WebApi 实现）。
/// </summary>
public sealed class SiteRealtimePublisher : ISiteRealtimePublisher
{
    private readonly IHubInvocationBus _hub;

    /// <summary>注入。</summary>
    public SiteRealtimePublisher(IHubInvocationBus hub) => _hub = hub;

    /// <inheritdoc />
    public Task PublishAdminNotifyAsync(int siteId, AdminNotifyCounts counts, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (siteId <= 0 || counts == null) return Task.CompletedTask;
        return _hub.SendGroupAsync(
            typeof(SiteNotifyHub),
            SiteNotifyHub.GroupName(siteId),
            nameof(ISiteNotifyHub.AdminNotify),
            counts);
    }

    /// <inheritdoc />
    public Task PublishUserSocketAsync(int siteId, int userId, object payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

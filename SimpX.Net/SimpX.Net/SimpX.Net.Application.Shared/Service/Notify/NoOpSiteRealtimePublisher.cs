namespace SimpX.Net.Application.Shared;

/// <summary>
/// 无 Hub 宿主（Schedule/Channels）的空实现。
/// </summary>
public sealed class NoOpSiteRealtimePublisher : ISiteRealtimePublisher
{
    /// <inheritdoc />
    public Task PublishAdminNotifyAsync(int siteId, AdminNotifyCounts counts, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task PublishUserSocketAsync(int siteId, int userId, object payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

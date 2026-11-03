namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站内信空实现，单测未断言通知时用。
/// </summary>
public sealed class NoOpUserMessageNotifyService : IUserMessageNotifyService
{
    /// <summary>共享实例。</summary>
    public static NoOpUserMessageNotifyService Instance { get; } = new();

    /// <inheritdoc />
    public Task SendNotifyAsync(
        int siteId,
        int userId,
        string content,
        string title = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

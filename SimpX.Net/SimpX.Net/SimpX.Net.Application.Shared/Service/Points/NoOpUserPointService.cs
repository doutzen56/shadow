namespace SimpX.Net.Application.Shared;

/// <summary>
/// 积分空实现（单测未断言积分时用）。
/// </summary>
public sealed class NoOpUserPointService : IUserPointService
{
    /// <summary>共享实例。</summary>
    public static NoOpUserPointService Instance { get; } = new();

    /// <inheritdoc />
    public Task AddPointAsync(
        int siteId,
        int userId,
        decimal point,
        int pointType,
        string content,
        int eventType = 0,
        CancellationToken cancellationToken = default,
        DateTime? createAt = null) =>
        Task.CompletedTask;
}

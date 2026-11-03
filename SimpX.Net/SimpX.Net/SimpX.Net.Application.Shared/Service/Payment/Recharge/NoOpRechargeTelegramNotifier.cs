using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 飞机通知空实现：真实 Bot 未接时不抛错。
/// </summary>
public sealed class NoOpRechargeTelegramNotifier : ITransient, IRechargeTelegramNotifier
{
    /// <summary>共享实例（单测）。</summary>
    public static NoOpRechargeTelegramNotifier Instance { get; } = new();

    /// <inheritdoc />
    public Task NotifyCreditedAsync(RechargeOrder order, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task NotifyFirstRechargeAsync(RechargeOrder order, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

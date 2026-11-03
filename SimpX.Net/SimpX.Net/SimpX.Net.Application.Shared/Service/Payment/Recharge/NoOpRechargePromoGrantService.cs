namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值活动发奖空实现，单测未断言活动时用。
/// </summary>
public sealed class NoOpRechargePromoGrantService : IRechargePromoGrantService
{
    /// <summary>共享实例。</summary>
    public static NoOpRechargePromoGrantService Instance { get; } = new();

    /// <inheritdoc />
    public Task<int> GrantForOrderAsync(string orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}

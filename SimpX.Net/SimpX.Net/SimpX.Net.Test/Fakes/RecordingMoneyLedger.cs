using SimpX.Net.Application.Shared;

namespace SimpX.Net.Test;

/// <summary>
/// 单测假账本：记录账变与解锁；锁操作空实现。
/// </summary>
internal sealed class RecordingMoneyLedger : IMoneyLedger
{
    /// <summary>已写入的账变（含流水倍率与活动编号）。</summary>
    public List<(int SiteId, int UserId, decimal Money, int Type, string SourceId, int Currency, decimal FlowRate, decimal MinFlow, int EventId, int ReviewId)> Logs { get; } = [];

    /// <summary>审核解锁记录。</summary>
    public List<(int SiteId, int UserId, int SourceId, int Currency)> Unlocks { get; } = [];

    /// <summary>提现解锁记录。</summary>
    public List<(int SiteId, int UserId, int SourceId)> WithdrawUnlocks { get; } = [];

    /// <inheritdoc />
    public Task LockReviewAsync(int siteId, int userId, decimal money, int sourceId, string description, int currency, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task UnlockReviewAsync(int siteId, int userId, int sourceId, string description, int currency, CancellationToken cancellationToken = default)
    {
        Unlocks.Add((siteId, userId, sourceId, currency));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnlockWithdrawAsync(int siteId, int userId, int sourceId, string description, int currency, CancellationToken cancellationToken = default)
    {
        WithdrawUnlocks.Add((siteId, userId, sourceId));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddMoneyLogAsync(
        int siteId,
        int userId,
        decimal money,
        int type,
        string sourceId,
        string description,
        int currency,
        CancellationToken cancellationToken = default,
        decimal flowRate = 0,
        decimal minFlow = 0,
        int eventId = 0,
        int reviewId = 0)
    {
        Logs.Add((siteId, userId, money, type, sourceId, currency, flowRate, minFlow, eventId, reviewId));
        return Task.CompletedTask;
    }
}

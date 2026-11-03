using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 旁路队列弹出与 VIP 到期取出（内存实现）。
/// </summary>
public sealed class LedgerQueueDrainTests
{
    /// <summary>
    /// FIFO 弹出充值 / 账变 / 代理统计；VIP 到期可取出。
    /// </summary>
    [Fact]
    public void Memory_queues_dequeue_fifo_and_take_due_vip()
    {
        var q = new MemoryLedgerSideEffectQueues();
        q.EnqueueRechargeLog("A");
        q.EnqueueRechargeLog("B");
        Assert.True(q.TryDequeueRechargeLog(out var o1));
        Assert.Equal("A", o1);
        Assert.True(q.TryDequeueRechargeLog(out var o2));
        Assert.Equal("B", o2);
        Assert.False(q.TryDequeueRechargeLog(out _));

        q.EnqueueMoneyLog(11);
        q.EnqueueMoneyLog(12);
        Assert.True(q.TryDequeueMoneyLog(out var logId));
        Assert.Equal(11, logId);

        q.EnqueueProxyMoneySts("{\"UserId\":1}");
        Assert.True(q.TryDequeueProxyMoneySts(out var payload));
        Assert.Contains("UserId", payload);

        q.EnqueueUserMoneyAndVip(88, currency: 1, skipVip: false, delayMinutes: 0);
        var due = q.TakeDueUserVip(10);
        Assert.Contains(88, due);
        Assert.Empty(q.TakeDueUserVip(10));
    }
}

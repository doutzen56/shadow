using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 报表队列消费：验证弹出后交给消费端；真实 SP 在无库时只记警告。
/// </summary>
public sealed class QueueConsumerSqliteTests
{
    /// <summary>
    /// 内存队列 FIFO 弹出并交给记录型消费端（对齐 Job 循环）。
    /// </summary>
    [Fact]
    public async Task Drain_loop_feeds_consumers()
    {
        var queues = new MemoryLedgerSideEffectQueues();
        queues.EnqueueMoneyLog(101);
        queues.EnqueueMoneyLog(102);
        queues.EnqueueProxyMoneySts(
            """{"UserId":9,"SiteId":1001,"Type":140,"Money":12.5,"Currency":1,"CreateAt":"2026-11-01T12:00:00"}""");

        var team = new RecordingTeamConsumer();
        var proxy = new RecordingProxyConsumer();

        while (queues.TryDequeueMoneyLog(out var logId))
            await team.ConsumeAsync(logId);
        while (queues.TryDequeueProxyMoneySts(out var payload))
            await proxy.ConsumeAsync(payload);

        Assert.Equal(new[] { 101, 102 }, team.Ids);
        Assert.Single(proxy.Payloads);
        Assert.Contains("UserId", proxy.Payloads[0]);
    }

    /// <summary>
    /// 无存储过程时消费端不抛（SQLite 内存库）。
    /// </summary>
    [Fact]
    public async Task Real_consumers_swallow_missing_sp()
    {
        var db = SqliteBizSchema.CreateMemoryDb([], []);
        var team = new TeamMoneyLogConsumer(new TestBizDb(db));
        var proxy = new ProxyMoneyStsConsumer(new TestBizDb(db));
        await team.ConsumeAsync(1);
        await proxy.ConsumeAsync(
            """{"UserId":1,"SiteId":1001,"Type":140,"Money":1,"Currency":1,"CreateAt":"2026-11-01"}""");
        db.Dispose();
    }

    private sealed class RecordingTeamConsumer : ITeamMoneyLogConsumer
    {
        public List<int> Ids { get; } = [];

        public Task ConsumeAsync(int logId, CancellationToken cancellationToken = default)
        {
            Ids.Add(logId);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingProxyConsumer : IProxyMoneyStsConsumer
    {
        public List<string> Payloads { get; } = [];

        public Task ConsumeAsync(string payloadJson, CancellationToken cancellationToken = default)
        {
            Payloads.Add(payloadJson);
            return Task.CompletedTask;
        }
    }
}

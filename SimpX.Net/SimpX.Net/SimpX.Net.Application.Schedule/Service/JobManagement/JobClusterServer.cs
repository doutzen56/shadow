using SimpX.Net.Contracts.Jobs;
using Furion.Logging;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 作业集群控制：多个调度实例之间选出一个执行任务（只有调度宿主注册调度）。
/// 执行任务的节点定时写心跳；心跳超时的“工作中”节点视为已宕机，由等待中的节点接管，
/// 避免节点被强杀、断电后没机会把自己标成停止，其他节点永远等下去。
/// </summary>
public class JobClusterServer : IJobClusterServer
{
    private static readonly SqlSugarRepository<SysJobCluster> _sysJobClusterRep = App.GetRequiredService<SqlSugarRepository<SysJobCluster>>();
    private readonly Random _random = new(DateTime.Now.Millisecond);
    private CancellationTokenSource _heartbeatCts;

    /// <summary>
    /// 当前作业调度器启动通知：登记本节点为等待中，并记一次心跳时间。
    /// </summary>
    /// <param name="context">作业集群服务上下文</param>
    public async void Start(JobClusterContext context)
    {
        var now = DateTime.Now;
        if (await _sysJobClusterRep.IsAnyAsync(u => u.ClusterId == context.ClusterId))
        {
            await _sysJobClusterRep.AsUpdateable()
                .SetColumns(u => new SysJobCluster { Status = ClusterStatus.Waiting, UpdatedTime = now })
                .Where(u => u.ClusterId == context.ClusterId)
                .ExecuteCommandAsync();
        }
        else
        {
            await _sysJobClusterRep.AsInsertable(new SysJobCluster { ClusterId = context.ClusterId, Status = ClusterStatus.Waiting, UpdatedTime = now }).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 等待被唤醒：多个实例之间抢执行权，同一时刻只有一个工作中。
    /// 别的节点工作中但心跳超时，就把它标成已宕机再接管。
    /// </summary>
    /// <param name="context">作业集群服务上下文</param>
    /// <returns>本节点拿到执行权后返回</returns>
    public async Task WaitingForAsync(JobClusterContext context)
    {
        var clusterId = context.ClusterId;
        var (heartbeat, timeout) = GetHeartbeatSettings();

        while (true)
        {
            // 控制集群心跳频率（放在头部为了防止 continue 没 sleep 占用大量 IO 和 CPU），随机错开集群同时启动
            await Task.Delay(3000 + _random.Next(500, 1000));

            try
            {
                using (App.GetRequiredService<ICacheStateStore>().AcquireLock(CommonCacheKeys.Lock, "JobClusterServer:WaitingForAsync", 1000))
                {
                    // 心跳超时的“工作中”节点视为已宕机。各节点用自己的时钟写心跳、判超时，多机部署要做时间同步
                    var deadline = DateTime.Now.AddSeconds(-timeout.TotalSeconds);
                    await _sysJobClusterRep.AsUpdateable()
                        .SetColumns(u => u.Status == ClusterStatus.Crashed)
                        .Where(u => u.Status == ClusterStatus.Working && u.ClusterId != clusterId && (u.UpdatedTime == null || u.UpdatedTime < deadline))
                        .ExecuteCommandAsync();

                    if (await _sysJobClusterRep.IsAnyAsync(u => u.Status == ClusterStatus.Working && u.ClusterId != clusterId)) continue;

                    await WorkNowAsync(clusterId);
                    StartHeartbeat(clusterId, heartbeat);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"作业集群抢执行权失败，稍后重试：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 当前作业调度器停止通知：停掉心跳，标成已停止，让其他节点马上能接管。
    /// </summary>
    /// <param name="context">作业集群服务上下文</param>
    public async void Stop(JobClusterContext context)
    {
        _heartbeatCts?.Cancel();
        await _sysJobClusterRep.UpdateAsync(u => new SysJobCluster { Status = ClusterStatus.Crashed }, u => u.ClusterId == context.ClusterId);
    }

    /// <summary>
    /// 当前作业调度器宕机：停掉心跳，标成已宕机。
    /// </summary>
    /// <param name="context">作业集群服务上下文</param>
    public async void Crash(JobClusterContext context)
    {
        _heartbeatCts?.Cancel();
        await _sysJobClusterRep.UpdateAsync(u => new SysJobCluster { Status = ClusterStatus.Crashed }, u => u.ClusterId == context.ClusterId);
    }

    /// <summary>
    /// 指示集群可以工作：标成工作中并记一次心跳时间。
    /// </summary>
    /// <param name="clusterId">集群 Id</param>
    /// <returns>异步任务</returns>
    private static async Task WorkNowAsync(string clusterId)
    {
        var now = DateTime.Now;
        await _sysJobClusterRep.UpdateAsync(u => new SysJobCluster { Status = ClusterStatus.Working, UpdatedTime = now }, u => u.ClusterId == clusterId);
    }

    /// <summary>
    /// 拿到执行权后在后台定时写心跳。写不进去（自己已不是工作中）说明心跳超时期间被别的节点接管了，
    /// 这时暂停本节点全部作业，否则同一作业会在两个节点上重复执行。
    /// </summary>
    /// <param name="clusterId">集群 Id</param>
    /// <param name="interval">心跳间隔</param>
    private void StartHeartbeat(string clusterId, TimeSpan interval)
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts = new CancellationTokenSource();
        var token = _heartbeatCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                try
                {
                    var now = DateTime.Now;
                    var updated = await _sysJobClusterRep.AsUpdateable()
                        .SetColumns(u => u.UpdatedTime == now)
                        .Where(u => u.ClusterId == clusterId && u.Status == ClusterStatus.Working)
                        .ExecuteCommandAsync();
                    if (updated > 0) continue;

                    App.GetRequiredService<ISchedulerFactory>().PauseAll();
                    Log.Error($"作业集群节点 {clusterId} 心跳超时已被其他节点接管，本节点作业已全部暂停，重启本节点后重新参与选举");
                    return;
                }
                catch (Exception ex)
                {
                    // 数据库一时连不上不算被接管，下一轮接着写；连续写不上超过超时时间，其他节点会接管
                    Log.Warning($"作业集群心跳写入失败：{ex.Message}");
                }
            }
        }, token);
    }

    /// <summary>
    /// 读心跳间隔和超时时间。超时至少是间隔的 2 倍，避免一次心跳稍慢就被误判为宕机。
    /// </summary>
    /// <returns>心跳间隔和超时时间</returns>
    private static (TimeSpan Heartbeat, TimeSpan Timeout) GetHeartbeatSettings()
    {
        var options = App.GetOptions<ScheduleHostOptions>();
        var heartbeat = Math.Max(1, options?.ClusterHeartbeatSeconds ?? 10);
        var timeout = Math.Max(heartbeat * 2, options?.ClusterTimeoutSeconds ?? 30);
        return (TimeSpan.FromSeconds(heartbeat), TimeSpan.FromSeconds(timeout));
    }
}

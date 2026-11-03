namespace SimpX.Net.Core;

/// <summary>
/// 集群相关配置。Redis 模式下用来区分实例；推送转发看缓存是不是 Redis，不再靠这里的开关。
/// </summary>
public sealed class ClusterOptions : IConfigurableOptions
{
    /// <summary>
    /// 本实例标识。事件总线广播按它分组，每个进程要不一样。
    /// 空着时会用「机器名-进程号」。
    /// </summary>
    public string ServerId { get; set; }

    /// <summary>
    /// 本机 IP，可选，仅展示用。
    /// </summary>
    public string ServerIp { get; set; }

    /// <summary>
    /// 解析本实例标识：配置有值就用配置，否则用机器名加进程号，保证多实例广播时每台都能收到。
    /// </summary>
    /// <returns>实例标识</returns>
    public string ResolveServerId()
    {
        if (!string.IsNullOrWhiteSpace(ServerId))
            return ServerId.Trim();

        return $"{Environment.MachineName}-{Environment.ProcessId}";
    }
}

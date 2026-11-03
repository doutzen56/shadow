using Furion.ConfigurableOptions;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 调度宿主自身的配置，从配置里的 ScheduleHost 这一段读取，不依赖后台的系统配置表。
/// </summary>
public sealed class ScheduleHostOptions : IConfigurableOptions
{
    /// <summary>
    /// 作业执行出错时是否发告警邮件（邮件通道用 Core 的 Email 配置）。
    /// </summary>
    public bool SendErrorMail { get; set; }

    /// <summary>
    /// 调度看板登录账号。留空则看板不允许登录。
    /// </summary>
    public string DashboardUsername { get; set; }

    /// <summary>
    /// 调度看板登录密码。只放在部署配置里，不写日志。
    /// </summary>
    public string DashboardPassword { get; set; }

    /// <summary>
    /// 调度看板登录凭据的签名密钥，至少 16 位。看板由 Hosting 的请求处理链挂载，直接读配置里的 ScheduleHost:DashboardAppSecret。
    /// </summary>
    public string DashboardAppSecret { get; set; }

    /// <summary>
    /// 集群心跳间隔（秒）：执行任务的节点每隔这么久写一次心跳。
    /// </summary>
    public int ClusterHeartbeatSeconds { get; set; } = 10;

    /// <summary>
    /// 集群心跳超时（秒）：工作中的节点超过这么久没写心跳就视为已宕机，由等待中的节点接管。
    /// 至少是心跳间隔的 2 倍；多机部署要做时间同步，否则会误判。
    /// </summary>
    public int ClusterTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 自助反水 V2 运行时总开关。关闭时到点切换与同步 Job 不动作。
    /// 也可用 AppSettings:AutoReturnV2RuntimeEnabled。
    /// </summary>
    public bool AutoReturnV2RuntimeEnabled { get; set; }
}

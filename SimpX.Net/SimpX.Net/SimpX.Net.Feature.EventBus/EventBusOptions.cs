namespace SimpX.Net.Feature.EventBus;

/// <summary>
/// 事件总线配置选项。默认事件源走内存或 Redis；要用 RabbitMQ 时再引用 SimpX.Net.EventBus.RabbitMQ，并配置本节。
/// </summary>
public sealed class EventBusOptions : IConfigurableOptions
{
    /// <summary>
    /// RabbitMQ 连接信息（仅在启用 RabbitMQ 事件源时用到）
    /// </summary>
    public RabbitMQSettings RabbitMQ { get; set; }
}

/// <summary>
/// RabbitMQ 连接信息
/// </summary>
public sealed class RabbitMQSettings
{
    /// <summary>
    /// 账号
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// 主机
    /// </summary>
    public string HostName { get; set; }

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; }
}
using Furion;
using Furion.EventBus;
using Furion.FriendlyException;
using RabbitMQ.Client;
using SimpX.Net.Core;

namespace SimpX.Net.EventBus.RabbitMQ;

/// <summary>
/// 创建 RabbitMQ 事件源存储器。默认宿主走内存或 Redis；多机要用 RabbitMQ 时，在 AddEventBus 里
/// <c>options.ReplaceStorer(_ => RabbitMQEventBusSetup.CreateEventSourceStorer())</c>。
/// </summary>
public static class RabbitMQEventBusSetup
{
    /// <summary>
    /// 按 EventBus:RabbitMQ 配置创建事件源存储器。
    /// </summary>
    /// <param name="routeKey">队列路由键</param>
    /// <param name="capacity">内存通道容量，写满后等待</param>
    /// <returns>可交给 ReplaceStorer 的存储器实例</returns>
    public static IEventSourceStorer CreateEventSourceStorer(
        string routeKey = "simpxnet_eventsource_queue",
        int capacity = 3000)
    {
        var settings = App.GetConfig<EventBusOptions>("EventBus", true)?.RabbitMQ
            ?? throw Oops.Oh("未找到 EventBus:RabbitMQ 配置，不能启用 RabbitMQ 事件源");

        var factory = new ConnectionFactory
        {
            UserName = settings.UserName,
            Password = settings.Password,
            HostName = settings.HostName,
            Port = settings.Port
        };

        return new RabbitMQEventSourceStore(factory, routeKey, capacity);
    }
}

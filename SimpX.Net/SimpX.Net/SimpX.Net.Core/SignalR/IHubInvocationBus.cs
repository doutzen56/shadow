namespace SimpX.Net.Core;

/// <summary>
/// 把集线器上的一次推送发到本机连接。缓存是 Redis 时，同时广播给其他实例，由各实例推给自己手上的连接。
/// 内存缓存只推本机。往分组里加连接只发生在本机，因为连接就在本机上。
/// </summary>
public interface IHubInvocationBus
{
    /// <summary>
    /// 推给所有连接。
    /// </summary>
    /// <param name="hubType">集线器类型</param>
    /// <param name="method">客户端方法名</param>
    /// <param name="argument">参数，会按原类型再还原</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task SendAllAsync(Type hubType, string method, object argument, CancellationToken cancellationToken = default);

    /// <summary>
    /// 推给一个连接。
    /// </summary>
    /// <param name="hubType">集线器类型</param>
    /// <param name="connectionId">连接编号</param>
    /// <param name="method">客户端方法名</param>
    /// <param name="argument">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task SendClientAsync(Type hubType, string connectionId, string method, object argument, CancellationToken cancellationToken = default);

    /// <summary>
    /// 推给若干连接。
    /// </summary>
    /// <param name="hubType">集线器类型</param>
    /// <param name="connectionIds">连接编号</param>
    /// <param name="method">客户端方法名</param>
    /// <param name="argument">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task SendClientsAsync(Type hubType, IEnumerable<string> connectionIds, string method, object argument, CancellationToken cancellationToken = default);

    /// <summary>
    /// 推给除这些连接以外的所有连接。
    /// </summary>
    /// <param name="hubType">集线器类型</param>
    /// <param name="excludedConnectionIds">要排除的连接编号</param>
    /// <param name="method">客户端方法名</param>
    /// <param name="argument">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task SendAllExceptAsync(Type hubType, IEnumerable<string> excludedConnectionIds, string method, object argument, CancellationToken cancellationToken = default);

    /// <summary>
    /// 推给一个分组。每个实例只推自己分组里的连接。
    /// </summary>
    /// <param name="hubType">集线器类型</param>
    /// <param name="groupName">分组名</param>
    /// <param name="method">客户端方法名</param>
    /// <param name="argument">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task SendGroupAsync(Type hubType, string groupName, string method, object argument, CancellationToken cancellationToken = default);

    /// <summary>
    /// 把连接加进分组。只在本机做，不广播。
    /// </summary>
    /// <param name="hubType">集线器类型</param>
    /// <param name="connectionId">连接编号</param>
    /// <param name="groupName">分组名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task AddToGroupAsync(Type hubType, string connectionId, string groupName, CancellationToken cancellationToken = default);
}

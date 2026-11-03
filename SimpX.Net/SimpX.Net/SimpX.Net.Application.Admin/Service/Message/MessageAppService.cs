namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统消息发送服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "message", Order = 370)]
public class MessageAppService : IDynamicApiController, ITransient
{
    private readonly ICacheStateStore _state;
    private readonly IHubInvocationBus _hub;

    /// <summary>
    /// 注入在线用户状态缓存和集线器推送。Redis 时推送会发到每个实例。
    /// </summary>
    /// <param name="state">状态缓存</param>
    /// <param name="hub">集线器推送</param>
    public MessageAppService(ICacheStateStore state, IHubInvocationBus hub)
    {
        _state = state;
        _hub = hub;
    }

    /// <summary>
    /// 发送消息给所有人 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("发送消息给所有人")]
    public async Task SendAllUser(MessageInput input)
    {
        await _hub.SendAllAsync(typeof(OnlineUserHub), nameof(IOnlineUserHub.ReceiveMessage), input);
    }

    /// <summary>
    /// 发送消息给除了发送人的其他人 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("发送消息给除了发送人的其他人")]
    public async Task SendOtherUser(MessageInput input)
    {
        var hashKey = _state.HashValues<SysOnlineUser>(CommonCacheKeys.OnlineUser, CommonCacheKeys.OnlineUserId);
        var exceptReceiveUsers = hashKey.Where(u => u.UserId == input.ReceiveUserId).Select(u => u).ToList();
        await _hub.SendAllExceptAsync(typeof(OnlineUserHub), exceptReceiveUsers.Select(t => t.ConnectionId), nameof(IOnlineUserHub.ReceiveMessage), input);
    }

    /// <summary>
    /// 发送消息给某个人 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("发送消息给某个人")]
    public async Task SendUser(MessageInput input)
    {
        var hashKey = _state.HashValues<SysOnlineUser>(CommonCacheKeys.OnlineUser, CommonCacheKeys.OnlineUserId);
        var receiveUser = hashKey.Where(u => u.UserId == input.ReceiveUserId).Select(u => u).FirstOrDefault();
        await _hub.SendClientAsync(typeof(OnlineUserHub), receiveUser.ConnectionId ?? "", nameof(IOnlineUserHub.ReceiveMessage), input);
    }

    /// <summary>
    /// 发送消息给某些人 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("发送消息给某些人")]
    public async Task SendUsers(MessageInput input)
    {
        var hashKey = _state.HashValues<SysOnlineUser>(CommonCacheKeys.OnlineUser, CommonCacheKeys.OnlineUserId);
        var receiveUsers = hashKey.Where(u => input.UserIds.Any(a => a == u.UserId)).Select(u => u).ToList();
        await _hub.SendClientsAsync(
            typeof(OnlineUserHub),
            receiveUsers.Select(u => u.ConnectionId),
            nameof(IOnlineUserHub.ReceiveMessage),
            input);
    }
}
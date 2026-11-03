using Furion.InstantMessaging;
using Microsoft.AspNetCore.SignalR;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 在线用户集线器：只负责登记在线用户、往客户端推送（在线列表、通知、强制下线）。
/// 要登录才能连；强制下线、发消息这类管理操作走带权限码的 HTTP 接口（在线用户、消息服务），不在这里开放给客户端直接调。
/// </summary>
[Authorize]
[MapHub("/hubs/onlineUser")]
public class OnlineUserHub : Hub<IOnlineUserHub>
{
    private const string GROUP_ONLINE = "GROUP_ONLINE_"; // 租户分组前缀

    private readonly SqlSugarRepository<SysOnlineUser> _sysOnlineUerRep;
    private readonly IHubInvocationBus _hub;
    private readonly ICacheStateStore _state;
    private readonly ConfigAppService _configAppService;

    /// <summary>
    /// 注入在线用户仓储、集线器推送、缓存和系统配置。
    /// </summary>
    /// <param name="sysOnlineUerRep">在线用户仓储</param>
    /// <param name="hub">集线器推送。加分组只在本机，在线列表会发到每个实例</param>
    /// <param name="state">在线用户状态缓存</param>
    /// <param name="configAppService">系统配置</param>
    public OnlineUserHub(SqlSugarRepository<SysOnlineUser> sysOnlineUerRep,
        IHubInvocationBus hub,
        ICacheStateStore state,
        ConfigAppService configAppService)
    {
        _sysOnlineUerRep = sysOnlineUerRep;
        _hub = hub;
        _state = state;
        _configAppService = configAppService;
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <returns></returns>
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userId = (httpContext.User.FindFirst(ClaimConst.UserId)?.Value).ToLong();
        var account = httpContext.User.FindFirst(ClaimConst.Account)?.Value;
        var realName = httpContext.User.FindFirst(ClaimConst.RealName)?.Value;
        var tenantId = (httpContext.User.FindFirst(ClaimConst.TenantId)?.Value).ToLong();

        if (userId < 0 || string.IsNullOrWhiteSpace(account)) return;
        var user = new SysOnlineUser
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId,
            UserName = account,
            RealName = realName,
            Time = DateTime.Now,
            Ip = httpContext.GetRemoteIpAddressToIPv4(true),
            Browser = httpContext.GetClientBrowser(),
            Os = httpContext.GetClientOs(),
            TenantId = tenantId,
        };
        await _sysOnlineUerRep.InsertAsync(user);

        // 是否开启单用户登录
        if (await _configAppService.GetConfigValue<bool>(ConfigConst.SysSingleLogin))
        {
            _state.HashSet(CommonCacheKeys.OnlineUser, CommonCacheKeys.OnlineUserId, user.UserId.ToString(), user);
        }
        else
        {
            _state.HashSet(CommonCacheKeys.OnlineUser, CommonCacheKeys.OnlineUserId, user.UserId + Context.ConnectionId, user);
        }

        // 以租户Id进行分组
        var groupName = $"{GROUP_ONLINE}{user.TenantId}";
        await _hub.AddToGroupAsync(typeof(OnlineUserHub), Context.ConnectionId, groupName);

        var userList = await _sysOnlineUerRep.AsQueryable().Filter("", true)
            .Where(u => u.TenantId == user.TenantId).Take(10).ToListAsync();

        if (await _configAppService.GetConfigValue<bool>(ConfigConst.SysLoginOutReminder))
            await _hub.SendGroupAsync(typeof(OnlineUserHub), groupName, nameof(IOnlineUserHub.OnlineUserList), new OnlineUserList
            {
                RealName = user.RealName,
                Online = true,
                UserList = userList
            });
    }

    /// <summary>
    /// 断开
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (string.IsNullOrEmpty(Context.ConnectionId)) return;

        var httpContext = Context.GetHttpContext();

        var user = await _sysOnlineUerRep.AsQueryable().Filter("", true).FirstAsync(u => u.ConnectionId == Context.ConnectionId);
        if (user == null) return;

        await _sysOnlineUerRep.DeleteByIdAsync(user.Id);

        // 是否开启单用户登录
        if (await _configAppService.GetConfigValue<bool>(ConfigConst.SysSingleLogin))
        {
            _state.HashRemove(CommonCacheKeys.OnlineUser, CommonCacheKeys.OnlineUserId, user.UserId.ToString());
        }
        else
        {
            _state.HashRemove(CommonCacheKeys.OnlineUser, CommonCacheKeys.OnlineUserId, user.UserId + Context.ConnectionId);
        }

        // 通知当前组用户变动
        var userList = await _sysOnlineUerRep.AsQueryable().Filter("", true)
            .Where(u => u.TenantId == user.TenantId).Take(10).ToListAsync();

        if (await _configAppService.GetConfigValue<bool>(ConfigConst.SysLoginOutReminder))
            await _hub.SendGroupAsync(typeof(OnlineUserHub), $"{GROUP_ONLINE}{user.TenantId}", nameof(IOnlineUserHub.OnlineUserList), new OnlineUserList
            {
                RealName = user.RealName,
                Online = false,
                UserList = userList
            });
    }
}
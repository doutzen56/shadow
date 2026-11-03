using Furion.InstantMessaging;
using Microsoft.AspNetCore.SignalR;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台业务推送 Hub。通道用新框架 SignalR；载荷对齐旧 Notify。
/// 连接后按当前站进组 <c>siteNotify:{siteId}</c>。
/// </summary>
[Authorize]
[MapHub("/hubs/siteNotify")]
public class SiteNotifyHub : Hub<ISiteNotifyHub>
{
    private readonly ICurrentSiteAccessor _site;
    private readonly IHubInvocationBus _hub;

    /// <summary>注入。</summary>
    public SiteNotifyHub(ICurrentSiteAccessor site, IHubInvocationBus hub)
    {
        _site = site;
        _hub = hub;
    }

    /// <summary>进站组。</summary>
    public override async Task OnConnectedAsync()
    {
        var siteId = _site.SiteId ?? 0;
        if (siteId > 0)
            await _hub.AddToGroupAsync(typeof(SiteNotifyHub), Context.ConnectionId, GroupName(siteId));
        await base.OnConnectedAsync();
    }

    /// <summary>客户端可主动订阅站（多站切换时）。</summary>
    /// <param name="siteId">站点</param>
    public async Task JoinSite(int siteId)
    {
        if (siteId <= 0) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(siteId));
    }

    /// <summary>组名。</summary>
    public static string GroupName(int siteId) => $"siteNotify:{siteId}";
}

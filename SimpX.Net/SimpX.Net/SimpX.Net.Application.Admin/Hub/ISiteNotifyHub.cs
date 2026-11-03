using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台业务推送客户端契约（SignalR）。内容对齐旧 Admin Notify 频道。
/// </summary>
public interface ISiteNotifyHub
{
    /// <summary>
    /// 角标刷新。客户端方法名 <c>AdminNotify</c>。
    /// </summary>
    /// <param name="counts">角标</param>
    Task AdminNotify(AdminNotifyCounts counts);
}

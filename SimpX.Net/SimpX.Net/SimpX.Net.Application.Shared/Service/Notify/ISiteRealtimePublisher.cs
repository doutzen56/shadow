namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点实时推送。实现放各宿主（Admin/WebApi SignalR）；Shared 只发内容。
/// </summary>
public interface ISiteRealtimePublisher
{
    /// <summary>
    /// 推送后台角标（对齐旧 NotifyAgent 刷新后的 Admin 频道内容）。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="counts">角标</param>
    /// <param name="cancellationToken">取消</param>
    Task PublishAdminNotifyAsync(int siteId, AdminNotifyCounts counts, CancellationToken cancellationToken = default);

    /// <summary>
    /// 推送会员端 Socket 事件（对齐旧 SocketAgent 载荷：Event/Status/…）。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="payload">匿名对象或字典，须含 Event</param>
    /// <param name="cancellationToken">取消</param>
    Task PublishUserSocketAsync(int siteId, int userId, object payload, CancellationToken cancellationToken = default);
}

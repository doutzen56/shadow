namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站内通知发送。对齐旧 MessageAgent.SendMessage(userId, content, title)。
/// </summary>
public interface IUserMessageNotifyService
{
    /// <summary>
    /// 给会员写一条站内通知（未读）。
    /// </summary>
    /// <param name="siteId">站点编号</param>
    /// <param name="userId">会员编号</param>
    /// <param name="content">正文</param>
    /// <param name="title">标题；空则截取正文前 50 字</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SendNotifyAsync(
        int siteId,
        int userId,
        string content,
        string title = null,
        CancellationToken cancellationToken = default);
}

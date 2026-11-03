using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站内通知：写入 usr_Message，类型为站内通知。
/// </summary>
public sealed class UserMessageNotifyService : ITransient, IUserMessageNotifyService
{
    /// <summary>旧 UserMessage.MessageType.Notify。</summary>
    public const int MessageTypeNotify = 0;

    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    public UserMessageNotifyService(IBizDbAccessor biz)
    {
        _db = biz.Db;
    }

    /// <inheritdoc />
    public async Task SendNotifyAsync(
        int siteId,
        int userId,
        string content,
        string title = null,
        CancellationToken cancellationToken = default)
    {
        if (siteId <= 0 || userId <= 0)
            return;
        var body = (content ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(body))
            return;

        var head = (title ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(head))
            head = StripRoughHtml(body);
        if (head.Length > 50)
            head = head[..50];

        var now = DateTime.Now;
        await _db.Insertable(new UserMessage
        {
            SiteID = siteId,
            UserID = userId,
            SendID = 0,
            Type = MessageTypeNotify,
            CreateAt = now,
            ReadAt = DateTime.MinValue,
            Title = head,
            Content = body,
            IsRead = false,
            SupportCurrency = string.Empty
        }).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 粗略去掉尖括号标签，给标题用。
    /// </summary>
    private static string StripRoughHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        var chars = new char[text.Length];
        var n = 0;
        var inTag = false;
        foreach (var c in text)
        {
            if (c == '<')
            {
                inTag = true;
                continue;
            }

            if (c == '>')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
                chars[n++] = c;
        }

        return new string(chars, 0, n).Trim();
    }
}

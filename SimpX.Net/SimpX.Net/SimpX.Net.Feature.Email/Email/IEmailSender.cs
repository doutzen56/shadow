namespace SimpX.Net.Feature.Email;

/// <summary>
/// 发邮件的基础能力，按 Email 配置连 SMTP。各宿主都能用，不依赖后台业务数据。
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// 发送 HTML 邮件。
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="htmlContent">HTML 正文</param>
    /// <param name="toEmail">收件人，空时发给 Email:DefaultToEmail</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task SendAsync(string title, string htmlContent, string toEmail = null, CancellationToken cancellationToken = default);
}

using MailKit.Net.Smtp;
using MimeKit;

namespace SimpX.Net.Feature.Email;

/// <summary>
/// 用 MailKit 通过 SMTP 发邮件，连接参数全部来自 Email 配置。
/// </summary>
public sealed class SmtpEmailSender : IEmailSender, ITransient
{
    private readonly IOptionsMonitor<EmailOptions> _options;

    /// <summary>
    /// 注入邮件配置。
    /// </summary>
    /// <param name="options">Email 配置</param>
    public SmtpEmailSender(IOptionsMonitor<EmailOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// 发送 HTML 邮件；每次发送新建连接，发完断开。
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="htmlContent">HTML 正文</param>
    /// <param name="toEmail">收件人，空时发给默认收件人</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task SendAsync(string title, string htmlContent, string toEmail = null, CancellationToken cancellationToken = default)
    {
        var opt = _options.CurrentValue;
        var to = string.IsNullOrWhiteSpace(toEmail) ? opt.DefaultToEmail : toEmail;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(opt.DefaultFromEmail, opt.DefaultFromEmail));
        message.To.Add(new MailboxAddress(to, to));
        message.Subject = title;
        message.Body = new TextPart("html") { Text = htmlContent };

        using var client = new SmtpClient();
        await client.ConnectAsync(opt.Host, opt.Port, opt.EnableSsl, cancellationToken);
        await client.AuthenticateAsync(opt.UserName, opt.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

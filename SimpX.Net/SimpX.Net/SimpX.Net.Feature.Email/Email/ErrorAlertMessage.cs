namespace SimpX.Net.Feature.Email;

/// <summary>
/// 告警邮件事件的内容（事件名 <see cref="CommonConst.SendErrorMail"/>）。
/// </summary>
public sealed class ErrorAlertMessage
{
    /// <summary>
    /// 来源，如“定时任务”“系统日志”，会拼进邮件标题。
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// 告警正文。
    /// </summary>
    public string Content { get; set; }
}

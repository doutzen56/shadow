namespace SimpX.Net.Core;

/// <summary>
/// 消息模板类型枚举
/// </summary>
[Description("消息模板类型枚举")]
public enum TemplateTypeEnum
{
    /// <summary>
    /// 通知公告
    /// </summary>
    [Description("通知")]
    Notice = 1,

    /// <summary>
    /// 短信
    /// </summary>
    [Description("短信")]
    SMS = 2,

    /// <summary>
    /// 邮件
    /// </summary>
    [Description("邮件")]
    Email = 3
}
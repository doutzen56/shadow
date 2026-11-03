namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 站内信
/// </summary>
[SugarTable("usr_Message")]
public class UserMessage
{
    /// <summary>
    /// 站内信编号
    /// </summary>
    [SugarColumn(ColumnName = "MsgID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 发送者账号
    /// </summary>
    [SugarColumn(ColumnName = "SendID")]
    public int SendID { get; set; }

    /// <summary>
    /// 信息类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 信息的创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 阅读时间
    /// </summary>
    [SugarColumn(ColumnName = "ReadAt")]
    public DateTime ReadAt { get; set; }

    /// <summary>
    /// 信息标题
    /// </summary>
    [SugarColumn(ColumnName = "Title")]
    public string Title { get; set; }

    /// <summary>
    /// 信息内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    [SugarColumn(ColumnName = "IsRead")]
    public bool IsRead { get; set; }

    /// <summary>
    /// 支持货币
    /// </summary>
    [SugarColumn(ColumnName = "SupportCurrency")]
    public string SupportCurrency { get; set; }
}

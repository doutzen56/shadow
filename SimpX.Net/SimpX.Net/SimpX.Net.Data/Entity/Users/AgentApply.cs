namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 代理申请日志
/// </summary>
[SugarTable("usr_AgentApply")]
public class AgentApply
{
    [SugarColumn(ColumnName = "ApplyID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "Skype")]
    public string Skype { get; set; }

    [SugarColumn(ColumnName = "QQ")]
    public string QQ { get; set; }

    [SugarColumn(ColumnName = "Area")]
    public string Area { get; set; }

    [SugarColumn(ColumnName = "Phone")]
    public string Phone { get; set; }

    [SugarColumn(ColumnName = "Telegram")]
    public string Telegram { get; set; }

    [SugarColumn(ColumnName = "Line")]
    public string Line { get; set; }

    [SugarColumn(ColumnName = "WhatsApp")]
    public string WhatsApp { get; set; }

    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 应用状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }
}

namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户日志
/// </summary>
[SugarTable("usr_Log")]
public class UserLog
{
    /// <summary>
    /// 日志编号
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 操作内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>
    /// 管理员代操作
    /// </summary>
    [SugarColumn(ColumnName = "AdminID")]
    public int AdminID { get; set; }
}

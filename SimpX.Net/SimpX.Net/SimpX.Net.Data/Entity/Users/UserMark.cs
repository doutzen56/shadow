namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户备注信息
/// </summary>
[SugarTable("usr_Mark")]
public class UserMark
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "MarkID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 备注内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 添加人
    /// </summary>
    [SugarColumn(ColumnName = "AdminName")]
    public string AdminName { get; set; }
}

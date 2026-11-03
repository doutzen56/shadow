namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户签到表
/// </summary>
[SugarTable("usr_Sign")]
public class UserSign
{
    /// <summary>
    /// 主键
    /// </summary>
    [SugarColumn(ColumnName = "SignID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 是否手动添加签到记录
    /// </summary>
    [SugarColumn(ColumnName = "IsAdd")]
    public bool IsAdd { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}

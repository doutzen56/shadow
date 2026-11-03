namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的代理层级管理
/// </summary>
[SugarTable("usr_Depth")]
public class UserDepth
{
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>
    /// 上级ID
    /// </summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>
    /// 下级ID
    /// </summary>
    [SugarColumn(ColumnName = "ChildID", IsPrimaryKey = true)]
    public int ChildID { get; set; }

    /// <summary>
    /// 距离上级的层数
    /// </summary>
    [SugarColumn(ColumnName = "Depth")]
    public int Depth { get; set; }
}

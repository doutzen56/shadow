namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 积分日志表
/// </summary>
[SugarTable("usr_DelotteryLog")]
public class UserDelotteryLog
{
    /// <summary>
    /// 主键
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
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
    /// 积分
    /// </summary>
    [SugarColumn(ColumnName = "Point")]
    public decimal Point { get; set; }

    /// <summary>
    /// 余额
    /// </summary>
    [SugarColumn(ColumnName = "Balance")]
    public decimal Balance { get; set; }

    /// <summary>
    /// 积分类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 详细情况
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}

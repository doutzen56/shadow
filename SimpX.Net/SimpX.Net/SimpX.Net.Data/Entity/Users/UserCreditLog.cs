namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的积分帐变日志
/// </summary>
[SugarTable("usr_CreditLog")]
public class UserCreditLog
{
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
    /// 帐变类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 帐变关联ID
    /// </summary>
    [SugarColumn(ColumnName = "SourceID")]
    public string SourceID { get; set; }

    /// <summary>
    /// 帐变金额
    /// </summary>
    [SugarColumn(ColumnName = "Credit")]
    public decimal Credit { get; set; }

    /// <summary>
    /// 帐变之后的余额
    /// </summary>
    [SugarColumn(ColumnName = "Balance")]
    public decimal Balance { get; set; }

    /// <summary>
    /// 帐变备注
    /// </summary>
    [SugarColumn(ColumnName = "LogDesc")]
    public string Description { get; set; }
}

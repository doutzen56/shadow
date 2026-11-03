namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的资金日志,根据TableID做约束分区视图
/// </summary>
[SugarTable("MoneyLog")]
public class MoneyLog
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true)]
    public int ID { get; set; }

    /// <summary>
    /// 站点
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 帐变金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 当前的锁定金额
    /// </summary>
    [SugarColumn(ColumnName = "LockMoney")]
    public decimal LockMoney { get; set; }

    /// <summary>
    /// 帐变之后的金额（含锁定金额）
    /// </summary>
    [SugarColumn(ColumnName = "Balance")]
    public decimal Balance { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 资金类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 子类型
    /// </summary>
    [SugarColumn(ColumnName = "SubType")]
    public byte SubType { get; set; }

    /// <summary>
    /// 来源
    /// </summary>
    [SugarColumn(ColumnName = "SourceID")]
    public string SourceID { get; set; }

    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>
    /// 帐变备注
    /// </summary>
    [SugarColumn(ColumnName = "LogDesc")]
    public string Description { get; set; }

    /// <summary>
    /// 标记该记录属于哪个表
    /// </summary>
    [SugarColumn(ColumnName = "TableID")]
    public int TableID { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }
}

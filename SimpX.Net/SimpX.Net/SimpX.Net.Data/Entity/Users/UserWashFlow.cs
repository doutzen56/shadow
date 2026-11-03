namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 流水稽查
/// </summary>
[SugarTable("usr_WashFlow")]
public class UserWashFlow
{
    /// <summary>
    /// 检查编号
    /// </summary>
    [SugarColumn(ColumnName = "WashID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 打码量要求开始时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnName = "EndAt")]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// 入款的资金类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 入款的金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 当前的流水总额
    /// </summary>
    [SugarColumn(ColumnName = "BetMoney")]
    public decimal BetMoney { get; set; }

    /// <summary>
    /// 要求的打码量
    /// </summary>
    [SugarColumn(ColumnName = "Flow")]
    public decimal Flow { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }

    /// <summary>
    /// 已被处理的提现订单号
    /// </summary>
    [SugarColumn(ColumnName = "OrderID")]
    public int OrderID { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    [SugarColumn(ColumnName = "EventID")]
    public int EventID { get; set; }

    /// <summary>
    /// 手动加款ID
    /// </summary>
    [SugarColumn(ColumnName = "ReviewID")]
    public int ReviewID { get; set; }
}

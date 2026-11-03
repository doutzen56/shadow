namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 提现订单操作日志
/// </summary>
[SugarTable("usr_WithdrawOrderLog")]
public class WithdrawOrderLog
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 提现订单编号
    /// </summary>
    [SugarColumn(ColumnName = "OrderID")]
    public int OrderID { get; set; }

    /// <summary>
    /// 操作之后的状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 操作人
    /// </summary>
    [SugarColumn(ColumnName = "AdminID")]
    public int AdminID { get; set; }

    /// <summary>
    /// 操作IP
    /// </summary>
    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }
}

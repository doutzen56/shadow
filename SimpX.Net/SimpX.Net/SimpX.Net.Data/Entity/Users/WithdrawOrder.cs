namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 提现订单
/// </summary>
[SugarTable("usr_WithdrawOrder")]
public class WithdrawOrder
{
    /// <summary>
    /// 提现订单
    /// </summary>
    [SugarColumn(ColumnName = "OrderID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 提现帐号所属银行
    /// </summary>
    [SugarColumn(ColumnName = "AccountBank")]
    public int Bank { get; set; }

    /// <summary>
    /// 提现帐号名称
    /// </summary>
    [SugarColumn(ColumnName = "AccountName")]
    public string AccountName { get; set; }

    /// <summary>
    /// 提现帐号
    /// </summary>
    [SugarColumn(ColumnName = "AccountNumber")]
    public string AccountNumber { get; set; }

    /// <summary>
    /// 提现金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 提现订单状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 手续费
    /// </summary>
    [SugarColumn(ColumnName = "Fee")]
    public decimal Fee { get; set; }

    /// <summary>
    /// 实际到帐金额
    /// </summary>
    [SugarColumn(ColumnName = "Paid")]
    public decimal Paid { get; set; }

    /// <summary>
    /// 实际到帐的时间
    /// </summary>
    [SugarColumn(ColumnName = "PayAt")]
    public DateTime PayAt { get; set; }

    /// <summary>
    /// 提现付款渠道编号
    /// </summary>
    [SugarColumn(ColumnName = "ChannelID")]
    public int ChannelID { get; set; }

    /// <summary>
    /// 订单信息
    /// </summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }

    /// <summary>
    /// 标注信息(仅管理员可见）
    /// </summary>
    [SugarColumn(ColumnName = "Mark")]
    public string Mark { get; set; }

    /// <summary>
    /// 申請提款的IP來源
    /// </summary>
    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>
    /// 行政费用
    /// </summary>
    [SugarColumn(ColumnName = "Cost")]
    public decimal Cost { get; set; }

    /// <summary>
    /// 第三方订单号
    /// </summary>
    [SugarColumn(ColumnName = "SystemID")]
    public string SystemID { get; set; }

    /// <summary>
    /// 审核人编号
    /// </summary>
    [SugarColumn(ColumnName = "AdminID")]
    public int AdminID { get; set; }

    /// <summary>
    /// 出款人编号
    /// </summary>
    [SugarColumn(ColumnName = "CashierID")]
    public int CashierID { get; set; }

    /// <summary>
    /// 实际扣除币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 申请币种
    /// </summary>
    [SugarColumn(ColumnName = "FromCurrency")]
    public int FromCurrency { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    [SugarColumn(ColumnName = "AuditAt")]
    public DateTime AuditAt { get; set; }
}

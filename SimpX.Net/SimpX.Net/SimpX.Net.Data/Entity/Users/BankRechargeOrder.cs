namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 线下渠道转账申请入账
/// </summary>
[SugarTable("usr_BankRechargeOrder")]
public class BankRechargeOrder
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 充值渠道
    /// </summary>
    [SugarColumn(ColumnName = "PayID")]
    public int PayID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    [SugarColumn(ColumnName = "CheckAt")]
    public DateTime CheckAt { get; set; }

    /// <summary>
    /// 审核状态（待审核、审核失败、审核通过）
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 转账的名字
    /// </summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>
    /// 转账的账号
    /// </summary>
    [SugarColumn(ColumnName = "Account")]
    public string Account { get; set; }

    /// <summary>
    /// 提交的转账金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 实际入账金额
    /// </summary>
    [SugarColumn(ColumnName = "Paid")]
    public decimal Paid { get; set; }

    /// <summary>
    /// 对应的充值订单
    /// </summary>
    [SugarColumn(ColumnName = "OrderID")]
    public string OrderID { get; set; }

    /// <summary>
    /// 审核理由
    /// </summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }

    /// <summary>
    /// 审核操作人
    /// </summary>
    [SugarColumn(ColumnName = "AdminID")]
    public int AdminID { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 支付的订单号
    /// </summary>
    [SugarColumn(ColumnName = "PayOrderID")]
    public string PayOrderID { get; set; }

    /// <summary>
    /// 是否已经删除
    /// </summary>
    [SugarColumn(ColumnName = "IsDeleted")]
    public bool IsDeleted { get; set; }
}

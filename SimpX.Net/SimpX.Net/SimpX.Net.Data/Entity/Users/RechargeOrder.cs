namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 充值订单
/// </summary>
[SugarTable("usr_RechargeOrder")]
public class RechargeOrder
{
    /// <summary>
    /// 充值订单编号
    /// </summary>
    [SugarColumn(ColumnName = "OrderID", IsPrimaryKey = true)]
    public string ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 充值金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 充值手续费
    /// </summary>
    [SugarColumn(ColumnName = "Fee")]
    public decimal Fee { get; set; }

    /// <summary>
    /// 行政费用
    /// </summary>
    [SugarColumn(ColumnName = "Cost")]
    public decimal Cost { get; set; }

    /// <summary>
    /// 提交充值的时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 充值渠道
    /// </summary>
    [SugarColumn(ColumnName = "PayID")]
    public int PayID { get; set; }

    /// <summary>
    /// 是否到帐
    /// </summary>
    [SugarColumn(ColumnName = "IsPay")]
    public bool IsPay { get; set; }

    /// <summary>
    /// 到帐时间
    /// </summary>
    [SugarColumn(ColumnName = "PayAt")]
    public DateTime PayAt { get; set; }

    /// <summary>
    /// 实际到帐的金额
    /// </summary>
    [SugarColumn(ColumnName = "Paid")]
    public decimal Paid { get; set; }

    /// <summary>
    /// 网关编号
    /// </summary>
    [SugarColumn(ColumnName = "SystemID")]
    public string SystemID { get; set; }

    /// <summary>
    /// 审核人编号
    /// </summary>
    [SugarColumn(ColumnName = "AdminID")]
    public int AdminID { get; set; }

    /// <summary>
    /// 充值次数
    /// </summary>
    [SugarColumn(ColumnName = "Count")]
    public int Count { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }

    /// <summary>
    /// 订单充值会员所在IP(后台代充,默认: 0.0.0.0)
    /// </summary>
    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>
    /// 充值到账币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 充值币种
    /// </summary>
    [SugarColumn(ColumnName = "FromCurrency")]
    public int FromCurrency { get; set; }

    /// <summary>
    /// 是否检测失败
    /// </summary>
    [SugarColumn(ColumnName = "IsCheckFail")]
    public bool IsCheckFail { get; set; }

    /// <summary>
    /// 活动ID
    /// </summary>
    [SugarColumn(ColumnName = "ActiveID")]
    public string ActiveID { get; set; }

    /// <summary>
    /// 是否已经删除
    /// </summary>
    [SugarColumn(ColumnName = "IsDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 是否代客充值
    /// </summary>
    [SugarColumn(ColumnName = "IsProxyRecharge")]
    public bool IsProxyRecharge { get; set; }

    /// <summary>
    /// 存款账号
    /// </summary>
    [SugarColumn(ColumnName = "PayAccount")]
    public string PayAccount { get; set; }
}

namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 佣金发放日志
/// </summary>
[SugarTable("usr_CommissionLog")]
public class UserCommissionLog
{
    /// <summary>
    /// 佣金编号
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 所属的佣金规则表ID
    /// </summary>
    [SugarColumn(ColumnName = "CommID")]
    public int CommID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 结算佣金的开始日期
    /// </summary>
    [SugarColumn(ColumnName = "StartDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结算佣金的截至日期
    /// </summary>
    [SugarColumn(ColumnName = "EndDate")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 本期实际有效会员数
    /// </summary>
    [SugarColumn(ColumnName = "ValidUserCount")]
    public int ValidUserCount { get; set; }

    /// <summary>
    /// 充提手续费
    /// </summary>
    [SugarColumn(ColumnName = "RechargeWithdrawFee")]
    public decimal RechargeWithdrawFee { get; set; }

    /// <summary>
    /// 活动或红利
    /// </summary>
    [SugarColumn(ColumnName = "ActivityMoney")]
    public decimal ActivityMoney { get; set; }

    /// <summary>
    /// 佣金总金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 佣金记录创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    [SugarColumn(ColumnName = "CheckAt")]
    public DateTime CheckAt { get; set; }

    /// <summary>
    /// 佣金的详情
    /// </summary>
    [SugarColumn(ColumnName = "Detail")]
    public string Detail { get; set; }

    /// <summary>
    /// 佣金发放状态（是否已经发放、拒绝）
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }
}

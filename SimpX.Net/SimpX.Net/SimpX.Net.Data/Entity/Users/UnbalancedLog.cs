namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 未结算资金的帐变日志
/// </summary>
[SugarTable("usr_UnbalancedLog")]
public class UnbalancedLog
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 所属站点
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 来源编号（格式 游戏平台-流水号）
    /// </summary>
    [SugarColumn(ColumnName = "SourceID")]
    public string SourceID { get; set; }

    /// <summary>
    /// 结算状态（未结算、已结算）
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 本地操作的金额（未结算为正数,已结算为负数）
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 当前游戏中未结算的金额
    /// </summary>
    [SugarColumn(ColumnName = "Unbalanced")]
    public decimal Unbalanced { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [SugarColumn(ColumnName = "LogDesc")]
    public string Description { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }
}

namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 提现额度变化日志
/// </summary>
[SugarTable("usr_WithdrawCreditLog")]
public class WithdrawCreditLog
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 当前用户
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 当前的提现额度变化
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    [SugarColumn(ColumnName = "Withdraw")]
    public decimal Withdraw { get; set; }

    /// <summary>
    /// 信息产生时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }
}

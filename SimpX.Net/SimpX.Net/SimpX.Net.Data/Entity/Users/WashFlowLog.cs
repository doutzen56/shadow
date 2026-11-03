namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 打码量日志
/// </summary>
[SugarTable("usr_WashFlowLog")]
public class WashFlowLog
{
    /// <summary>
    /// 日志编号
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 流水金额,如果为负数则为打码量要求
    /// </summary>
    [SugarColumn(ColumnName = "BetMoney")]
    public decimal BetMoney { get; set; }

    /// <summary>
    /// 实际的打码量
    /// </summary>
    [SugarColumn(ColumnName = "BetAmount")]
    public decimal BetAmount { get; set; }

    /// <summary>
    /// 剩余的流水要求
    /// </summary>
    [SugarColumn(ColumnName = "WashFlow")]
    public decimal WashFlow { get; set; }

    /// <summary>
    /// 打码时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [SugarColumn(ColumnName = "LogDesc")]
    public string Description { get; set; }

    /// <summary>
    /// 流水ID
    /// </summary>
    [SugarColumn(ColumnName = "WashID")]
    public int WashID { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// EsID
    /// </summary>
    [SugarColumn(ColumnName = "EsID")]
    public string EsID { get; set; }
}

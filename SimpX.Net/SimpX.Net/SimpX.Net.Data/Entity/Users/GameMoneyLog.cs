namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 游戏的资金日志
/// </summary>
[SugarTable("usr_GameMoneyLog")]
public class GameMoneyLog
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 游戏类型
    /// </summary>
    [SugarColumn(ColumnName = "Game")]
    public int Game { get; set; }

    /// <summary>
    /// 资金类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 流水号
    /// </summary>
    [SugarColumn(ColumnName = "SourceID")]
    public string SourceID { get; set; }

    /// <summary>
    /// 帐变金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 资金帐变产生的时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [SugarColumn(ColumnName = "LogDesc")]
    public string Description { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public string Currency { get; set; }
}

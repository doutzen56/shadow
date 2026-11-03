namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 流水详情
/// </summary>
[SugarTable("usr_WashDetail")]
public class UserWashDetail
{
    [SugarColumn(ColumnName = "WashID", IsPrimaryKey = true)]
    public int WashID { get; set; }

    /// <summary>
    /// 游戏类型
    /// </summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>
    /// 游戏类型
    /// </summary>
    [SugarColumn(ColumnName = "Category", IsPrimaryKey = true)]
    public int Category { get; set; }

    /// <summary>
    /// 已打的流水金额(实际）
    /// </summary>
    [SugarColumn(ColumnName = "BetMoney")]
    public decimal BetMoney { get; set; }

    /// <summary>
    /// 按照流水要求换算之后的要求
    /// </summary>
    [SugarColumn(ColumnName = "BetAmount")]
    public decimal BetAmount { get; set; }
}

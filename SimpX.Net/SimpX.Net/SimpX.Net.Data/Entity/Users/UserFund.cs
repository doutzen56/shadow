namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的资金
/// </summary>
[SugarTable("usr_Fund")]
public class UserFund
{
    /// <summary>
    /// 所属站点
    /// </summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>
    /// 所属用户
    /// </summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency", IsPrimaryKey = true)]
    public int Currency { get; set; }

    /// <summary>
    /// 可用余额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 被锁定金额
    /// </summary>
    [SugarColumn(ColumnName = "LockMoney")]
    public decimal LockMoney { get; set; }

    /// <summary>
    /// 游戏余额汇总
    /// </summary>
    [SugarColumn(ColumnName = "GameMoney")]
    public decimal GameMoney { get; set; }

    /// <summary>
    /// 当前游戏中未结算的金额
    /// </summary>
    [SugarColumn(ColumnName = "Unbalanced")]
    public decimal Unbalanced { get; set; }
}

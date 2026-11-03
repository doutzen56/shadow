namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的资金
/// </summary>
[SugarTable("usr_Money")]
public class UserMoney
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
    /// 总金额
    /// </summary>
    [SugarColumn(ColumnName = "TotalMoney")]
    public decimal TotalMoney { get; set; }

    /// <summary>
    /// 被锁定金额
    /// </summary>
    [SugarColumn(ColumnName = "LockMoney")]
    public decimal LockMoney { get; set; }

    /// <summary>
    /// 金币 --不在使用 变成user的积分使用
    /// </summary>
    [SugarColumn(ColumnName = "Coin")]
    public decimal Coin { get; set; }

    /// <summary>
    /// 积分
    /// </summary>
    [SugarColumn(ColumnName = "Credit")]
    public decimal Credit { get; set; }

    /// <summary>
    /// 经验值
    /// </summary>
    [SugarColumn(ColumnName = "Exper")]
    public int Exper { get; set; }

    /// <summary>
    /// 流水要求（为0的时候才允许提现）
    /// </summary>
    [SugarColumn(ColumnName = "WashFlow")]
    public decimal WashFlow { get; set; }

    /// <summary>
    /// 当前游戏中未结算的金额
    /// </summary>
    [SugarColumn(ColumnName = "Unbalanced")]
    public decimal Unbalanced { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency", IsPrimaryKey = true)]
    public int Currency { get; set; }

    /// <summary>
    /// 是否为基础币种
    /// </summary>
    [SugarColumn(ColumnName = "IsBase")]
    public bool IsBase { get; set; }
}

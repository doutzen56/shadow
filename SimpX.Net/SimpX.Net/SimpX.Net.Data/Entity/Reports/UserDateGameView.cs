using SqlSugar;

namespace SimpX.Net.Data.Entity.Reports;

/// <summary>
/// 日结游戏报表视图（只读；表/视图 rpt_UserDateGameView）。
/// </summary>
[SugarTable("rpt_UserDateGameView")]
public class UserDateGameView
{
    /// <summary>游戏类型。</summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>日期。</summary>
    [SugarColumn(ColumnName = "Date", IsPrimaryKey = true)]
    public DateTime Date { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>会员。</summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>游戏分类。</summary>
    [SugarColumn(ColumnName = "Category", IsPrimaryKey = true)]
    public int Category { get; set; }

    /// <summary>投注金额。</summary>
    [SugarColumn(ColumnName = "BetMoney")]
    public decimal BetMoney { get; set; }

    /// <summary>有效投注。</summary>
    [SugarColumn(ColumnName = "BetAmount")]
    public decimal BetAmount { get; set; }

    /// <summary>盈亏。</summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>单量。</summary>
    [SugarColumn(ColumnName = "OrderCount")]
    public int OrderCount { get; set; }

    /// <summary>币种。</summary>
    [SugarColumn(ColumnName = "Currency", IsPrimaryKey = true)]
    public int Currency { get; set; }

    /// <summary>时区类型：1 北京 / 2 美东 / 3 GMT-0。</summary>
    [SugarColumn(ColumnName = "TimeType", IsPrimaryKey = true)]
    public int TimeType { get; set; }

    /// <summary>JackPot。</summary>
    [SugarColumn(ColumnName = "JackPot")]
    public decimal JackPot { get; set; }

    /// <summary>JackPot 累计。</summary>
    [SugarColumn(ColumnName = "JackPotCount")]
    public decimal JackPotCount { get; set; }
}

using SqlSugar;

namespace SimpX.Net.Data.Entity.Reports;

/// <summary>对战日游戏汇总（rpt_UserDateGameBattle）。</summary>
[SugarTable("rpt_UserDateGameBattle")]
public class UserDateGameBattle
{
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "Date", IsPrimaryKey = true)]
    public DateTime Date { get; set; }

    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    [SugarColumn(ColumnName = "Category", IsPrimaryKey = true)]
    public int Category { get; set; }

    /// <summary>有效投注（库列 BetAmount）。</summary>
    [SugarColumn(ColumnName = "BetAmount")]
    public decimal BetMoney { get; set; }

    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    [SugarColumn(ColumnName = "Currency", IsPrimaryKey = true)]
    public int Currency { get; set; }

    [SugarColumn(ColumnName = "OrderCount")]
    public int OrderCount { get; set; }
}

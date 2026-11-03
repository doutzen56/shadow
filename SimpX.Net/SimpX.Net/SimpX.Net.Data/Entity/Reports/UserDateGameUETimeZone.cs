using SqlSugar;

namespace SimpX.Net.Data.Entity.Reports;

/// <summary>美东/UTC 时区用户日游戏汇总（rpt_UserDateGame_UETimeZone）。</summary>
[SugarTable("rpt_UserDateGame_UETimeZone")]
public class UserDateGameUETimeZone
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

    /// <summary>时区类型：EST=2 / UTC=3（对齐旧 ToTimeType）。</summary>
    [SugarColumn(ColumnName = "TimeType", IsPrimaryKey = true)]
    public int TimeType { get; set; }

    [SugarColumn(ColumnName = "OrderCount")]
    public int OrderCount { get; set; }
}

using SqlSugar;

namespace SimpX.Net.Data.Entity.Reports;

/// <summary>
/// 会员按日游戏投注汇总（只读；改初始等级时用来判断是否新会员）。
/// </summary>
[SugarTable("rpt_UserDateGame")]
public class UserDateGame
{
    /// <summary>站点编号。</summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>会员编号。</summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>业务日。</summary>
    [SugarColumn(ColumnName = "Date", IsPrimaryKey = true)]
    public DateTime Date { get; set; }

    /// <summary>游戏类型。</summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>游戏种类。</summary>
    [SugarColumn(ColumnName = "Category", IsPrimaryKey = true)]
    public int Category { get; set; }

    /// <summary>投注金额（库列 BetAmount；属性名沿用本仓 BetMoney）。</summary>
    [SugarColumn(ColumnName = "BetAmount")]
    public decimal BetMoney { get; set; }

    /// <summary>盈亏金额（平台视角）。</summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>币种。</summary>
    [SugarColumn(ColumnName = "Currency", IsPrimaryKey = true)]
    public int Currency { get; set; }
}

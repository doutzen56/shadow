using SqlSugar;

namespace SimpX.Net.Data.Entity.Reports;

/// <summary>
/// 会员按日资金类型统计（只读；rpt_UserDateMoney）。
/// </summary>
[SugarTable("rpt_UserDateMoney")]
public class UserDateMoney
{
    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>会员。</summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>业务日。</summary>
    [SugarColumn(ColumnName = "Date", IsPrimaryKey = true)]
    public DateTime Date { get; set; }

    /// <summary>资金类型。</summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>金额。</summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>笔数。</summary>
    [SugarColumn(ColumnName = "Count")]
    public int Count { get; set; }

    /// <summary>币种。</summary>
    [SugarColumn(ColumnName = "Currency", IsPrimaryKey = true)]
    public int Currency { get; set; }
}

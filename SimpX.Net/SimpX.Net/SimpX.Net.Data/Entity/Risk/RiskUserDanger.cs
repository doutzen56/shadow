using SqlSugar;

namespace SimpX.Net.Data.Entity.Risk;

/// <summary>
/// 异常/风险会员记录（risk_UserDanger）。
/// </summary>
[SugarTable("risk_UserDanger")]
public class RiskUserDanger
{
    /// <summary>自增编号。</summary>
    [SugarColumn(ColumnName = "RiskID", IsIdentity = true)]
    public int RiskID { get; set; }

    /// <summary>业务日。</summary>
    [SugarColumn(ColumnName = "Date", IsPrimaryKey = true)]
    public DateTime Date { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>会员。</summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>摘要说明。</summary>
    [SugarColumn(ColumnName = "Context")]
    public string Context { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>风险类型（逗号分隔编号）。</summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public string Type { get; set; }

    /// <summary>详情 JSON。</summary>
    [SugarColumn(ColumnName = "Detail")]
    public string Detail { get; set; }
}

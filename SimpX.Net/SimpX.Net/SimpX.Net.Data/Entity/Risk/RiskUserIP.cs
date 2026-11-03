// 由旧版 risk_UserIP 映射；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Risk;

/// <summary>
/// 会员 IP 操作记录（同 IP 查询用）。
/// </summary>
[SugarTable("risk_UserIP")]
public class RiskUserIP
{
    /// <summary>自增编号。</summary>
    [SugarColumn(ColumnName = "RiskID", IsIdentity = true)]
    public int RiskID { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>会员。</summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>IP。</summary>
    [SugarColumn(ColumnName = "IP", IsPrimaryKey = true)]
    public string IP { get; set; }

    /// <summary>日期。</summary>
    [SugarColumn(ColumnName = "Date", IsPrimaryKey = true)]
    public DateTime Date { get; set; }
}

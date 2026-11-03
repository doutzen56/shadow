// 由旧版 risk_UserDevice 映射；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Risk;

/// <summary>
/// 会员设备操作记录（同设备查询用）。
/// </summary>
[SugarTable("risk_UserDevice")]
public class RiskUserDevice
{
    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>会员。</summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>设备号。</summary>
    [SugarColumn(ColumnName = "DeviceID", IsPrimaryKey = true)]
    public string DeviceID { get; set; }

    /// <summary>日期。</summary>
    [SugarColumn(ColumnName = "Date", IsPrimaryKey = true)]
    public DateTime Date { get; set; }
}

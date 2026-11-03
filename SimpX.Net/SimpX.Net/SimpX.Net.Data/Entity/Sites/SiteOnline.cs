// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点在线会员
/// </summary>
[SugarTable("site_Online")]
public class SiteOnline
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "OnlineTime")]
    public DateTime OnlineTime { get; set; }

    [SugarColumn(ColumnName = "Count")]
    public int Count { get; set; }

    [SugarColumn(ColumnName = "RechargeCount")]
    public int RechargeCount { get; set; }

    /// <summary>
    /// PC端在线会员
    /// </summary>
    [SugarColumn(ColumnName = "PC")]
    public int PC { get; set; }

    /// <summary>
    /// 移动端在线会员
    /// </summary>
    [SugarColumn(ColumnName = "Mobile")]
    public int Mobile { get; set; }

    /// <summary>
    /// 安卓端在线会员
    /// </summary>
    [SugarColumn(ColumnName = "Android")]
    public int Android { get; set; }

    /// <summary>
    /// IOS在线会员
    /// </summary>
    [SugarColumn(ColumnName = "IOS")]
    public int IOS { get; set; }
}

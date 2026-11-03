// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点主机映射表
/// </summary>
[SugarTable("site_Host")]
public class SiteHost
{
    /// <summary>
    /// 主机ID
    /// </summary>
    [SugarColumn(ColumnName = "HostID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 主机名称
    /// </summary>
    [SugarColumn(ColumnName = "Host")]
    public string Host { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "IsEnable")]
    public bool IsEnable { get; set; }
}

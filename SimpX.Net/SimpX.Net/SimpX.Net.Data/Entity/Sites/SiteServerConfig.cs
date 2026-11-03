// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的请求回应规则
/// </summary>
[SugarTable("site_ServerConfig")]
public class SiteServerConfig
{
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>
    /// 请求规则
    /// </summary>
    [SugarColumn(ColumnName = "Request")]
    public string Request { get; set; }

    /// <summary>
    /// 回应规则
    /// </summary>
    [SugarColumn(ColumnName = "Response")]
    public string Response { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [SugarColumn(ColumnName = "PublishAt")]
    public DateTime PublishAt { get; set; }
}

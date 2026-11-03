// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的自定义视图配置
/// </summary>
[SugarTable("site_ViewConfig")]
public class SiteViewConfig
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "ViewID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 视图名字
    /// </summary>
    [SugarColumn(ColumnName = "ViewName")]
    public string Name { get; set; }

    /// <summary>
    /// 要匹配到的路径
    /// </summary>
    [SugarColumn(ColumnName = "Path")]
    public string Path { get; set; }

    /// <summary>
    /// 要输出至的路径（如果不填写则输出到同名的站点路径下）
    /// </summary>
    [SugarColumn(ColumnName = "Target")]
    public string Target { get; set; }
}

// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 新闻栏目
/// </summary>
[SugarTable("site_NewsColumn")]
public class NewsColumn
{
    /// <summary>
    /// 栏目分类
    /// </summary>
    [SugarColumn(ColumnName = "ColID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 栏目名称
    /// </summary>
    [SugarColumn(ColumnName = "ColName")]
    public string Name { get; set; }

    /// <summary>
    /// 栏目排序（从大到小）
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 栏目代码
    /// </summary>
    [SugarColumn(ColumnName = "Code")]
    public string Code { get; set; }
}

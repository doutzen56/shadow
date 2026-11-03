// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点帮助
/// </summary>
[SugarTable("site_Language")]
public class SiteLanguage
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "LangID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 存储的类型
    /// </summary>
    [SugarColumn(ColumnName = "Category")]
    public int Category { get; set; }

    /// <summary>
    /// 语言类型
    /// </summary>
    [SugarColumn(ColumnName = "Language")]
    public int Language { get; set; }

    /// <summary>
    /// 存储类型对应的表的Id
    /// </summary>
    [SugarColumn(ColumnName = "ExpandID")]
    public int ExpandID { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }
}

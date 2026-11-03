// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 帮助栏目
/// </summary>
[SugarTable("site_HelpColumn")]
public class HelpColumn
{
    [SugarColumn(ColumnName = "ColID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 栏目名
    /// </summary>
    [SugarColumn(ColumnName = "ColName")]
    public string Name { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 下属文章数量
    /// </summary>
    [SugarColumn(ColumnName = "Count")]
    public int Count { get; set; }

    /// <summary>
    /// 排序值（从大到小）
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 自定义链接
    /// </summary>
    [SugarColumn(ColumnName = "Url")]
    public string Url { get; set; }

    /// <summary>
    /// 是否在底部显示
    /// </summary>
    [SugarColumn(ColumnName = "IsFooter")]
    public bool IsFooter { get; set; }
}

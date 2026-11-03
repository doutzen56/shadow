// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 活动分类
/// </summary>
[SugarTable("site_EventColumn")]
public class SiteEventColumn
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [SugarColumn(ColumnName = "ColID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [SugarColumn(ColumnName = "ColName")]
    public string Name { get; set; }

    /// <summary>
    /// 排序值（从大到小）
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 活动数量
    /// </summary>
    [SugarColumn(ColumnName = "EventCount")]
    public int EventCount { get; set; }
}

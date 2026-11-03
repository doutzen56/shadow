// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的模板设定
/// </summary>
[SugarTable("site_TemplateConfig")]
public class SiteTemplateConfig
{
    /// <summary>
    /// 模板编号
    /// </summary>
    [SugarColumn(ColumnName = "TemplateID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 可设置的对象（关联 sys_TemplateConfig）
    /// </summary>
    [SugarColumn(ColumnName = "ConfigID")]
    public int ConfigID { get; set; }

    /// <summary>
    /// 设定内容（JSON格式）
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string Setting { get; set; }
}

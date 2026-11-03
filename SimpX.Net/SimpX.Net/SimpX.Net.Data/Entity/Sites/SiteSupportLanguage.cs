// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点支持语言
/// </summary>
[SugarTable("Site_SupportLanguage")]
public class SiteSupportLanguage
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 语言类型
    /// </summary>
    [SugarColumn(ColumnName = "Language")]
    public int Language { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public int Sort { get; set; }

    /// <summary>
    /// 系统是否给商户开启
    /// </summary>
    [SugarColumn(ColumnName = "SysOpen")]
    public bool SysOpen { get; set; }
}

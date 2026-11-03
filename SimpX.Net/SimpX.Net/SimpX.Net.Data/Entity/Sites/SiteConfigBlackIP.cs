// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 黑名单设置
/// </summary>
[SugarTable("site_Config_BlackIP")]
public class SiteConfigBlackIP
{
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>
    /// 起始IP
    /// </summary>
    [SugarColumn(ColumnName = "StartIP")]
    public long StartIP { get; set; }

    /// <summary>
    /// 结束IP
    /// </summary>
    [SugarColumn(ColumnName = "EndIP")]
    public long EndIP { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }
}

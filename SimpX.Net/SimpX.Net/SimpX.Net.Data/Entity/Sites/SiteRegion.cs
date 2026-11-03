// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的自定义区域
/// </summary>
[SugarTable("site_Region")]
public class SiteRegion
{
    /// <summary>
    /// 区域编号
    /// </summary>
    [SugarColumn(ColumnName = "RegionID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 区域的名字
    /// </summary>
    [SugarColumn(ColumnName = "RegionName")]
    public string Name { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }

    /// <summary>
    /// 区域内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>
    /// 区域类型（Html、轮播图、模板控件）
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 是否站点动画 0系统 1站点
    /// </summary>
    [SugarColumn(ColumnName = "IsSiteGif")]
    public bool IsSiteGif { get; set; }

    /// <summary>
    /// 系统动画类型 1无 2光照 3呼吸 4翻转
    /// </summary>
    [SugarColumn(ColumnName = "GifType")]
    public byte GifType { get; set; }
}

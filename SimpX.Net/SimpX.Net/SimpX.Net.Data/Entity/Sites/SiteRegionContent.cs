// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的自定义区域
/// </summary>
[SugarTable("site_RegionContent")]
public class SiteRegionContent
{
    /// <summary>
    /// 标识
    /// </summary>
    [SugarColumn(ColumnName = "AdId", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 区域ID
    /// </summary>
    [SugarColumn(ColumnName = "RegionID")]
    public int RegionID { get; set; }

    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "AdNo")]
    public string AdNo { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(ColumnName = "Title")]
    public string Title { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }

    /// <summary>
    /// 链接
    /// </summary>
    [SugarColumn(ColumnName = "Url")]
    public string Url { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public int Sort { get; set; }

    /// <summary>
    /// 图片
    /// </summary>
    [SugarColumn(ColumnName = "Pic")]
    public string Pic { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    [SugarColumn(ColumnName = "IsShow")]
    public bool IsShow { get; set; }

    /// <summary>
    /// 点击次数
    /// </summary>
    [SugarColumn(ColumnName = "ClickCount")]
    public int ClickCount { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    [SugarColumn(ColumnName = "ExpireAt")]
    public DateTime ExpireAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 链接类型
    /// </summary>
    [SugarColumn(ColumnName = "UrlType")]
    public int Type { get; set; }
}

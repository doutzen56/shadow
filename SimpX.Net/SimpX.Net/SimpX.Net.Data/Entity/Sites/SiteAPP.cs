// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// APP包
/// </summary>
[SugarTable("site_APP")]
public class SiteAPP
{
    [SugarColumn(ColumnName = "APPID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 平台类型（安卓/苹果）
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 文件下载路径
    /// </summary>
    [SugarColumn(ColumnName = "FilePath")]
    public string FilePath { get; set; }

    /// <summary>
    /// 文件大小
    /// </summary>
    [SugarColumn(ColumnName = "FileSize")]
    public int FileSize { get; set; }

    [SugarColumn(ColumnName = "Icon")]
    public string Icon { get; set; }

    [SugarColumn(ColumnName = "Version")]
    public string Version { get; set; }

    /// <summary>
    /// APP名字
    /// </summary>
    [SugarColumn(ColumnName = "APPName")]
    public string Name { get; set; }

    /// <summary>
    /// APP名字
    /// </summary>
    [SugarColumn(ColumnName = "BundleID")]
    public string BundleID { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }

    /// <summary>
    /// 热更新包路径
    /// </summary>
    [SugarColumn(ColumnName = "WGT")]
    public string WGT { get; set; }

    /// <summary>
    /// 热更新包版本号
    /// </summary>
    [SugarColumn(ColumnName = "WGTVersion")]
    public string WGTVersion { get; set; }

    /// <summary>
    /// 热更新包的更新时间
    /// </summary>
    [SugarColumn(ColumnName = "WGTAt")]
    public DateTime WGTAt { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    [SugarColumn(ColumnName = "DownCount")]
    public int DownCount { get; set; }
}

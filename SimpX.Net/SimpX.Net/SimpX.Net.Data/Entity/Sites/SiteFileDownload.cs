// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 报表文件下载
/// </summary>
[SugarTable("site_FileDownload")]
public class SiteFileDownload
{
    [SugarColumn(ColumnName = "FileID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    [SugarColumn(ColumnName = "FileName")]
    public string FileName { get; set; }

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

    /// <summary>
    /// 下载赔率
    /// </summary>
    [SugarColumn(ColumnName = "Frequency")]
    public int Frequency { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}

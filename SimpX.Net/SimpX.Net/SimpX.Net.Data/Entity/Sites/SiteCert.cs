// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点证书
/// </summary>
[SugarTable("site_Cert")]
public class SiteCert
{
    [SugarColumn(ColumnName = "CertID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 证书的更新时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 证书名字（域名）
    /// </summary>
    [SugarColumn(ColumnName = "CertName")]
    public string Name { get; set; }

    /// <summary>
    /// 证书包含的域名（逗号隔开）
    /// </summary>
    [SugarColumn(ColumnName = "Domain")]
    public string Domain { get; set; }

    /// <summary>
    /// 证书内容
    /// </summary>
    [SugarColumn(ColumnName = "Cert")]
    public string Cert { get; set; }

    /// <summary>
    /// 证书密钥
    /// </summary>
    [SugarColumn(ColumnName = "Key")]
    public string Key { get; set; }

    /// <summary>
    /// 到期日期
    /// </summary>
    [SugarColumn(ColumnName = "ExpireAt")]
    public DateTime ExpireAt { get; set; }
}

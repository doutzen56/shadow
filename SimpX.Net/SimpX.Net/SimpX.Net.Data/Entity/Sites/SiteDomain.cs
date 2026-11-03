// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的域名绑定
/// </summary>
[SugarTable("site_Domain")]
public class SiteDomain
{
    /// <summary>
    /// 域名编号
    /// </summary>
    [SugarColumn(ColumnName = "DomainID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 域名（不含http部分）
    /// </summary>
    [SugarColumn(ColumnName = "Domain")]
    public string Domain { get; set; }

    /// <summary>
    /// 所属站点
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 所属代理
    /// </summary>
    [SugarColumn(ColumnName = "AgentName")]
    public string AgentName { get; set; }

    /// <summary>
    /// 权重
    /// </summary>
    [SugarColumn(ColumnName = "Weights")]
    public short Weights { get; set; }

    /// <summary>
    /// 是否对外显示该域名
    /// </summary>
    [SugarColumn(ColumnName = "IsVisible")]
    public bool IsVisible { get; set; }

    /// <summary>
    /// 是否启用https
    /// </summary>
    [SugarColumn(ColumnName = "IsSSL")]
    public bool IsSSL { get; set; }

    /// <summary>
    /// 是否APP专用
    /// </summary>
    [SugarColumn(ColumnName = "IsAPP")]
    public bool IsAPP { get; set; }

    /// <summary>
    /// 是否代理
    /// </summary>
    [SugarColumn(ColumnName = "IsSingle")]
    public bool IsSingle { get; set; }

    /// <summary>
    /// 测速站模板
    /// </summary>
    [SugarColumn(ColumnName = "Speed")]
    public int Speed { get; set; }
}

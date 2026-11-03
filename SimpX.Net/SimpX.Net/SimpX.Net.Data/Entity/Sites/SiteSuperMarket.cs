// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点超商
/// </summary>
[SugarTable("Site_SuperMarket")]
public class SiteSuperMarket
{
    /// <summary>
    /// 自增编号
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 超商编号
    /// </summary>
    [SugarColumn(ColumnName = "MarketID")]
    public string MarketID { get; set; }

    /// <summary>
    /// 超商名字
    /// </summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>
    /// 超商县
    /// </summary>
    [SugarColumn(ColumnName = "County")]
    public string County { get; set; }

    /// <summary>
    /// 超商区
    /// </summary>
    [SugarColumn(ColumnName = "Region")]
    public string Region { get; set; }
}

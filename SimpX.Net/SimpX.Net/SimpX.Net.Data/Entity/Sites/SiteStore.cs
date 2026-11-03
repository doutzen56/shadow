// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 超商门店号
/// </summary>
[SugarTable("site_Store")]
public class SiteStore
{
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 超商编号
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 门店号
    /// </summary>
    [SugarColumn(ColumnName = "StoreID")]
    public int StoreID { get; set; }

    /// <summary>
    /// 门店名称
    /// </summary>
    [SugarColumn(ColumnName = "StoreName")]
    public string StoreName { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    [SugarColumn(ColumnName = "City")]
    public string City { get; set; }

    /// <summary>
    /// 区县
    /// </summary>
    [SugarColumn(ColumnName = "District")]
    public string District { get; set; }

    /// <summary>
    /// 详细地址
    /// </summary>
    [SugarColumn(ColumnName = "Adress")]
    public string Adress { get; set; }

    /// <summary>
    /// 创建日期
    /// </summary>
    [SugarColumn(ColumnName = "CreatAt")]
    public DateTime CreatAt { get; set; }
}

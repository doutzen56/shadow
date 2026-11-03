using SqlSugar;

namespace SimpX.Net.Data.Entity.Shop;

/// <summary>
/// 积分商城分类（shop_ProductCategory）。
/// </summary>
[SugarTable("shop_ProductCategory")]
public class ShopProductCategory
{
    /// <summary>分类编号。</summary>
    [SugarColumn(ColumnName = "TypeID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>父级。</summary>
    [SugarColumn(ColumnName = "ParentID")]
    public int ParentID { get; set; }

    /// <summary>名称。</summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>排序。</summary>
    [SugarColumn(ColumnName = "Sort")]
    public int Sort { get; set; }
}

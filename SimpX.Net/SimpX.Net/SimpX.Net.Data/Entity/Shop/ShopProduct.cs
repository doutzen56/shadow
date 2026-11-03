using SqlSugar;

namespace SimpX.Net.Data.Entity.Shop;

/// <summary>
/// 积分商城商品（shop_Product）。
/// </summary>
[SugarTable("shop_Product")]
public class ShopProduct
{
    /// <summary>商品编号。</summary>
    [SugarColumn(ColumnName = "ProductID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>名称。</summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>图片。</summary>
    [SugarColumn(ColumnName = "Pic")]
    public string Pic { get; set; }

    /// <summary>分类。</summary>
    [SugarColumn(ColumnName = "TypeID")]
    public int TypeID { get; set; }

    /// <summary>分类名。</summary>
    [SugarColumn(ColumnName = "TypeName")]
    public string TypeName { get; set; }

    /// <summary>标价原文。</summary>
    [SugarColumn(ColumnName = "Price")]
    public string Price { get; set; }

    /// <summary>所需积分。</summary>
    [SugarColumn(ColumnName = "Points")]
    public int Points { get; set; }

    /// <summary>简述。</summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }

    /// <summary>详情。</summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>参数原文。</summary>
    [SugarColumn(ColumnName = "Settings")]
    public string Settings { get; set; }

    /// <summary>状态。</summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>更新时间。</summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }

    /// <summary>点击。</summary>
    [SugarColumn(ColumnName = "ClickCount")]
    public int ClickCount { get; set; }

    /// <summary>库存。</summary>
    [SugarColumn(ColumnName = "Stock")]
    public decimal Stock { get; set; }

    /// <summary>流水倍数。</summary>
    [SugarColumn(ColumnName = "WashFlow")]
    public decimal WashFlow { get; set; }

    /// <summary>是否自动发放。</summary>
    [SugarColumn(ColumnName = "IsAuto")]
    public bool IsAuto { get; set; }

    /// <summary>单位。</summary>
    [SugarColumn(ColumnName = "Unit")]
    public string Unit { get; set; }

    /// <summary>排序。</summary>
    [SugarColumn(ColumnName = "Sort")]
    public int Sort { get; set; }
}

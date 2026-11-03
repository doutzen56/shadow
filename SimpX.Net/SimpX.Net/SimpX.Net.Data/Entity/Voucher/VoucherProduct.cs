using SqlSugar;

namespace SimpX.Net.Data.Entity.Voucher;

/// <summary>
/// 彩金兑换产品（voucher_Product）。
/// </summary>
[SugarTable("voucher_Product")]
public class VoucherProduct
{
    /// <summary>产品编号。</summary>
    [SugarColumn(ColumnName = "ProductID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>兑换码/名称。</summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>图片。</summary>
    [SugarColumn(ColumnName = "Pic")]
    public string Pic { get; set; }

    /// <summary>币种。</summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>开始。</summary>
    [SugarColumn(ColumnName = "StartAt")]
    public DateTime StartAt { get; set; }

    /// <summary>价格。</summary>
    [SugarColumn(ColumnName = "Price")]
    public decimal Price { get; set; }

    /// <summary>积分。</summary>
    [SugarColumn(ColumnName = "Point")]
    public int Point { get; set; }

    /// <summary>结束。</summary>
    [SugarColumn(ColumnName = "EndAt")]
    public DateTime EndAt { get; set; }

    /// <summary>简述。</summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }

    /// <summary>详情。</summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>创建。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>参数。</summary>
    [SugarColumn(ColumnName = "Settings")]
    public string Settings { get; set; }

    /// <summary>状态。</summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>更新。</summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }

    /// <summary>点击。</summary>
    [SugarColumn(ColumnName = "ClickCount")]
    public int ClickCount { get; set; }

    /// <summary>库存。</summary>
    [SugarColumn(ColumnName = "Stock")]
    public int Stock { get; set; }

    /// <summary>最大库存。</summary>
    [SugarColumn(ColumnName = "MaxStock")]
    public int MaxStock { get; set; }

    /// <summary>流水倍数。</summary>
    [SugarColumn(ColumnName = "WashFlow")]
    public decimal WashFlow { get; set; }
}

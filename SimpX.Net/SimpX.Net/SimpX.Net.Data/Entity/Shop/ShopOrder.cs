using SqlSugar;

namespace SimpX.Net.Data.Entity.Shop;

/// <summary>
/// 积分商城订单（shop_Order）。
/// </summary>
[SugarTable("shop_Order")]
public class ShopOrder
{
    /// <summary>订单编号。</summary>
    [SugarColumn(ColumnName = "OrderID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>下单时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>状态。</summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>会员。</summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [SugarColumn(ColumnName = "UserName")]
    public string UserName { get; set; }

    /// <summary>备注。</summary>
    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }

    /// <summary>活动来源。</summary>
    [SugarColumn(ColumnName = "EventID")]
    public int EventID { get; set; }

    /// <summary>价值。</summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>商品名。</summary>
    [SugarColumn(ColumnName = "ProductName")]
    public string ProductName { get; set; }

    /// <summary>商品编号。</summary>
    [SugarColumn(ColumnName = "ProductId")]
    public int ProductId { get; set; }
}

// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 充值订单提交数据内容（DEBUG调试）
/// </summary>
[SugarTable("site_WithdrawLog")]
public class SiteWithdrawLog
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "OrderID")]
    public string OrderID { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 要记录的日志内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }
}

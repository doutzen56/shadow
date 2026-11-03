// 由旧版系统表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 系统市场汇率（总后台货币汇率）。
/// </summary>
[SugarTable("sys_Exchange")]
public class SystemExchange
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 基本币种
    /// </summary>
    [SugarColumn(ColumnName = "BaseCurrency")]
    public int BaseCurrency { get; set; }

    /// <summary>
    /// 兑换币种
    /// </summary>
    [SugarColumn(ColumnName = "ExchangeCurrency")]
    public int ExchangeCurrency { get; set; }

    /// <summary>
    /// 市场价格
    /// </summary>
    [SugarColumn(ColumnName = "MarketPrice")]
    public decimal MarketPrice { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public bool Status { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }
}

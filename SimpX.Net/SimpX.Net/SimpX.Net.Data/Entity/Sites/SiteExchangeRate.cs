// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点汇率设置
/// </summary>
[SugarTable("Site_ExchangeRate")]
public class SiteExchangeRate
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

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
    /// 最终点数
    /// </summary>
    [SugarColumn(ColumnName = "Point")]
    public decimal Point { get; set; }

    /// <summary>
    /// 交易模式
    /// </summary>
    [SugarColumn(ColumnName = "ExchangeMode")]
    public int ExchangeMode { get; set; }

    /// <summary>
    /// 参数
    /// </summary>
    [SugarColumn(ColumnName = "Parameter")]
    public decimal Parameter { get; set; }

    /// <summary>
    /// 是否为基本币
    /// </summary>
    [SugarColumn(ColumnName = "IsBase")]
    public bool IsBase { get; set; }

    /// <summary>
    /// 手续费模式
    /// </summary>
    [SugarColumn(ColumnName = "ExchangeFee")]
    public int ExchangeFee { get; set; }

    /// <summary>
    /// 手续费%
    /// </summary>
    [SugarColumn(ColumnName = "ExchangeCharge")]
    public decimal ExchangeCharge { get; set; }

    /// <summary>
    /// 兑币下限 0 无限制
    /// </summary>
    [SugarColumn(ColumnName = "ExchangeLimit")]
    public int ExchangeLimit { get; set; }

    /// <summary>
    /// 是否审核
    /// </summary>
    [SugarColumn(ColumnName = "IsCheck")]
    public bool IsCheck { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }
}

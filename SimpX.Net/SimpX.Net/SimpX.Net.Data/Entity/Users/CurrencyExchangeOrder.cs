namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的银行帐号
/// </summary>
[SugarTable("usr_CurrencyExchange")]
public class CurrencyExchangeOrder
{
    /// <summary>
    /// 账户编号
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "OrderID")]
    public string OrderID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "UserName")]
    public string UserName { get; set; }

    /// <summary>
    /// 转化前货币类型
    /// </summary>
    [SugarColumn(ColumnName = "BeforeCurrency")]
    public int BeforeCurrency { get; set; }

    /// <summary>
    /// 转化前额度
    /// </summary>
    [SugarColumn(ColumnName = "BeforeMoney")]
    public decimal BeforeMoney { get; set; }

    /// <summary>
    /// 转化后币种
    /// </summary>
    [SugarColumn(ColumnName = "AfterCurrency")]
    public int AfterCurrency { get; set; }

    /// <summary>
    /// 转化额度
    /// </summary>
    [SugarColumn(ColumnName = "AfterMoney")]
    public decimal AfterMoney { get; set; }

    /// <summary>
    /// 转化的金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 当前转化的汇率
    /// </summary>
    [SugarColumn(ColumnName = "Point")]
    public decimal Point { get; set; }

    /// <summary>
    /// 手续费
    /// </summary>
    [SugarColumn(ColumnName = "ExchangeMoney")]
    public decimal ExchangeMoney { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    [SugarColumn(ColumnName = "CheckTime")]
    public DateTime? CheckTime { get; set; }

    /// <summary>
    /// 审核人
    /// </summary>
    [SugarColumn(ColumnName = "Checker")]
    public int? Checker { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 添加时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }
}

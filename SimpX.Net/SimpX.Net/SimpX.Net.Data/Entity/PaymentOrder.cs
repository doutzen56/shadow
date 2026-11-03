namespace SimpX.Net.Data;

/// <summary>
/// 支付中心订单。只存渠道无关字段；按租户隔离。厂商差异放 ChannelTradeNo / ExtraJson。
/// </summary>
[SugarTable("pay_payment_order", "支付中心订单")]
public class PaymentOrder : EntityBaseTenant
{
    /// <summary>支付中心单号。</summary>
    [SugarColumn(ColumnDescription = "支付单号", Length = 64)]
    public string PaymentOrderNo { get; set; }

    /// <summary>业务订单号。</summary>
    [SugarColumn(ColumnDescription = "业务订单号", Length = 64)]
    public string BusinessOrderNo { get; set; }

    /// <summary>渠道编码。</summary>
    [SugarColumn(ColumnDescription = "渠道编码", Length = 32)]
    public string ChannelCode { get; set; }

    /// <summary>金额（元）。</summary>
    [SugarColumn(ColumnDescription = "金额", DecimalDigits = 2)]
    public decimal Amount { get; set; }

    /// <summary>标题。</summary>
    [SugarColumn(ColumnDescription = "标题", Length = 256)]
    public string Subject { get; set; }

    /// <summary>状态。</summary>
    [SugarColumn(ColumnDescription = "状态")]
    public PaymentStatus Status { get; set; }

    /// <summary>渠道交易号。</summary>
    [SugarColumn(ColumnDescription = "渠道交易号", Length = 128, IsNullable = true)]
    public string ChannelTradeNo { get; set; }

    /// <summary>附加数据。</summary>
    [SugarColumn(ColumnDescription = "附加数据", Length = 512, IsNullable = true)]
    public string Attach { get; set; }

    /// <summary>渠道扩展 JSON，禁止存密钥。</summary>
    [SugarColumn(ColumnDescription = "扩展JSON", ColumnDataType = "text", IsNullable = true)]
    public string ExtraJson { get; set; }
}

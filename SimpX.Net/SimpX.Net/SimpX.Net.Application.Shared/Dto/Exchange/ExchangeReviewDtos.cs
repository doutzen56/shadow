using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 待审核兑币列表筛选（对齐旧版 /admin/exchange/review/list）。
/// </summary>
public sealed class ExchangeReviewListInput
{
    /// <summary>开始时间；可空。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束时间；可空。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>会员用户名；可空。</summary>
    public string UserName { get; set; }

    /// <summary>兑出币种整型；空不筛。</summary>
    public string Currency { get; set; }
}

/// <summary>
/// 已审核兑币列表筛选（对齐旧版 /admin/exchange/review/log）。
/// </summary>
public sealed class ExchangeReviewLogInput
{
    /// <summary>时间类型：0 按审核时间，1 按创建时间。</summary>
    public int TimeType { get; set; }

    /// <summary>开始时间；可空。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束时间；可空。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>会员用户名；可空。</summary>
    public string UserName { get; set; }

    /// <summary>兑出币种整型；空不筛。</summary>
    public string Currency { get; set; }
}

/// <summary>
/// 兑币审核列表行。
/// </summary>
public sealed class ExchangeReviewListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>订单号。</summary>
    [JsonPropertyName("OrderID")]
    public string OrderID { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>会员用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>兑出币种。</summary>
    [JsonPropertyName("BeforeCurrency")]
    public int BeforeCurrency { get; set; }

    /// <summary>兑出前额度。</summary>
    [JsonPropertyName("BeforeMoney")]
    public decimal BeforeMoney { get; set; }

    /// <summary>兑入币种。</summary>
    [JsonPropertyName("AfterCurrency")]
    public int AfterCurrency { get; set; }

    /// <summary>兑入额度。</summary>
    [JsonPropertyName("AfterMoney")]
    public decimal AfterMoney { get; set; }

    /// <summary>手续费金额。</summary>
    [JsonPropertyName("ExchangeMoney")]
    public decimal ExchangeMoney { get; set; }

    /// <summary>兑出金额。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>状态：0 待处理，1 失败，2 成功等。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>汇率点数。</summary>
    [JsonPropertyName("Point")]
    public decimal Point { get; set; }

    /// <summary>审核时间；待审可空。</summary>
    [JsonPropertyName("CheckTime")]
    public DateTime? CheckTime { get; set; }

    /// <summary>审核人显示名；待审可空。</summary>
    [JsonPropertyName("Checker")]
    public string Checker { get; set; }
}

/// <summary>
/// 兑币审核入参（对齐旧版 /admin/exchange/review/check）。
/// </summary>
public sealed class ExchangeReviewCheckInput
{
    /// <summary>订单编号。</summary>
    public int ID { get; set; }

    /// <summary>审核结果：1 失败，2 成功（对齐旧 CheckStatus）。</summary>
    public byte Status { get; set; }

    /// <summary>备注；可空。</summary>
    public string Remark { get; set; }
}

/// <summary>
/// 汇兑总账筛选（对齐旧 summary）。
/// </summary>
public sealed class ExchangeSummaryInput
{
    /// <summary>开始日（含）；默认今天。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束日（含）；默认今天。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>
/// 汇兑总账一日数据。
/// </summary>
public sealed class ExchangeSummaryDayDto
{
    /// <summary>日期。</summary>
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    /// <summary>币种金额与人数（币种名、币种名+Count、TOTAL、NOFEE）。</summary>
    [JsonPropertyName("Data")]
    public Dictionary<string, decimal> Data { get; set; }
}

/// <summary>
/// 汇兑总账结果。
/// </summary>
public sealed class ExchangeSummaryOutput
{
    /// <summary>按日明细。</summary>
    [JsonPropertyName("Items")]
    public List<ExchangeSummaryDayDto> Items { get; set; }

    /// <summary>合计行（对齐旧 total Json）。</summary>
    [JsonPropertyName("Total")]
    public Dictionary<string, decimal> Total { get; set; }
}

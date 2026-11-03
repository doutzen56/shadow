using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 交收表筛选（对齐旧版 /admin/report/bill/list）。
/// </summary>
public sealed class SettlementBillListInput
{
    /// <summary>游戏类型整型；空不限。</summary>
    public string Game { get; set; }

    /// <summary>账单月份 yyyy-M 或 yyyy-MM。</summary>
    public string Month { get; set; }

    /// <summary>游戏分类整型；空不限。</summary>
    public string Category { get; set; }

    /// <summary>币种整型；空=全部并折算合计。</summary>
    public string Currency { get; set; }

    /// <summary>时区；空默认 1。</summary>
    public string TimeType { get; set; }
}

/// <summary>
/// 交收表行。
/// </summary>
public sealed class SettlementBillListItemDto
{
    /// <summary>游戏类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>游戏展示名。</summary>
    [JsonPropertyName("Game")]
    public string Game { get; set; }

    /// <summary>投注。</summary>
    [JsonPropertyName("BetMoney")]
    public decimal BetMoney { get; set; }

    /// <summary>有效投注。</summary>
    [JsonPropertyName("BetAmount")]
    public decimal BetAmount { get; set; }

    /// <summary>盈亏。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>JackPot。</summary>
    [JsonPropertyName("JackPot")]
    public decimal JackPot { get; set; }

    /// <summary>条件（点位）。</summary>
    [JsonPropertyName("Rate")]
    public decimal Rate { get; set; }

    /// <summary>交收金额。</summary>
    [JsonPropertyName("Pay")]
    public decimal Pay { get; set; }

    /// <summary>币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>分类描述串。</summary>
    [JsonPropertyName("Category")]
    public string Category { get; set; }

    /// <summary>JackPotCount。</summary>
    [JsonPropertyName("JackPotCount")]
    public decimal JackPotCount { get; set; }
}

/// <summary>
/// 交收合计。
/// </summary>
public sealed class SettlementBillSummaryDto
{
    /// <summary>投注合计。</summary>
    [JsonPropertyName("BetMoney")]
    public decimal BetMoney { get; set; }

    /// <summary>有效投注合计。</summary>
    [JsonPropertyName("BetAmount")]
    public decimal BetAmount { get; set; }

    /// <summary>盈亏合计。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>JackPot 合计。</summary>
    [JsonPropertyName("JackPot")]
    public decimal JackPot { get; set; }

    /// <summary>交收合计。</summary>
    [JsonPropertyName("PayMoney")]
    public decimal PayMoney { get; set; }

    /// <summary>单量合计。</summary>
    [JsonPropertyName("OrderCount")]
    public int? OrderCount { get; set; }

    /// <summary>JackPotCount 合计。</summary>
    [JsonPropertyName("JackPotCount")]
    public decimal JackPotCount { get; set; }
}

/// <summary>
/// 交收表输出。
/// </summary>
public sealed class SettlementBillListOutput
{
    /// <summary>行。</summary>
    public List<SettlementBillListItemDto> Data { get; set; }

    /// <summary>合计；无数据可为 null。</summary>
    public SettlementBillSummaryDto Summary { get; set; }
}

using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 对账报表筛选（对齐旧版 /admin/report/bill/billdate）。
/// </summary>
public sealed class ReconcileBillListInput
{
    /// <summary>游戏类型逗号分隔；空=当前站已开游戏。</summary>
    public string GameType { get; set; }

    /// <summary>开始日期；空不限。</summary>
    public string StartAt { get; set; }

    /// <summary>结束日期（含当天）；空不限。</summary>
    public string EndAt { get; set; }

    /// <summary>游戏分类整型；空不限。</summary>
    public string Category { get; set; }

    /// <summary>币种整型；空=全部并折算合计。</summary>
    public string Currency { get; set; }

    /// <summary>时区；空默认 1。</summary>
    public string TimeType { get; set; }

    /// <summary>仅开局游戏；传 1 时过滤 IsOpen。</summary>
    public string IsOpen { get; set; }
}

/// <summary>
/// 对账报表行。
/// </summary>
public sealed class ReconcileBillListItemDto
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

    /// <summary>单量。</summary>
    [JsonPropertyName("OrderCount")]
    public int? OrderCount { get; set; }

    /// <summary>会员数（去重）。</summary>
    [JsonPropertyName("Members")]
    public int Members { get; set; }

    /// <summary>盈亏。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>条件（点位）。</summary>
    [JsonPropertyName("Rate")]
    public decimal Rate { get; set; }

    /// <summary>交收金额。</summary>
    [JsonPropertyName("Pay")]
    public decimal Pay { get; set; }

    /// <summary>币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>JackPot。</summary>
    [JsonPropertyName("JackPot")]
    public decimal JackPot { get; set; }

    /// <summary>JackPotCount。</summary>
    [JsonPropertyName("JackPotCount")]
    public decimal JackPotCount { get; set; }
}

/// <summary>
/// 对账合计。
/// </summary>
public sealed class ReconcileBillSummaryDto
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

    /// <summary>交收合计。</summary>
    [JsonPropertyName("PayMoney")]
    public decimal PayMoney { get; set; }

    /// <summary>JackPot 合计。</summary>
    [JsonPropertyName("JackPot")]
    public decimal JackPot { get; set; }

    /// <summary>JackPotCount 合计。</summary>
    [JsonPropertyName("JackPotCount")]
    public decimal JackPotCount { get; set; }

    /// <summary>单量合计。</summary>
    [JsonPropertyName("OrderCount")]
    public int? OrderCount { get; set; }

    /// <summary>会员数合计。</summary>
    [JsonPropertyName("Members")]
    public int Members { get; set; }
}

/// <summary>
/// 对账报表输出。
/// </summary>
public sealed class ReconcileBillListOutput
{
    /// <summary>行。</summary>
    public List<ReconcileBillListItemDto> Data { get; set; }

    /// <summary>合计；无数据可为 null。</summary>
    public ReconcileBillSummaryDto Summary { get; set; }
}

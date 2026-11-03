using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 打赏报表筛选（对齐旧版 /admin/report/bill/rewarddate）。
/// </summary>
public sealed class TipReportListInput
{
    /// <summary>开始日期；空默认今天。</summary>
    public string StartAt { get; set; }

    /// <summary>结束日期（含当天）；空默认今天。</summary>
    public string EndAt { get; set; }

    /// <summary>币种整型；空=全部并折算合计。</summary>
    public string Currency { get; set; }

    /// <summary>交易类型整型；空不限。</summary>
    public string TradeType { get; set; }

    /// <summary>游戏类型逗号分隔；空=当前站已开游戏。</summary>
    public string GameType { get; set; }

    /// <summary>仅开局游戏；传 1 时过滤 IsOpen。</summary>
    public string IsOpen { get; set; }
}

/// <summary>
/// 打赏报表行。
/// </summary>
public sealed class TipReportListItemDto
{
    /// <summary>游戏类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>游戏展示名。</summary>
    [JsonPropertyName("Game")]
    public string Game { get; set; }

    /// <summary>会员数（去重）。</summary>
    [JsonPropertyName("Members")]
    public int Members { get; set; }

    /// <summary>打赏金额。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>条件（点位）。</summary>
    [JsonPropertyName("Rate")]
    public decimal Rate { get; set; }

    /// <summary>交易类型。</summary>
    [JsonPropertyName("TradeType")]
    public int TradeType { get; set; }

    /// <summary>币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }
}

/// <summary>
/// 打赏合计。
/// </summary>
public sealed class TipReportSummaryDto
{
    /// <summary>金额合计。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>会员数合计。</summary>
    [JsonPropertyName("Members")]
    public int Members { get; set; }
}

/// <summary>
/// 打赏报表输出。
/// </summary>
public sealed class TipReportListOutput
{
    /// <summary>行。</summary>
    public List<TipReportListItemDto> Data { get; set; }

    /// <summary>合计；无数据可为 null。</summary>
    public TipReportSummaryDto Summary { get; set; }
}

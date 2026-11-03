using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 平台货币汇率列表筛选（对齐旧版 /system/setting/config/exchangelist）。
/// </summary>
public sealed class SystemExchangeListInput
{
    /// <summary>基准币整型（旧 Currency）。</summary>
    public int Currency { get; set; }
}

/// <summary>
/// 平台货币汇率列表行。
/// </summary>
public sealed class SystemExchangeListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>币对展示（如 CNY/USD）。</summary>
    [JsonPropertyName("CurrencyGroup")]
    public string CurrencyGroup { get; set; }

    /// <summary>市场价。</summary>
    [JsonPropertyName("MarketPrice")]
    public decimal MarketPrice { get; set; }

    /// <summary>更新时间。</summary>
    [JsonPropertyName("UpdateAt")]
    public DateTime UpdateAt { get; set; }
}

/// <summary>
/// 改市场价入参（对齐旧 updateExchange）。
/// </summary>
public sealed class SystemExchangeUpdatePriceInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>市场价（字符串，对齐旧 Form）。</summary>
    public string MarketPrice { get; set; }
}

/// <summary>
/// 改市场价结果。
/// </summary>
public sealed class SystemExchangeUpdatePriceResult
{
    /// <summary>服务端写入的更新时间。</summary>
    [JsonPropertyName("UpdateAt")]
    public DateTime UpdateAt { get; set; }
}

using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 汇率列表筛选（对齐旧版 /admin/exchange/rate/list）。
/// </summary>
public sealed class ExchangeRateListInput
{
    /// <summary>基准币整型（旧 Currency）。</summary>
    public int Currency { get; set; }
}

/// <summary>
/// 汇率列表行。
/// </summary>
public sealed class ExchangeRateListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>币对展示。</summary>
    [JsonPropertyName("CurrencyGroup")]
    public string CurrencyGroup { get; set; }

    /// <summary>市场价。</summary>
    [JsonPropertyName("MarketPrice")]
    public decimal MarketPrice { get; set; }

    /// <summary>最终点数。</summary>
    [JsonPropertyName("Point")]
    public decimal Point { get; set; }

    /// <summary>模式文案。</summary>
    [JsonPropertyName("Mode")]
    public string Mode { get; set; }

    /// <summary>参数展示。</summary>
    [JsonPropertyName("Parameter")]
    public string Parameter { get; set; }
}

/// <summary>
/// 基准币。
/// </summary>
public sealed class ExchangeBaseCurrencyDto
{
    /// <summary>基准币整型。</summary>
    [JsonPropertyName("Base")]
    public int Base { get; set; }

    /// <summary>同 Base。</summary>
    [JsonPropertyName("BaseValue")]
    public int BaseValue { get; set; }
}

/// <summary>
/// 汇率详情（对齐旧 info）。
/// </summary>
public sealed class ExchangeRateDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>市场价。</summary>
    [JsonPropertyName("MarketPrice")]
    public decimal MarketPrice { get; set; }

    /// <summary>站点。</summary>
    [JsonPropertyName("SiteID")]
    public int SiteID { get; set; }

    /// <summary>模式。</summary>
    [JsonPropertyName("ExchangeMode")]
    public byte ExchangeMode { get; set; }

    /// <summary>参数。</summary>
    [JsonPropertyName("Parameter")]
    public decimal Parameter { get; set; }

    /// <summary>点数。</summary>
    [JsonPropertyName("Point")]
    public decimal Point { get; set; }

    /// <summary>是否基准。</summary>
    [JsonPropertyName("IsBase")]
    public bool IsBase { get; set; }
}

/// <summary>
/// 汇率保存入参（对齐旧版 /admin/exchange/rate/saveinfo）。
/// </summary>
public sealed class ExchangeRateSaveInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>市场价。</summary>
    public decimal MarketPrice { get; set; }

    /// <summary>模式：0 市场行情，1 差价，2 倍率，3 自定。</summary>
    public byte ExchangeMode { get; set; }

    /// <summary>参数（差价/倍率/自定点数）。</summary>
    public decimal Parameter { get; set; }
}

/// <summary>
/// 兑币设置列表筛选（对齐旧版 /admin/exchange/rate/exchangelist）。
/// </summary>
public sealed class ExchangeSettingListInput
{
    /// <summary>基准币（旧 CurrencyType）。</summary>
    public int CurrencyType { get; set; }

    /// <summary>状态筛选项；空不筛。0 关闭，1 开放。</summary>
    public string Status { get; set; }
}

/// <summary>
/// 兑币设置列表行。
/// </summary>
public sealed class ExchangeSettingListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>币对展示。</summary>
    [JsonPropertyName("CurrencyGroup")]
    public string CurrencyGroup { get; set; }

    /// <summary>手续费比例（库内小数）。</summary>
    [JsonPropertyName("ExchangeCharge")]
    public decimal ExchangeCharge { get; set; }

    /// <summary>状态文案。</summary>
    [JsonPropertyName("Status")]
    public string Status { get; set; }

    /// <summary>手续费模式文案。</summary>
    [JsonPropertyName("ExchangeFee")]
    public string ExchangeFee { get; set; }

    /// <summary>点数。</summary>
    [JsonPropertyName("Point")]
    public decimal Point { get; set; }

    /// <summary>兑币下限（旧 Limit）。</summary>
    [JsonPropertyName("Limit")]
    public int Limit { get; set; }
}

/// <summary>
/// 兑币设置详情（对齐旧 exchangeinfo）。
/// </summary>
public sealed class ExchangeSettingDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>币对展示。</summary>
    [JsonPropertyName("CurrencyGroup")]
    public string CurrencyGroup { get; set; }

    /// <summary>站点。</summary>
    [JsonPropertyName("SiteID")]
    public int SiteID { get; set; }

    /// <summary>手续费模式：0 倍率，1 自定。</summary>
    [JsonPropertyName("ExchangeFee")]
    public byte ExchangeFee { get; set; }

    /// <summary>兑币下限。</summary>
    [JsonPropertyName("ExchangeLimit")]
    public int ExchangeLimit { get; set; }

    /// <summary>状态：0 关闭，1 开放。</summary>
    [JsonPropertyName("Status")]
    public byte Status { get; set; }

    /// <summary>是否审核。</summary>
    [JsonPropertyName("IsCheck")]
    public bool IsCheck { get; set; }

    /// <summary>手续费百分比字符串，对齐旧 ExchangeCharge*100 的 0.00 展示。</summary>
    [JsonPropertyName("ExchangeCharge")]
    public string ExchangeCharge { get; set; }
}

/// <summary>
/// 兑币设置保存入参（对齐旧 saveexchange）。
/// </summary>
public sealed class ExchangeSettingSaveInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>手续费模式：0 倍率，1 自定。</summary>
    public byte ExchangeFee { get; set; }

    /// <summary>手续费百分比（表单值；服务端除以 100 入库）。</summary>
    public decimal ExchangeCharge { get; set; }

    /// <summary>兑币下限；0 表示不限。</summary>
    public int ExchangeLimit { get; set; }

    /// <summary>是否审核。</summary>
    public bool IsCheck { get; set; }

    /// <summary>状态：0 关闭，1 开放。</summary>
    public byte Status { get; set; }
}

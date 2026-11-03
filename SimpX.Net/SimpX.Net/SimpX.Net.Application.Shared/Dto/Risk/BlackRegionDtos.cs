using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>国别/地区黑名单列表出参。</summary>
public sealed class BlackRegionListOutput
{
    /// <summary>已限制地区名列表。</summary>
    [JsonPropertyName("Regions")]
    public List<string> Regions { get; set; } = [];
}

/// <summary>保存国别限制入参。对齐旧 saveblackregion。</summary>
public sealed class BlackRegionSaveInput
{
    /// <summary>地区名（不可含逗号）。</summary>
    [JsonPropertyName("Value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>true=加入；false=移除。</summary>
    [JsonPropertyName("Save")]
    public bool Save { get; set; } = true;
}

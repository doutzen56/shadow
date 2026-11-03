using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>手机黑名单列表行。</summary>
public sealed class BlackMobileListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>手机号。</summary>
    [JsonPropertyName("BlackMobile")]
    public string BlackMobile { get; set; } = string.Empty;

    /// <summary>备注。</summary>
    [JsonPropertyName("Demo")]
    public string Demo { get; set; } = string.Empty;

    /// <summary>是否已禁止（"1"）。</summary>
    [JsonPropertyName("IsLock")]
    public string IsLock { get; set; } = string.Empty;

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }
}

/// <summary>手机黑名单保存入参。</summary>
public sealed class BlackMobileSaveInput
{
    /// <summary>编号；0=新建。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>手机号。</summary>
    [JsonPropertyName("BlackMobile")]
    public string BlackMobile { get; set; } = string.Empty;

    /// <summary>备注。</summary>
    [JsonPropertyName("Demo")]
    public string Demo { get; set; } = string.Empty;
}

/// <summary>证件黑名单列表行。</summary>
public sealed class BlackKycListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>证件号。</summary>
    [JsonPropertyName("BlackKyc")]
    public string BlackKyc { get; set; } = string.Empty;

    /// <summary>备注。</summary>
    [JsonPropertyName("Demo")]
    public string Demo { get; set; } = string.Empty;

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }
}

/// <summary>证件黑名单保存入参。</summary>
public sealed class BlackKycSaveInput
{
    /// <summary>编号；0=新建。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>证件号。</summary>
    [JsonPropertyName("BlackKyc")]
    public string BlackKyc { get; set; } = string.Empty;

    /// <summary>备注。</summary>
    [JsonPropertyName("Demo")]
    public string Demo { get; set; } = string.Empty;
}
